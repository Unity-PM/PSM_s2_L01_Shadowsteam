using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillInput : MonoBehaviour
{
    SkillManager skillManager;

    void Awake()
    {
        skillManager = GetComponent<SkillManager>();
        if (skillManager == null)
            Debug.LogError("PlayerSkillInput requires SkillManager on the same GameObject.", this);
    }

    void Update()
    {
        if (skillManager == null)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            skillManager.CastSkill("Fireball");

        if (keyboard.digit2Key.wasPressedThisFrame)
            skillManager.CastSkill("Heal");
    }
}
