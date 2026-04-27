using UnityEngine;
public class SprintModule : MovementModule
{
    public override MovementState State => MovementState.Sprinting;
    public override bool CanEnter(MovementBrain brain)
    {
        if (brain.CurrentState == MovementState.Gliding) return false;

        // если нажата кнопка и есть движение — мы можем спринтить (даже в воздухе)
        bool hasInput = brain.Input.MoveVector.magnitude > 0.1f;
        bool hasStamina = brain.GetComponent<StatComponent>().getStamina() > 0;

        return brain.Input.IsSprintPressed && hasInput && hasStamina;
    }
    public override void Process(MovementBrain brain)
    {
        Vector3 dir = GetDirection(brain, brain.Input.MoveVector);
        brain.RotateTowards(dir, rotationSpeed);
        brain.Controller.Move(dir * brain.settings.sprintSpeed * Time.deltaTime);
        EventBus.Publish(new StatChangeEvent(brain.GetComponent<StatComponent>(), StatType.Stamina, -brain.settings.staminaDrainPerSecond * Time.deltaTime));
    }
}