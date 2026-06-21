using UnityEngine;

namespace Player.Movement.Modules2
{
    public class WalkModule : MovementModule
    {
        public override MovementState State => MovementState.Walking;

        public override bool CanEnter(MovementBrain brain) => LocomotionKit.WantsToMove(brain);

        public override Vector3 Process(MovementBrain brain)
        {
            if (brain.settings == null)
                return Vector3.zero;

            return LocomotionKit.CalculateTargetVelocity(brain, brain.settings.walkSpeed);
        }
    }
}
