using UnityEngine;

public class RunModule : MovementModule
{
    public override MovementState State => MovementState.Running;

    public override bool CanEnter(MovementBrain brain) =>
        brain.Input != null && brain.Input.MoveVector.magnitude > 0.1f;

    public override void Process(MovementBrain brain)
    {
        if (brain.Input == null || brain.settings == null)
            return;

        Vector3 direction = GetDirection(brain, brain.Input.MoveVector);
        brain.RotateTowards(direction, rotationSpeed);
        brain.Controller.Move(direction * brain.settings.walkSpeed * Time.deltaTime);
    }
}
