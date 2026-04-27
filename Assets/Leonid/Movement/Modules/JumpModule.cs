using UnityEngine;
public class JumpModule : MovementModule
{
    public override MovementState State => MovementState.Airborne;
    public override bool CanEnter(MovementBrain brain) => brain.Controller.isGrounded && brain.Input.IsJumpDown;
    public override void Process(MovementBrain brain)
    {
        brain.SetVerticalVelocity(Mathf.Sqrt(brain.settings.jumpHeight * -2f * brain.settings.gravity));
    }
}