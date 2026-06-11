using UnityEngine;

public interface IMovementInput
{
    Vector2 MoveVector { get; }
    bool IsSprintPressed { get; }
    bool IsJumpDown { get; }
}