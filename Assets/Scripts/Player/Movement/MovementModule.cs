using UnityEngine;

public abstract class MovementModule : MonoBehaviour
{
    public abstract MovementState State { get; }
    public abstract bool CanEnter(MovementBrain brain);
    public abstract Vector3 Process(MovementBrain brain);
}
