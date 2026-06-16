using UnityEngine;

public class GlideModule : MovementModule
{
    public override MovementState State => MovementState.Gliding;

    public override bool CanEnter(MovementBrain brain) => brain.IsAirborne;

    public override void Process(MovementBrain brain)
    {
        if (brain.Input == null || brain.settings == null)
            return;

        Vector3 direction = GetDirection(brain, brain.Input.MoveVector);
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        brain.RotateTowards(direction, rotationSpeed);
        brain.Controller.Move(direction * brain.settings.walkSpeed * Time.deltaTime);
    }
}
