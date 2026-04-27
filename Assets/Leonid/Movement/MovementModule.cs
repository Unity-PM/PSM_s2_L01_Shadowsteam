using UnityEngine;

public abstract class MovementModule : MonoBehaviour
{
    [SerializeField] protected float rotationSpeed = 10f;
    public abstract MovementState State { get; }
    public abstract bool CanEnter(MovementBrain brain);
    public abstract void Process(MovementBrain brain);

    protected Vector3 GetDirection(MovementBrain brain, Vector2 input)
    {
        if (brain.gameObject.CompareTag("Player") && brain.cameraTransform != null)
        {
            Vector3 forward = brain.cameraTransform.forward;
            Vector3 right = brain.cameraTransform.right;
            forward.y = 0; right.y = 0;
            return (forward.normalized * input.y + right.normalized * input.x).normalized;
        }
        return new Vector3(input.x, 0, input.y).normalized;
    }
}