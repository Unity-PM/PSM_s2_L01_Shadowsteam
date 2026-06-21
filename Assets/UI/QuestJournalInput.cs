using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Opens the quest journal with J (same as navbar Quests button).
/// </summary>
public class QuestJournalInput : MonoBehaviour
{
    [SerializeField] private Key toggleKey = Key.J;

    static bool keyboardNullLogged;

    void Update()
    {
        if (Keyboard.current == null)
        {
            if (!keyboardNullLogged)
            {
                keyboardNullLogged = true;
                Debug.LogWarning("[QuestJournalInput] Keyboard.current is null (focus Game view / check Input System).", this);
            }
            return;
        }

        bool pressed = Keyboard.current.jKey.wasPressedThisFrame
            || Keyboard.current[toggleKey].wasPressedThisFrame;
        if (!pressed)
            return;

        if (UIManager.Instance == null)
        {
            Debug.LogError("[QuestJournalInput] UIManager.Instance is null.", this);
            return;
        }

        UIManager.UnlockCursorForUi();
        UIManager.Instance.ToggleQuestJournal();
    }
}
