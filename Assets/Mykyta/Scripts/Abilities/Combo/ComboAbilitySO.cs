using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCombo", menuName = "Scriptable Objects/Abilities/Combo")]
public class ComboAbilitySO : AbilitySO
{
    public List<AbilitySO> comboSteps;
    public float resetTime = 1.5f; // Время на нажатие следующей кнопки

    public override void Execute(StatComponent caster, Transform castPoint) { } // Логика в SkillManager
}