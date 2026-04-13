using UnityEngine;
[CreateAssetMenu(fileName = "HealAbility", menuName = "Scriptable Objects/HealAbility")]
public class HealAbility : AbilitySO
{
    public float healAmount;

    public override void Execute(StatComponent caster, Transform castPoint)
    {
        EventBus.Publish(new StatChangeEvent(
            caster,
            StatType.HP,
            healAmount
        ));
    }
}
