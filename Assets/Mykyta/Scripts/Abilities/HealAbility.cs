using UnityEngine;

[CreateAssetMenu(fileName = "HealAbility", menuName = "Scriptable Objects/HealAbility")]
public class HealAbility : AbilitySO
{
    public float healAmount;

    // Исправлено: добавлена правильная сигнатура (MovementBrain brain)
    public override void Execute(StatComponent caster, Transform castPoint, MovementBrain brain)
    {
        EventBus.Publish(new StatChangeEvent(caster, StatType.HP, healAmount));
    }
}