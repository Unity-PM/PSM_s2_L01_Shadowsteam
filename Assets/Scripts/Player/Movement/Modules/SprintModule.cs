using UnityEngine;

namespace Player.Movement.Modules2
{
    public class SprintModule : MovementModule
    {
        public override MovementState State => MovementState.Sprinting;

        public override bool CanEnter(MovementBrain brain) =>
            LocomotionKit.WantsSprint(brain)
            && LocomotionKit.WantsToMove(brain)
            && LocomotionKit.CanSpendStamina(brain);

        public override Vector3 Process(MovementBrain brain)
        {
            if (brain.settings == null)
                return Vector3.zero;

            LocomotionKit.SpendStaminaPerSecond(LocomotionKit.Stats(brain), brain.settings.staminaDrainPerSecond);
            return LocomotionKit.CalculateTargetVelocity(brain, brain.settings.sprintSpeed);
        }
    }
}
