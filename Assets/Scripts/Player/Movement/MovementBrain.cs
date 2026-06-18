using System;
using Player.Movement.Modules2;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementBrain : MonoBehaviour
{
    const float GroundStickVelocity = -2f;
    const float AirControlSharpness = 2f;

    [Header("Modules")]
    [SerializeField] MovementModule idleModule;
    [SerializeField] MovementModule walkModule;
    [SerializeField] MovementModule sprintModule;
    [SerializeField] MovementModule jumpModule;

    [Header("References")]
    [SerializeField] public MovementSettingsSO settings;
    [SerializeField] public Transform cameraTransform;
    [SerializeField] DynamicAnimator dynamicAnimator;
    [SerializeField] string jumpAnimationStateId = "Jump";

    CharacterController controller;
    IMovementInput movementInput;
    Vector3 verticalVelocity;
    Vector3 currentHorizontalVelocity;
    MovementModule activeModule;
    bool isAirborne;
    bool inputLocked;
    bool wasMovementLocked;
    float defaultStepOffset;

    public CharacterController Controller => controller;
    public IMovementInput Input => movementInput;
    public MovementState CurrentState => activeModule != null ? activeModule.State : MovementState.Idle;
    public bool IsAirborne => isAirborne;
    public bool IsGroundedForJump => !inputLocked && !IsMovementLocked && controller.isGrounded;
    public bool IsMovementLocked => dynamicAnimator != null && dynamicAnimator.IsMovementLocked;
    public float VerticalVelocityY => verticalVelocity.y;
    public event Action JumpStarted;

    public void SetVerticalVelocity(float value) => verticalVelocity.y = value;

    public void SetInputLocked(bool locked) => inputLocked = locked;

    public void StopHorizontalMovement() => currentHorizontalVelocity = Vector3.zero;

    public void StartJump()
    {
        if (settings == null || IsMovementLocked || inputLocked || !controller.isGrounded)
            return;

        isAirborne = true;
        controller.stepOffset = 0f;
        verticalVelocity.y = Mathf.Sqrt(settings.jumpHeight * -2f * settings.gravity);
        JumpStarted?.Invoke();

        if (dynamicAnimator != null
            && !string.IsNullOrEmpty(jumpAnimationStateId)
            && dynamicAnimator.CurrentStateId != jumpAnimationStateId)
            dynamicAnimator.ForcePlay(jumpAnimationStateId);
    }

    public void RotateTowards(Vector3 direction, float rotationSpeed)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            rotationSpeed * Time.deltaTime);
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        defaultStepOffset = controller.stepOffset;
        movementInput = GetComponent<IMovementInput>();

        idleModule ??= GetComponent<IdleModule>();
        walkModule ??= GetComponent<WalkModule>();
        sprintModule ??= GetComponent<SprintModule>();
        jumpModule ??= GetComponent<JumpModule>();
        dynamicAnimator ??= GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();

        if (dynamicAnimator != null && GetComponent<PlayerLocomotionAnimator>() == null)
            gameObject.AddComponent<PlayerLocomotionAnimator>();

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

        SyncAirborneState();

        if (IsMovementLocked && !wasMovementLocked)
            currentHorizontalVelocity = Vector3.zero;

        wasMovementLocked = IsMovementLocked;

        if (!inputLocked && !IsMovementLocked)
        {
            TryJump();
            SelectGroundModule();
        }
        else if (isAirborne)
            activeModule = idleModule;

        if (isAirborne)
        {
            if (!inputLocked && !IsMovementLocked && movementInput.MoveVector.sqrMagnitude > 0.04f)
            {
                Vector3 airDirection = LocomotionKit.WorldDirection(this, movementInput.MoveVector);
                float speedInAir = Mathf.Max(settings.walkSpeed, currentHorizontalVelocity.magnitude);
                Vector3 targetHorizontal = airDirection * speedInAir;
                currentHorizontalVelocity = Vector3.Lerp(
                    currentHorizontalVelocity,
                    targetHorizontal,
                    Time.deltaTime * AirControlSharpness);
            }
        }
        else
        {
            Vector3 targetHorizontal = Vector3.zero;
            if (!inputLocked && !IsMovementLocked && activeModule != null)
                targetHorizontal = activeModule.Process(this);

            currentHorizontalVelocity = targetHorizontal;
        }

        ApplyGravity();

        Vector3 finalVelocity = currentHorizontalVelocity + new Vector3(0f, verticalVelocity.y, 0f);
        controller.Move(finalVelocity * Time.deltaTime);

        if (!IsMovementLocked && currentHorizontalVelocity.sqrMagnitude > 0.01f)
            RotateTowards(currentHorizontalVelocity.normalized, settings.rotationSpeed);

        if (TryGetComponent(out StatComponent stats))
            stats.StaminaRegenPaused = CurrentState == MovementState.Sprinting;
    }

    void TryJump()
    {
        if (jumpModule == null || !jumpModule.CanEnter(this))
            return;

        jumpModule.Process(this);
    }

    void SelectGroundModule()
    {
        if (isAirborne)
        {
            activeModule = idleModule;
            return;
        }

        if (sprintModule != null && sprintModule.CanEnter(this))
            activeModule = sprintModule;
        else if (walkModule != null && walkModule.CanEnter(this))
            activeModule = walkModule;
        else
            activeModule = idleModule;
    }

    void SyncAirborneState()
    {
        bool wasAirborne = isAirborne;
        isAirborne = !controller.isGrounded;

        if (!wasAirborne && isAirborne)
            controller.stepOffset = 0f;
        else if (wasAirborne && !isAirborne)
            controller.stepOffset = defaultStepOffset;
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity.y <= 0f)
            verticalVelocity.y = GroundStickVelocity;
        else
            verticalVelocity.y += settings.gravity * Time.deltaTime;
    }
}
