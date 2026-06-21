using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class DynamicAnimator : MonoBehaviour
{
    public enum AnimationCategory
    {
        Locomotion,
        Combat,
        Death,
        Interaction
    }

    [Serializable]
    public class AnimationState
    {
        public string id;
        public AnimationClip clip;
        public AnimationCategory category = AnimationCategory.Locomotion;
        public bool loop = true;
        public float fadeTime = 0.15f;
        public float speed = 1f;
        public string nextStateAfterFinish;
        [Tooltip("Blocks movement while the clip is playing (Combat/Death categories set this automatically).")]
        public bool locksMovement;
    }

    [Header("Animation Setup")]
    [SerializeField] private string defaultState = "Idle";
    [SerializeField] private List<AnimationState> states = new();
    [SerializeField] private float defaultLocomotionFadeTime = 0.15f;
    [SerializeField] private float defaultActionFadeTime = 0.08f;

    private readonly Dictionary<string, AnimationState> stateMap = new();
    private readonly Dictionary<string, AnimationClipPlayable> playableCache = new();

    private Animator animator;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable currentPlayable;
    private AnimationClipPlayable nextPlayable;

    private int currentInput;
    private int nextInput = 1;

    private AnimationState currentState;
    private AnimationState pendingState;
    private float currentFadeTime;
    private double fadeStartGraphTime;
    private bool isFading;

    public string CurrentStateId => pendingState?.id ?? currentState?.id;

    public bool IsMovementLocked =>
        IsStateCurrentlyBlocking(currentState)
        || (isFading && IsStateCurrentlyBlocking(pendingState));

    public AnimationCategory GetCategory(string stateId)
    {
        return stateMap.TryGetValue(stateId, out var state) ? state.category : AnimationCategory.Locomotion;
    }

    public bool TryGetClipLength(string stateId, out float length)
    {
        length = 0f;

        if (!stateMap.TryGetValue(stateId, out var state) || state?.clip == null)
            return false;

        length = state.clip.length / Mathf.Max(0.01f, state.speed);
        return true;
    }

    public bool CanRestartState(string stateId)
    {
        if (!TryGetState(stateId, out var state))
            return true;

        bool isCurrent = string.Equals(currentState?.id, stateId, StringComparison.Ordinal);
        bool isPending = string.Equals(pendingState?.id, stateId, StringComparison.Ordinal);
        if (!isCurrent && !isPending)
            return true;

        if (isPending && isFading)
            return false;

        if (!isCurrent)
            return false;

        return CanReplayCurrentState(state);
    }

    public bool TryGetStateNormalizedTime(string stateId, out float normalizedTime)
    {
        normalizedTime = 0f;

        if (!stateMap.TryGetValue(stateId, out var state) || state?.clip == null)
            return false;

        if (currentState == null || !string.Equals(currentState.id, stateId, StringComparison.Ordinal))
            return false;

        if (!currentPlayable.IsValid())
            return false;

        float duration = state.clip.length / Mathf.Max(0.01f, state.speed);
        if (duration <= Mathf.Epsilon)
            return false;

        normalizedTime = Mathf.Clamp01((float)(currentPlayable.GetTime() / duration));
        return true;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator.runtimeAnimatorController != null)
            animator.runtimeAnimatorController = null;

        RebuildStateMap();
        ApplyCategoryDefaults();
        EnsureValidDefaultState();

        graph = PlayableGraph.Create($"{name}_DynamicAnimator");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        mixer = AnimationMixerPlayable.Create(graph, 2);
        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        output.SetSourcePlayable(mixer);

        mixer.SetInputWeight(currentInput, 1f);
        mixer.SetInputWeight(nextInput, 0f);

        BuildPlayableCache();
        graph.Play();

        Play(defaultState, true);
    }

    private void OnEnable()
    {
        if (graph.IsValid())
            graph.Play();
    }

    private void OnDisable()
    {
        if (graph.IsValid())
            graph.Stop();
    }

    private void Update()
    {
        ApplyLoopingStateWrap();
        HandleFade();
        HandleAutoTransition();
    }

    public void Play(string stateId) => Play(stateId, false);

    public void ForcePlay(string stateId) => Play(stateId, true);

    public void ResetToState(string stateId) => ForcePlay(stateId);

    private void Play(string stateId, bool force)
    {
        if (!TryGetState(stateId, out var state))
            return;

        if (!force && IsMovementLocked && !IsStateLockingMovement(state))
            return;

        var playable = GetOrCreatePlayable(state);

        bool isSameCurrentState = currentState?.id == state.id;
        bool isSamePendingState = pendingState?.id == state.id;
        bool canReplayFinishedCurrentState = isSameCurrentState && CanReplayCurrentState(state);

        if (isSameCurrentState && !canReplayFinishedCurrentState)
            return;

        if (!force && isSamePendingState)
            return;

        if (!currentPlayable.IsValid() && !isFading)
        {
            ApplyImmediateState(state, playable);
            return;
        }

        if (isFading)
            CompleteFadeImmediately();

        if (ReferenceEquals(playable, currentPlayable))
        {
            ApplyImmediateState(state, playable);
            return;
        }

        if (ReferenceEquals(playable, nextPlayable) && nextPlayable.IsValid())
            return;

        DisconnectPlayableFromMixer(playable);

        nextPlayable = ResetPlayable(playable, state);
        mixer.DisconnectInput(nextInput);
        mixer.ConnectInput(nextInput, nextPlayable, 0);
        mixer.SetInputWeight(currentInput, 1f);
        mixer.SetInputWeight(nextInput, 0f);

        pendingState = state;
        fadeStartGraphTime = GetGraphTime();
        currentFadeTime = ResolveFadeTime(state);
        isFading = true;
    }

    private void ApplyImmediateState(AnimationState state, AnimationClipPlayable playable)
    {
        if (isFading)
            CompleteFadeImmediately();

        playable = ResetPlayable(playable, state);
        DisconnectPlayableFromMixer(playable);

        mixer.DisconnectInput(currentInput);
        mixer.DisconnectInput(nextInput);

        currentPlayable = playable;
        nextPlayable = default;
        mixer.ConnectInput(currentInput, currentPlayable, 0);
        mixer.SetInputWeight(currentInput, 1f);
        mixer.SetInputWeight(nextInput, 0f);

        currentState = state;
        pendingState = null;
        isFading = false;
    }

    private void CompleteFadeImmediately()
    {
        if (!isFading)
            return;

        mixer.DisconnectInput(currentInput);

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

    private void DisconnectPlayableFromMixer(AnimationClipPlayable playable)
    {
        if (!mixer.IsValid() || !playable.IsValid())
            return;

        int inputCount = mixer.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            var input = mixer.GetInput(i);
            if (input.IsValid() && input.Equals(playable))
                mixer.DisconnectInput(i);
        }
    }

    private AnimationClipPlayable ResetPlayable(AnimationClipPlayable playable, AnimationState state)
    {
        playable.SetSpeed(state.speed);
        playable.SetTime(0d);
        return playable;
    }

    private AnimationClipPlayable PreparePlayable(AnimationState state)
    {
        return ResetPlayable(GetOrCreatePlayable(state), state);
    }

    private AnimationClipPlayable GetOrCreatePlayable(AnimationState state)
    {
        if (playableCache.TryGetValue(state.id, out var cached) && cached.IsValid())
            return cached;

        var playable = AnimationClipPlayable.Create(graph, state.clip);
        playable.SetApplyFootIK(true);
        playableCache[state.id] = playable;
        return playable;
    }

    private void BuildPlayableCache()
    {
        playableCache.Clear();

        foreach (var pair in stateMap)
        {
            var state = pair.Value;
            var playable = AnimationClipPlayable.Create(graph, state.clip);
            playable.SetApplyFootIK(true);
            playableCache[pair.Key] = playable;
        }
    }

    private double GetGraphTime()
    {
        if (!graph.IsValid())
            return 0d;

        var root = graph.GetRootPlayable(0);
        return root.IsValid() ? root.GetTime() : 0d;
    }

    float ResolveFadeTime(AnimationState state)
    {
        if (state.fadeTime > 0f)
            return state.fadeTime;

        float fade = IsStateLockingMovement(state) ? defaultActionFadeTime : defaultLocomotionFadeTime;
        return Mathf.Max(0.01f, fade);
    }

    bool IsStateCurrentlyBlocking(AnimationState state)
    {
        if (state == null)
            return false;

        if (state.category == AnimationCategory.Death)
            return ReferenceEquals(state, currentState) || ReferenceEquals(state, pendingState);

        if (!IsStateLockingMovement(state))
            return false;

        if (ReferenceEquals(state, pendingState))
            return true;

        return IsClipStillPlaying(state);
    }

    bool IsClipStillPlaying(AnimationState state)
    {
        if (state?.clip == null || !currentPlayable.IsValid() || currentState != state)
            return false;

        float duration = state.clip.length / Mathf.Max(0.01f, state.speed);
        return currentPlayable.GetTime() < duration - 0.02f;
    }

    bool IsStateLockingMovement(AnimationState state)
    {
        if (state == null)
            return false;

        if (state.locksMovement)
            return true;

        return state.category is AnimationCategory.Combat or AnimationCategory.Death;
    }

    private void HandleFade()
    {
        if (!isFading)
            return;

        float elapsed = (float)(GetGraphTime() - fadeStartGraphTime);
        float t = Mathf.Clamp01(elapsed / currentFadeTime);

        mixer.SetInputWeight(currentInput, 1f - t);
        mixer.SetInputWeight(nextInput, t);

        if (t < 1f)
            return;

        CompleteFadeImmediately();
    }

    private void ApplyLoopingStateWrap()
    {
        WrapPlayableIfNeeded(currentState, currentPlayable);
        if (isFading)
            WrapPlayableIfNeeded(pendingState, nextPlayable);
    }

    private void WrapPlayableIfNeeded(AnimationState state, AnimationClipPlayable playable)
    {
        if (state == null || !state.loop || state.clip == null || !playable.IsValid())
            return;

        double duration = GetStateDuration(state);
        if (duration <= double.Epsilon)
            return;

        double time = playable.GetTime();
        if (time < duration)
            return;

        playable.SetTime(time % duration);
    }

    private void HandleAutoTransition()
    {
        if (isFading || currentState == null || currentState.loop || !currentPlayable.IsValid())
            return;

        if (currentPlayable.GetTime() < GetStateDuration(currentState))
            return;

        if (!string.IsNullOrWhiteSpace(currentState.nextStateAfterFinish))
        {
            Play(currentState.nextStateAfterFinish);
            return;
        }

        Play(defaultState);
    }

    private bool CanReplayCurrentState(AnimationState requestedState)
    {
        if (isFading || currentState == null || !currentPlayable.IsValid())
            return false;

        if (!ReferenceEquals(currentState, requestedState) && currentState.id != requestedState.id)
            return false;

        return !currentState.loop && currentPlayable.GetTime() >= GetStateDuration(currentState);
    }

    double GetStateDuration(AnimationState state)
    {
        if (state?.clip == null)
            return 0d;

        return state.clip.length / Mathf.Max(0.01f, state.speed);
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

    void ApplyCategoryDefaults()
    {
        foreach (var state in states)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.id))
                continue;

            InferLegacyCategory(state);

            switch (state.category)
            {
                case AnimationCategory.Combat:
                    state.loop = false;
                    state.locksMovement = true;
                    if (state.fadeTime <= 0f)
                        state.fadeTime = defaultActionFadeTime;
                    if (string.IsNullOrWhiteSpace(state.nextStateAfterFinish))
                        state.nextStateAfterFinish = defaultState;
                    break;

                case AnimationCategory.Death:
                    state.loop = false;
                    state.locksMovement = true;
                    if (state.fadeTime <= 0f)
                        state.fadeTime = defaultActionFadeTime;
                    break;
            }
        }
    }

    static void InferLegacyCategory(AnimationState state)
    {
        if (state.category != AnimationCategory.Locomotion)
            return;

        if (!string.IsNullOrEmpty(state.id) && state.id.StartsWith("Attack", StringComparison.Ordinal))
            state.category = AnimationCategory.Combat;
        else if (string.Equals(state.id, "Die", StringComparison.Ordinal))
            state.category = AnimationCategory.Death;
    }

    private void EnsureValidDefaultState()
    {
        if (!string.IsNullOrWhiteSpace(defaultState) && stateMap.ContainsKey(defaultState))
            return;

        foreach (var state in states)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.id) || state.clip == null)
                continue;

            Debug.LogWarning(
                $"Default animation state '{defaultState}' not found on {name}. Falling back to '{state.id}'.",
                this);
            defaultState = state.id;
            return;
        }

        Debug.LogError($"DynamicAnimator on {name} has no valid animation states.", this);
    }

    private void OnDestroy()
    {
        foreach (var playable in playableCache.Values)
        {
            if (playable.IsValid())
                playable.Destroy();
        }

        playableCache.Clear();

        if (graph.IsValid())
            graph.Destroy();
    }
}
