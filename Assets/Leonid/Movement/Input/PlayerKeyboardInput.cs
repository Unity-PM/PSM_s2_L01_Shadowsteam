using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerKeyboardInput : MonoBehaviour, IMovementInput
{
#if ENABLE_INPUT_SYSTEM
    [SerializeField] InputActionReference jumpAction;
    [SerializeField] InputActionReference sprintAction;
#endif

    public Vector2 MoveVector => Keyboard.current != null ?
        new Vector2(Keyboard.current.dKey.ReadValue() - Keyboard.current.aKey.ReadValue(),
                    Keyboard.current.wKey.ReadValue() - Keyboard.current.sKey.ReadValue()) : Vector2.zero;

    public bool IsRunPressed {
        get {
#if ENABLE_INPUT_SYSTEM
            if (sprintAction != null && sprintAction.action != null)
                return sprintAction.action.IsPressed();
#endif
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        }
    }

    public bool IsJumpDown {
        get {
#if ENABLE_INPUT_SYSTEM
            if (jumpAction != null && jumpAction.action != null)
                return jumpAction.action.WasPerformedThisFrame();
#endif
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        }
    }

    void OnEnable() {
#if ENABLE_INPUT_SYSTEM
        jumpAction?.action?.Enable();
        sprintAction?.action?.Enable();
#endif
    }

    void OnDisable() {
#if ENABLE_INPUT_SYSTEM
        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.Disable();
        if (sprintAction != null && sprintAction.action != null)
            sprintAction.action.Disable();
#endif
    }
}
