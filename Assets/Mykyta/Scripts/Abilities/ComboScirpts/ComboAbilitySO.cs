// --- FILE ComboAbilitySO.cs ---
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCombo", menuName = "Scriptable Objects/Abilities/Combo")]
public class ComboAbilitySO : AbilitySO
{
    [System.Serializable]
    public struct ComboStep
    {
        public AbilitySO ability;
        public float minDelayBeforeNext;
        public float windowToNext;
    }

    public List<ComboStep> steps;
    public float finalCooldown;

    public override void Execute(StatComponent caster, Transform castPoint, MovementBrain brain) { }
}