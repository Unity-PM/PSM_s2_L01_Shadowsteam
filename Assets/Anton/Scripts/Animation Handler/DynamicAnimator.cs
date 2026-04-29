using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Serialization;

[RequireComponent(typeof(Animator))]
public class UniversalClipAnimator : MonoBehaviour
{
    public enum InputPlaybackMode
    {
        Auto,
        Press,
        Hold,
        PressAndHold
    }

    [Serializable]
    public class AnimationState
    {
        public string id;
        public AnimationClip clip;
        public bool loop = true;
        public float fadeTime = 0.15f;
        public float speed = 1f;
        public string nextStateAfterFinish;
    }

    [Serializable]
    public class InputAnimationBinding
    {
        public InputActionReference action;
        public string animationId;
        [Tooltip("Auto keeps old behavior. Press plays once. Hold stays active while pressed. PressAndHold plays once on tap and repeats full cycles while held.")]
        public InputPlaybackMode playbackMode = InputPlaybackMode.Auto;
        [FormerlySerializedAs("playOnlyOnPressed")]
        [HideInInspector] public bool legacyPlayOnlyOnPressed = true;
        public bool forcePlay;
        public int priority;
        public string releaseStateId;
        public bool finishCurrentCycleOnRelease;
    }

    [Header("Animation Setup")]
    [SerializeField] private string defaultState = "Idle";
    [SerializeField] private List<AnimationState> states = new();

    [Header("Input Setup")]
    [SerializeField] private List<InputAnimationBinding> inputBindings = new();

    private readonly Dictionary<string, AnimationState> stateMap = new();
    private readonly Dictionary<int, double> activeBindingTimes = new();
    private readonly HashSet<InputAction> subscribedActions = new();

    private Animator animator;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable currentPlayable;
    private AnimationClipPlayable nextPlayable;

    private int currentInput;
    private int nextInput = 1;

    private AnimationState currentState;
    private AnimationState pendingState;
    private float fadeTimer;
    private float currentFadeTime;
    private bool isFading;
    private string deferredReleaseAnimationId;
    private string deferredReleaseFallbackStateId;
    private int deferredReleaseTargetCycle = -1;

    public string CurrentStateId => pendingState?.id ?? currentState?.id;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        RebuildStateMap();

        graph = PlayableGraph.Create($"{name}_UniversalAnimator");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        mixer = AnimationMixerPlayable.Create(graph, 2);
        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        output.SetSourcePlayable(mixer);

        mixer.SetInputWeight(currentInput, 1f);
        mixer.SetInputWeight(nextInput, 0f);

        graph.Play();

