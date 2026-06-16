using UnityEngine;

public class JumpModule : MovementModule
{
    public override MovementState State => MovementState.Airborne;

    public override bool CanEnter(MovementBrain brain)
    {
        if (!brain.IsGroundedForJump || brain.Input == null || !brain.Input.IsJumpDown)
            return false;

        StatComponent stats = brain.GetComponent<StatComponent>();
        if (stats == null || brain.settings == null)
            return true;

        return !stats.IsStaminaExhausted
            && stats.getStamina() >= brain.settings.jumpStaminaCost;
    }

    public override void Process(MovementBrain brain)
    {
        if (!brain.IsGroundedForJump)
            return;

        StatComponent stats = brain.GetComponent<StatComponent>();
        if (stats != null && brain.settings != null && brain.settings.jumpStaminaCost > 0f)
        {
            EventBus.Publish(new StatChangeEvent(
                stats,
                StatType.Stamina,
                -brain.settings.jumpStaminaCost));
        }

        brain.StartJump(ResolveJumpMomentum(brain));
    }

    static Vector3 ResolveJumpMomentum(MovementBrain brain)
    {
        if (brain.Input == null || brain.settings == null)
            return Vector3.zero;

        Vector2 input = brain.Input.MoveVector;
        if (input.magnitude < 0.1f)
            return Vector3.zero;

        float speed = ResolveMoveSpeed(brain);
        Vector3 direction = GetDirectionStatic(brain, input);
        return direction * speed;
    }

    static float ResolveMoveSpeed(MovementBrain brain)
    {
        StatComponent stats = brain.GetComponent<StatComponent>();
        bool canRun = stats == null
            || (!stats.IsStaminaExhausted && stats.getStamina() > 0f);

        if (brain.Input.IsRunPressed && canRun)
            return brain.settings.sprintSpeed;

        return brain.settings.walkSpeed;
    }

    static Vector3 GetDirectionStatic(MovementBrain brain, Vector2 input)
    {
        if (brain.gameObject.CompareTag("Player") && brain.cameraTransform != null)
        {
            Vector3 forward = brain.cameraTransform.forward;
            Vector3 right = brain.cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            return (forward.normalized * input.y + right.normalized * input.x).normalized;
        }

        return new Vector3(input.x, 0f, input.y).normalized;
    }
}
