using UnityEngine;
public class RunModule : MovementModule
{
    public override MovementState State => MovementState.Running;
    public override bool CanEnter(MovementBrain brain) => brain.Input.MoveVector.magnitude > 0.1f;
    public override void Process(MovementBrain brain)
    {
        Vector3 dir = GetDirection(brain, brain.Input.MoveVector);
        brain.RotateTowards(dir, rotationSpeed);
        brain.Controller.Move(dir * brain.settings.runSpeed * Time.deltaTime);
    }
}