        Play(defaultState, true);
    }

    private void OnEnable()
    {
        subscribedActions.Clear();

        foreach (var binding in inputBindings)
        {
            var action = binding.action?.action;
            if (action == null || !subscribedActions.Add(action))
                continue;

            action.Enable();
            action.performed += OnActionPerformed;
            action.canceled += OnActionCanceled;
        }
    }

    private void OnDisable()
    {
        foreach (var action in subscribedActions)
        {
            action.performed -= OnActionPerformed;
            action.canceled -= OnActionCanceled;
        }

        subscribedActions.Clear();
        activeBindingTimes.Clear();
    }

    private void Update()
    {
        HandleFade();
        HandleDeferredRelease();
        HandleAutoTransition();
    }

    public void Play(string stateId)
    {
        Play(stateId, false);
    }

    public void ForcePlay(string stateId)
    {
        Play(stateId, true);
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        for (int i = 0; i < inputBindings.Count; i++)
        {
            var binding = inputBindings[i];
            if (binding.action?.action != context.action || string.IsNullOrWhiteSpace(binding.animationId))
                continue;

            if (ShouldTrackAsActive(binding, context.action))
                activeBindingTimes[i] = Time.timeAsDouble;

            CancelDeferredRelease(binding.animationId);

            if (binding.forcePlay)
                ForcePlay(binding.animationId);
            else
                Play(binding.animationId);
        }
    }

    private void OnActionCanceled(InputAction.CallbackContext context)
    {
        string fallbackStateId = null;
        bool changed = false;
        string deferredAnimationId = null;

        for (int i = 0; i < inputBindings.Count; i++)
        {
            var binding = inputBindings[i];
            if (binding.action?.action != context.action || !ShouldTrackAsActive(binding, context.action))
                continue;

            changed |= activeBindingTimes.Remove(i);

            if (string.IsNullOrWhiteSpace(fallbackStateId) && !string.IsNullOrWhiteSpace(binding.releaseStateId))
                fallbackStateId = binding.releaseStateId;

            if (ShouldFinishCurrentCycleOnRelease(binding) && IsCurrentOrPendingState(binding.animationId))
                deferredAnimationId = binding.animationId;
        }

        if (!changed)
            return;

        string resolvedStateId = ResolveStateFromInput(fallbackStateId);
        if (!string.IsNullOrWhiteSpace(deferredAnimationId) && !string.Equals(resolvedStateId, deferredAnimationId, StringComparison.Ordinal))
        {
            ScheduleDeferredRelease(deferredAnimationId, fallbackStateId);
            return;
        }

        Play(resolvedStateId);
    }

    private void Play(string stateId, bool force)
    {
        if (!TryGetState(stateId, out var state))
            return;

        bool isSameCurrentState = currentState?.id == state.id;
        bool isSamePendingState = pendingState?.id == state.id;
        bool canReplayFinishedCurrentState = isSameCurrentState && CanReplayCurrentState(state);

        if (!force && (isSamePendingState || (isSameCurrentState && !canReplayFinishedCurrentState)))
            return;

        if (force || !string.Equals(state.id, deferredReleaseAnimationId, StringComparison.Ordinal))
            ClearDeferredRelease();

        if (!currentPlayable.IsValid() && !isFading)
        {
            currentPlayable = CreatePlayable(state);
            mixer.DisconnectInput(currentInput);
            mixer.ConnectInput(currentInput, currentPlayable, 0);
            mixer.SetInputWeight(currentInput, 1f);
            mixer.SetInputWeight(nextInput, 0f);

            currentState = state;
            pendingState = null;
            return;
        }

        if (nextPlayable.IsValid())
            nextPlayable.Destroy();

        nextPlayable = CreatePlayable(state);
        mixer.DisconnectInput(nextInput);
        mixer.ConnectInput(nextInput, nextPlayable, 0);
        mixer.SetInputWeight(nextInput, 0f);

        pendingState = state;
        fadeTimer = 0f;
        currentFadeTime = Mathf.Max(0.01f, state.fadeTime);
        isFading = true;
    }

    private AnimationClipPlayable CreatePlayable(AnimationState state)
    {
        var playable = AnimationClipPlayable.Create(graph, state.clip);
        playable.SetSpeed(state.speed);
        playable.SetTime(0d);
        playable.SetApplyFootIK(true);
        return playable;
    }

    private void HandleFade()
    {
        if (!isFading)
            return;

        fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fadeTimer / currentFadeTime);

        mixer.SetInputWeight(currentInput, 1f - t);
        mixer.SetInputWeight(nextInput, t);

        if (t < 1f)
            return;

        if (currentPlayable.IsValid())
        {
            mixer.DisconnectInput(currentInput);
            currentPlayable.Destroy();
        }

        currentPlayable = nextPlayable;
        nextPlayable = default;
        currentState = pendingState;
        pendingState = null;

        int previousInput = currentInput;
        currentInput = nextInput;
        nextInput = previousInput;

        mixer.SetInputWeight(currentInput, 1f);
        mixer.SetInputWeight(nextInput, 0f);

        isFading = false;
    }

    private void HandleDeferredRelease()
    {
        if (string.IsNullOrWhiteSpace(deferredReleaseAnimationId) || isFading)
            return;

        if (currentState == null || !string.Equals(currentState.id, deferredReleaseAnimationId, StringComparison.Ordinal) || !currentPlayable.IsValid())
        {
            ClearDeferredRelease();
            return;
        }

        if (deferredReleaseTargetCycle < 0)
            deferredReleaseTargetCycle = GetTargetCycleIndex(currentPlayable.GetTime(), currentState);

        if (!HasReachedCycleEnd(currentPlayable.GetTime(), currentState, deferredReleaseTargetCycle))
            return;

        string nextStateId = ResolveStateFromInput(deferredReleaseFallbackStateId);
        ClearDeferredRelease();
        Play(nextStateId);
    }

    private void HandleAutoTransition()
    {
        if (isFading || currentState == null || currentState.loop || !currentPlayable.IsValid())
            return;

        if (currentPlayable.GetTime() < currentState.clip.length)
            return;

        if (TryGetActiveBindingForState(currentState.id, out _, out _))
        {
            ForcePlay(currentState.id);
            return;
        }

        if (!string.IsNullOrWhiteSpace(currentState.nextStateAfterFinish))
        {
            Play(currentState.nextStateAfterFinish);
            return;
        }

        Play(ResolveStateFromInput(defaultState));
    }

    private string ResolveStateFromInput(string fallbackStateId)
    {
        int bestBindingIndex = -1;
        int bestPriority = int.MinValue;
        double bestTime = double.MinValue;

        foreach (var pair in activeBindingTimes)
        {
            int bindingIndex = pair.Key;
            if (bindingIndex < 0 || bindingIndex >= inputBindings.Count)
                continue;

            var binding = inputBindings[bindingIndex];
            if (string.IsNullOrWhiteSpace(binding.animationId))
                continue;

            if (binding.priority > bestPriority || (binding.priority == bestPriority && pair.Value > bestTime))
            {
                bestBindingIndex = bindingIndex;
                bestPriority = binding.priority;
                bestTime = pair.Value;
            }
        }

        if (bestBindingIndex >= 0)
            return inputBindings[bestBindingIndex].animationId;

        return string.IsNullOrWhiteSpace(fallbackStateId) ? defaultState : fallbackStateId;
    }

    private void ScheduleDeferredRelease(string animationId, string fallbackStateId)
    {
        deferredReleaseAnimationId = animationId;
        deferredReleaseFallbackStateId = fallbackStateId;
        deferredReleaseTargetCycle = -1;
    }

    private void CancelDeferredRelease(string animationId)
    {
        if (string.Equals(deferredReleaseAnimationId, animationId, StringComparison.Ordinal))
            ClearDeferredRelease();
    }

    private void ClearDeferredRelease()
    {
        deferredReleaseAnimationId = null;
        deferredReleaseFallbackStateId = null;
        deferredReleaseTargetCycle = -1;
    }

    private bool IsCurrentOrPendingState(string stateId)
    {
        if (string.IsNullOrWhiteSpace(stateId))
            return false;

        return string.Equals(currentState?.id, stateId, StringComparison.Ordinal)
               || string.Equals(pendingState?.id, stateId, StringComparison.Ordinal);
    }

    private static int GetTargetCycleIndex(double time, AnimationState state)
    {
        if (state?.clip == null || state.clip.length <= Mathf.Epsilon)
            return 0;

        if (!state.loop)
            return 1;

        double safeTime = Math.Max(0d, time);
        return Mathf.FloorToInt((float)(safeTime / state.clip.length)) + 1;
    }

    private static bool HasReachedCycleEnd(double time, AnimationState state, int targetCycleIndex)
    {
        if (state?.clip == null || state.clip.length <= Mathf.Epsilon || targetCycleIndex <= 0)
            return true;

        if (!state.loop)
            return time >= state.clip.length;

        return time >= state.clip.length * targetCycleIndex;
    }

    private bool CanReplayCurrentState(AnimationState requestedState)
    {
        if (isFading || currentState == null || currentPlayable.IsValid() == false)
            return false;

        if (!ReferenceEquals(currentState, requestedState) && currentState.id != requestedState.id)
            return false;

        return !currentState.loop && currentPlayable.GetTime() >= currentState.clip.length;
    }

    private bool TryGetActiveBindingForState(string stateId, out int bindingIndex, out InputAnimationBinding binding)
    {
        bindingIndex = -1;
        binding = null;

        if (string.IsNullOrWhiteSpace(stateId))
            return false;

        int bestPriority = int.MinValue;
        double bestTime = double.MinValue;

        foreach (var pair in activeBindingTimes)
        {
            int currentBindingIndex = pair.Key;
            if (currentBindingIndex < 0 || currentBindingIndex >= inputBindings.Count)
                continue;

            var currentBinding = inputBindings[currentBindingIndex];
            if (!string.Equals(currentBinding.animationId, stateId, StringComparison.Ordinal))
                continue;

            if (currentBinding.priority > bestPriority || (currentBinding.priority == bestPriority && pair.Value > bestTime))
            {
                bindingIndex = currentBindingIndex;
                binding = currentBinding;
                bestPriority = currentBinding.priority;
                bestTime = pair.Value;
            }
        }

        return binding != null;
    }

    private static bool ShouldTrackAsActive(InputAnimationBinding binding, InputAction action)
    {
        if (binding == null || action == null)
            return false;

        switch (binding.playbackMode)
        {
            case InputPlaybackMode.Hold:
            case InputPlaybackMode.PressAndHold:
                return true;
            case InputPlaybackMode.Press:
                return false;
            default:
                return !binding.legacyPlayOnlyOnPressed
                       || action.type == InputActionType.Value
                       || action.type == InputActionType.PassThrough;
        }
    }

    private static bool ShouldFinishCurrentCycleOnRelease(InputAnimationBinding binding)
    {
        if (binding == null)
            return false;

        return binding.finishCurrentCycleOnRelease || binding.playbackMode == InputPlaybackMode.PressAndHold;
    }

    private bool TryGetState(string stateId, out AnimationState state)
    {
        state = null;

        if (string.IsNullOrWhiteSpace(stateId))
            return false;

        if (stateMap.TryGetValue(stateId, out state))
            return true;

        Debug.LogWarning($"Animation state not found: {stateId}", this);
        return false;
    }

    private void RebuildStateMap()
    {
        stateMap.Clear();

        foreach (var state in states)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.id) || state.clip == null)
                continue;

            if (stateMap.ContainsKey(state.id))
                Debug.LogWarning($"Duplicate animation state id '{state.id}' on {name}. Last value wins.", this);

            stateMap[state.id] = state;
        }
    }

    private void OnDestroy()
    {
        if (nextPlayable.IsValid())
            nextPlayable.Destroy();

        if (currentPlayable.IsValid())
            currentPlayable.Destroy();

        if (graph.IsValid())
            graph.Destroy();
    }
}
