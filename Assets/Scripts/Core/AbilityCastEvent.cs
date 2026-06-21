/// <summary>Raised by <see cref="SkillManager"/> right after an ability is successfully cast.</summary>
public class AbilityCastEvent
{
    public string AbilityId;
    public StatComponent Caster;

    public AbilityCastEvent(string abilityId, StatComponent caster)
    {
        AbilityId = abilityId;
        Caster = caster;
    }
}
