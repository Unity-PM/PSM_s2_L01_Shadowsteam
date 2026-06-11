using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class UniversalClipAnimator : MonoBehaviour
{
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
        public bool playOnlyOnPressed = true;
        public bool forcePlay;
    }

    [Header("Animation Setup")]
    [SerializeField] private string defaultState = "Idle";
    [SerializeField] private List<AnimationState> states = new();

    [Header("Input Setup")]
    [SerializeField] private List<InputAnimationBinding> inputBindings = new();

    private readonly Dictionary<string, AnimationState> stateMap = new();

    private Animator animator;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable currentPlayable;
    private AnimationClipPlayable nextPlayable;

    private int currentInput = 0;
    private int nextInput = 1;

    private AnimationState currentState;
    private float fadeTimer;
    private float currentFadeTime;
    private bool isFading;

    public string CurrentStateId => currentState?.id;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        foreach (var state in states)
        {
            if (state != null && !string.IsNullOrWhiteSpace(state.id) && state.clip != null)
                stateMap[state.id] = state;
        }

        graph = PlayableGraph.Create($"{name}_UniversalAnimator");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        mixer = AnimationMixerPlayable.Create(graph, 2);

        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        output.SetSourcePlayable(mixer);

        graph.Play();

        Play(defaultState, true);
    }

    private void OnEnable()
    {
        foreach (var binding in inputBindings)
        {
            if (binding.action == null)
                continue;

            binding.action.action.Enable();
            binding.action.action.performed += OnActionPerformed;

            if (!binding.playOnlyOnPressed)
                binding.action.action.canceled += OnActionCanceled;
        }
    }

    private void OnDisable()
    {
        foreach (var binding in inputBindings)
        {
            if (binding.action == null)
                continue;

            binding.action.action.performed -= OnActionPerformed;
            binding.action.action.canceled -= OnActionCanceled;
        }
    }

    private void Update()
    {
        HandleFade();
        HandleAutoTransition();
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        foreach (var binding in inputBindings)
        {
            if (binding.action == null)
                continue;

            if (binding.action.action != context.action)
                continue;

            if (binding.forcePlay)
                ForcePlay(binding.animationId);
            else
                Play(binding.animationId);
        }
    }

    private void OnActionCanceled(InputAction.CallbackContext context)
    {
        Play(defaultState);
    }

    public void Play(string stateId)
    {
        Play(stateId, false);
    }

    public void ForcePlay(string stateId)
    {
        Play(stateId, true);
    }

    private void Play(string stateId, bool force)
    {
        if (!stateMap.TryGetValue(stateId, out var state))
        {
            Debug.LogWarning($"Animation state not found: {stateId}", this);
            return;
        }

        if (!force && currentState != null && currentState.id == stateId)
            return;

        if (nextPlayable.IsValid())
            nextPlayable.Destroy();

        nextPlayable = AnimationClipPlayable.Create(graph, state.clip);
        nextPlayable.SetSpeed(state.speed);
        nextPlayable.SetTime(0);
        nextPlayable.SetApplyFootIK(true);

        mixer.DisconnectInput(nextInput);
        mixer.ConnectInput(nextInput, nextPlayable, 0);
        mixer.SetInputWeight(nextInput, 0f);

        currentState = state;
        fadeTimer = 0f;
        currentFadeTime = Mathf.Max(0.01f, state.fadeTime);
        isFading = true;
    }

    private void HandleFade()
    {
        if (!isFading)
            return;

        fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fadeTimer / currentFadeTime);

        mixer.SetInputWeight(currentInput, 1f - t);
        mixer.SetInputWeight(nextInput, t);

        if (t >= 1f)
        {
            if (currentPlayable.IsValid())
                currentPlayable.Destroy();

            currentPlayable = nextPlayable;

            int temp = currentInput;
            currentInput = nextInput;
            nextInput = temp;

            isFading = false;
        }
    }

    private void HandleAutoTransition()
    {
        if (currentState == null || currentState.loop)
            return;

        if (string.IsNullOrWhiteSpace(currentState.nextStateAfterFinish))
            return;

        if (!currentPlayable.IsValid())
            return;

        if (currentPlayable.GetTime() >= currentState.clip.length)
            Play(currentState.nextStateAfterFinish);
    }

    private void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}