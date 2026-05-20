using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerKeyboardInput : MonoBehaviour, IMovementInput
{
    public Vector2 MoveVector => Keyboard.current != null ?
        new Vector2(Keyboard.current.dKey.ReadValue() - Keyboard.current.aKey.ReadValue(),
                    Keyboard.current.wKey.ReadValue() - Keyboard.current.sKey.ReadValue()) : Vector2.zero;

    public bool IsRunPressed => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    public bool IsJumpDown => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
}