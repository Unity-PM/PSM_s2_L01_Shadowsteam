using UnityEngine;

public class WalkModule : MovementModule
{
    public override MovementState State => MovementState.Walking;

    public override bool CanEnter(MovementBrain brain) =>
        brain.Input != null && brain.Input.MoveVector.sqrMagnitude > 0.04f;

    public override void Process(MovementBrain brain)
    {
        if (brain.Input == null || brain.settings == null)
            return;

        Vector3 dir = GetDirection(brain, brain.Input.MoveVector);
        brain.RotateTowards(dir, rotationSpeed);
        brain.Controller.Move(dir * brain.settings.walkSpeed * Time.deltaTime);
    }
}
