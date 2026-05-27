using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Listens for the gameplay HUD toggle key (F1 by default).
/// </summary>
public class GameplayHudVisibilityInput : MonoBehaviour
{
    [SerializeField] private Key toggleKey = Key.F1;

    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
            GameplayHudVisibility.Toggle();
    }
}
