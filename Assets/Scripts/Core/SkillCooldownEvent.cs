public class SkillCooldownEvent
{
    public string SkillId;
    public float CooldownRemaining;

    public SkillCooldownEvent(string skillId, float cooldownRemaining)
    {
        SkillId = skillId;
        CooldownRemaining = cooldownRemaining;
    }
}