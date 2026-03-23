using UnityEngine;

public abstract class AbilitySO : ScriptableObject
{
    public string skillId;
    public float manaCost;
    public float cooldown;

    public abstract void Execute(StatComponent caster, Transform castPoint);
}
