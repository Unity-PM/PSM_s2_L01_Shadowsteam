using UnityEngine;

public class SprintModule : MovementModule
{
    public override MovementState State => MovementState.Sprinting;

    public override bool CanEnter(MovementBrain brain)
    {
        if (brain.Input == null || brain.settings == null)
            return false;

        if (!brain.Input.IsRunPressed || brain.Input.MoveVector.sqrMagnitude < 0.04f)
            return false;

        StatComponent stats = brain.GetComponent<StatComponent>();
        if (stats == null)
            return true;

        return !stats.IsStaminaExhausted && stats.getStamina() > 0f;
    }

    public override void Process(MovementBrain brain)
    {
        StatComponent stats = brain.GetComponent<StatComponent>();
        if (brain.Input == null || brain.settings == null || stats == null)
            return;

        Vector3 dir = GetDirection(brain, brain.Input.MoveVector);
        brain.RotateTowards(dir, rotationSpeed);
        brain.Controller.Move(dir * brain.settings.sprintSpeed * Time.deltaTime);

        if (brain.settings.staminaDrainPerSecond <= 0f || stats.getStamina() <= 0f)
            return;

        EventBus.Publish(new StatChangeEvent(
            stats,
            StatType.Stamina,
            -brain.settings.staminaDrainPerSecond * Time.deltaTime));
    }
}
