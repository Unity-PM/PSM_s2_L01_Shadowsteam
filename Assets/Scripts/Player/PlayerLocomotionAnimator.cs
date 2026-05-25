using UnityEngine;

/// <summary>
/// Drives Idle/Walk/Run clips from sustained movement, not brief input taps.
/// </summary>
[DefaultExecutionOrder(50)]
public class PlayerLocomotionAnimator : MonoBehaviour {
    [SerializeField] MovementBrain movementBrain;
    [SerializeField] DynamicAnimator dynamicAnimator;
    [SerializeField] string idleAnimationStateId = "Idle";
    [SerializeField] string walkAnimationStateId = "Walk";
    [SerializeField] string walkBackAnimationStateId = "WalkBack";
    [SerializeField] string runAnimationStateId = "Run";
    [SerializeField] float minMoveSpeed = 0.15f;
    [SerializeField] float locomotionStartDelay = 0.00f;
    [SerializeField] float walkBackDotThreshold = -0.35f;

    string currentLocomotionId;
    Vector3 lastPosition;
    float sustainedMoveTime;

    void Awake() {
        if (movementBrain == null)
            movementBrain = GetComponent<MovementBrain>();

        if (dynamicAnimator == null)
            dynamicAnimator = GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();

        lastPosition = transform.position;
    }

    void LateUpdate() {
        SyncLocomotion();
        lastPosition = transform.position;
    }

    void SyncLocomotion() {
        if (dynamicAnimator == null || movementBrain == null)
            return;

        if (dynamicAnimator.IsMovementLocked || movementBrain.IsAirborne) {
            sustainedMoveTime = 0f;
            currentLocomotionId = null;
            return;
        }

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        float speed = Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;

        bool hasMoveInput = HasMoveInput();
        bool isActuallyMoving = speed >= minMoveSpeed;

        if (hasMoveInput && isActuallyMoving)
            sustainedMoveTime += Time.deltaTime;
        else
            sustainedMoveTime = 0f;

        string nextId = idleAnimationStateId;
        if (hasMoveInput && isActuallyMoving && sustainedMoveTime >= locomotionStartDelay)
            nextId = ResolveMovingState(delta);

        if (string.Equals(currentLocomotionId, nextId, System.StringComparison.Ordinal))
            return;

        currentLocomotionId = nextId;

        if (string.Equals(nextId, idleAnimationStateId, System.StringComparison.Ordinal))
            dynamicAnimator.ForcePlay(nextId);
        else
            dynamicAnimator.Play(nextId);
    }

    bool HasMoveInput() =>
        movementBrain.Input != null && movementBrain.Input.MoveVector.sqrMagnitude > 0.04f;

    string ResolveMovingState(Vector3 delta) {
        MovementState state = movementBrain.CurrentState;

        if (state == MovementState.Sprinting || state == MovementState.Running)
            return runAnimationStateId;

        if (delta.sqrMagnitude > 0.0001f) {
            float forwardDot = Vector3.Dot(transform.forward, delta.normalized);
            if (forwardDot <= walkBackDotThreshold
                && !string.IsNullOrEmpty(walkBackAnimationStateId))
                return walkBackAnimationStateId;
        }

        return walkAnimationStateId;
    }
}
