using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementBrain : MonoBehaviour
{
    [Header("Modules")]
    [SerializeField]
    protected MovementModule idleModule;
    [SerializeField]
    protected MovementModule walkModule;
    [SerializeField]
    protected MovementModule runModule;
    [SerializeField]
    protected MovementModule sprintModule;
    [SerializeField]
    protected MovementModule jumpModule;

    [Header("References")]
    [SerializeField]
    public MovementSettingsSO settings;
    [SerializeField]
    public Transform cameraTransform;
    [SerializeField]
    DynamicAnimator dynamicAnimator;
    [SerializeField] string jumpAnimationStateId = "Jump";

    CharacterController controller;
    IMovementInput movementInput;
    Vector3 verticalVelocity;
    Vector3 jumpHorizontalVelocity;
    MovementModule activeModule;
    bool wasGrounded = true;
    bool isAirborne;
    bool inputLocked;
    bool wasMovementLocked;

    public CharacterController Controller => controller;
    public IMovementInput Input => movementInput;
    public MovementState CurrentState => activeModule != null ? activeModule.State : MovementState.Idle;
    public bool IsAirborne => isAirborne;
    public bool IsGroundedForJump => !inputLocked && !IsMovementLocked && !isAirborne && IsFirmlyGrounded();
    public bool IsMovementLocked => dynamicAnimator != null && dynamicAnimator.IsMovementLocked;
    public float VerticalVelocityY => verticalVelocity.y;
    public event Action JumpStarted;

    public void SetVerticalVelocity(float val) => verticalVelocity.y = val;

    public void SetInputLocked(bool locked) => inputLocked = locked;

    public void StopHorizontalMovement() => jumpHorizontalVelocity = Vector3.zero;

    public void StartJump(Vector3 horizontalVelocity)
    {
        if (settings == null || isAirborne || IsMovementLocked || inputLocked)
            return;

        isAirborne = true;
        jumpHorizontalVelocity = horizontalVelocity;
        SetVerticalVelocity(Mathf.Sqrt(settings.jumpHeight * -2f * settings.gravity));
        JumpStarted?.Invoke();

        if (dynamicAnimator != null && !string.IsNullOrEmpty(jumpAnimationStateId))
            dynamicAnimator.ForcePlay(jumpAnimationStateId);
    }

    void TryProcessJumpInput() {
        if (inputLocked || jumpModule == null)
            return;

        if (IsGroundedForJump && jumpModule.CanEnter(this))
            jumpModule.Process(this);
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        movementInput = GetComponent<IMovementInput>();

        if (dynamicAnimator == null)
            dynamicAnimator = GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();

        if (dynamicAnimator != null && GetComponent<PlayerLocomotionAnimator>() == null)
            gameObject.AddComponent<PlayerLocomotionAnimator>();

        if (walkModule == null)
            walkModule = GetComponent<WalkModule>();

        if (walkModule == null)
            walkModule = gameObject.AddComponent<WalkModule>();

        if (settings == null)
            Debug.LogError("MovementBrain requires MovementSettingsSO.", this);

        if (movementInput == null)
            Debug.LogError("MovementBrain requires a component implementing IMovementInput.", this);

        if (gameObject.CompareTag("Player") && cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (movementInput == null || settings == null)
            return;

        TryProcessJumpInput();

        if (IsMovementLocked && !wasMovementLocked)
            StopHorizontalMovement();

        wasMovementLocked = IsMovementLocked;

        UpdateAirborneState();

        if (!inputLocked && !IsMovementLocked)
            UpdateActiveModule();
        else if (isAirborne)
            activeModule = idleModule;

        if (!inputLocked && !IsMovementLocked && activeModule != null)
            activeModule.Process(this);

        if (!IsMovementLocked)
            ApplyJumpMomentum();

        ApplyGravity();
        UpdateAirborneState();

        if (TryGetComponent(out StatComponent stats))
        {
            stats.StaminaRegenPaused = CurrentState == MovementState.Running
                || CurrentState == MovementState.Sprinting;
        }
    }

    void UpdateAirborneState()
    {
        if (controller.isGrounded && verticalVelocity.y <= 0.05f)
            isAirborne = false;
        else
            isAirborne = true;
    }

    bool IsFirmlyGrounded() =>
        controller.isGrounded && verticalVelocity.y <= 0.05f;

    void UpdateActiveModule()
    {
        if (isAirborne)
        {
            activeModule = idleModule;
            wasGrounded = false;
            return;
        }

        if (!wasGrounded)
            jumpHorizontalVelocity = Vector3.zero;

        wasGrounded = true;

        if (sprintModule != null && sprintModule.CanEnter(this))
            activeModule = sprintModule;
        else if (runModule != null && runModule.CanEnter(this))
            activeModule = runModule;
        else if (walkModule != null && walkModule.CanEnter(this))
            activeModule = walkModule;
        else
            activeModule = idleModule;
    }

    void ApplyJumpMomentum()
    {
        if (!isAirborne || jumpHorizontalVelocity.sqrMagnitude <= 0.0001f)
            return;

        controller.Move(jumpHorizontalVelocity * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (IsFirmlyGrounded() && verticalVelocity.y < 0)
            verticalVelocity.y = -2f;

        verticalVelocity.y += settings.gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    public void RotateTowards(Vector3 direction, float rotSpeed)
    {
        if (direction.magnitude < 0.1f)
            return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            rotSpeed * Time.deltaTime);
    }
}
