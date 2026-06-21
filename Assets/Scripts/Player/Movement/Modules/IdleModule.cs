using UnityEngine;

namespace Player.Movement.Modules2
{
    public class IdleModule : MovementModule
    {
        public override MovementState State => MovementState.Idle;

        public override bool CanEnter(MovementBrain brain) => true;

        public override Vector3 Process(MovementBrain brain) => Vector3.zero;
    }
}
