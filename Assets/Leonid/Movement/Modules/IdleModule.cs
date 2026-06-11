using UnityEngine;
public class IdleModule : MovementModule
{
    public override MovementState State => MovementState.Idle;
    public override bool CanEnter(MovementBrain brain) => true;
    public override void Process(MovementBrain brain) { }
}