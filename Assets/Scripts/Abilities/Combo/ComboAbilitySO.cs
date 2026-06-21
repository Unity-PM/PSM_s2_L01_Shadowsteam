using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCombo", menuName = "Scriptable Objects/Abilities/Combo")]
public class ComboAbilitySO : AbilitySO
{
    public List<AbilitySO> comboSteps;
    public float resetTime = 1.5f; // Time window to press the next combo button

    public override void Execute(StatComponent caster, Transform castPoint) { } // Logic handled in SkillManager
}