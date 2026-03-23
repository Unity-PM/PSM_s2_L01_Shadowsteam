using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillInput : MonoBehaviour
{
    private SkillManager skillManager;
    private void Awake()
    {
        skillManager = gameObject.GetComponent<SkillManager>();
    }

    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            skillManager.CastSkill("Fireball");

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            skillManager.CastSkill("Heal");
    }
}
