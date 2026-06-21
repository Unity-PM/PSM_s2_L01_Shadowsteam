using UnityEngine;

namespace Player.Movement.Modules2
{
    public class JumpModule : MovementModule
    {
        public override MovementState State => MovementState.Airborne;

        public override bool CanEnter(MovementBrain brain) =>
            brain.IsGroundedForJump
            && brain.Input != null
            && brain.Input.IsJumpDown
            && LocomotionKit.HasStaminaFor(
                brain,
                brain.settings != null ? brain.settings.jumpStaminaCost : 0f);

        public override Vector3 Process(MovementBrain brain)
        {
            float cost = brain.settings != null ? brain.settings.jumpStaminaCost : 0f;
            LocomotionKit.SpendStaminaFlat(LocomotionKit.Stats(brain), cost);
            brain.StartJump();
            return Vector3.zero;
        }
    }
}
