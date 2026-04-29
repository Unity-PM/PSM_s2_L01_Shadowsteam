using UnityEngine;
public class SprintModule : MovementModule
{
    public override MovementState State => MovementState.Sprinting;
    public override bool CanEnter(MovementBrain brain)
    {
        if (brain.CurrentState == MovementState.Gliding) return false;

        var stats = brain.GetComponent<StatComponent>();
        bool hasInput = brain.Input.MoveVector.magnitude > 0.1f;

        // Добавляем проверку на IsStaminaExhausted
        return brain.Input.IsSprintPressed && hasInput && !stats.IsStaminaExhausted && stats.getStamina() > 0;
    }
    public override void Process(MovementBrain brain)
    {
        Vector3 dir = GetDirection(brain, brain.Input.MoveVector);
        brain.RotateTowards(dir, rotationSpeed);
        brain.Controller.Move(dir * brain.settings.sprintSpeed * Time.deltaTime);
        EventBus.Publish(new StatChangeEvent(brain.GetComponent<StatComponent>(), StatType.Stamina, -brain.settings.staminaDrainPerSecond * Time.deltaTime));
    }
}