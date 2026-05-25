using UnityEngine;
using System.Collections.Generic;

public abstract class AbilitySO : ScriptableObject
{
    public string skillId;
    public float manaCost;
    public float cooldown;
    public List<MovementState> allowedStates = new List<MovementState> { MovementState.Idle, MovementState.Running, MovementState.Sprinting };

    public abstract void Execute(StatComponent caster, Transform castPoint);
}