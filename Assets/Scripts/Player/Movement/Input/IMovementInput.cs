using UnityEngine;

public interface IMovementInput
{
    Vector2 MoveVector { get; }
    bool IsRunPressed { get; }
    bool IsJumpDown { get; }
}