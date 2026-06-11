// --- FILE PlayerSkillInput.cs ---
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillInput : MonoBehaviour
{
    private SkillManager skillManager;
    private void Awake() { skillManager = GetComponent<SkillManager>(); }

    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) skillManager.TryExecuteCombo(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) skillManager.TryExecuteCombo(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) skillManager.TryExecuteCombo(2);
    }
}