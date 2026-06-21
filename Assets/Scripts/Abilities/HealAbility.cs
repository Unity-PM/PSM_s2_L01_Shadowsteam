using UnityEngine;
[CreateAssetMenu(fileName = "HealAbility", menuName = "Scriptable Objects/HealAbility")]
public class HealAbility : AbilitySO
{
    public float healAmount;
    [SerializeField] float magicStatMultiplier = 1f;

    public override void Execute(StatComponent caster, Transform castPoint)
    {
        float magicBonus = caster != null
            ? Mathf.Max(0f, caster.GetEffectiveStat(StatType.MAG)) * Mathf.Max(0f, magicStatMultiplier)
            : 0f;

        EventBus.Publish(new StatChangeEvent(
            caster,
            StatType.HP,
            Mathf.Max(0f, healAmount + magicBonus)
        ));
    }
}
