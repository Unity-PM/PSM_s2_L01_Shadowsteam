public class SkillCooldownEndedEvent
{
    public string SkillId;

    public SkillCooldownEndedEvent(string skillId)
    {
        SkillId = skillId;
    }
}