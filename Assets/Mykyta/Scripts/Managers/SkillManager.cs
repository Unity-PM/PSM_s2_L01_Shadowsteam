using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public StatComponent casterStats;
    public Transform castPoint;
    public List<AbilitySO> skills;

    private Dictionary<string, AbilitySO> skillMap = new();
    private Dictionary<string, float> cooldownTimers = new();

    private void Awake()
    {
        foreach (var skill in skills)
        {
            skillMap[skill.skillId] = skill;
            cooldownTimers[skill.skillId] = 0f;
        }
    }

    private void Update()
    {
        List<string> keys = new(cooldownTimers.Keys);

        foreach (var key in keys)
        {
            if (cooldownTimers[key] > 0)
            {
                cooldownTimers[key] -= Time.deltaTime;
                if (cooldownTimers[key] < 0)
                {
                    cooldownTimers[key] = 0;
                    EventBus.Publish(new SkillCooldownEndedEvent(key));
                }

                EventBus.Publish(new SkillCooldownEvent(key, cooldownTimers[key]));
            }
            else
            {
                EventBus.Publish(new SkillCooldownEndedEvent(key));
            }
        }
    }

    public void CastSkill(string skillId)
    {
        if (!skillMap.ContainsKey(skillId))
            return;

        AbilitySO skill = skillMap[skillId];

        if (cooldownTimers[skillId] > 0)
        {
            return;
        }

        if (casterStats.getMP() < skill.manaCost)
        {
            return;
        }

        EventBus.Publish(new StatChangeEvent(
            casterStats,
            StatType.MP,
            -skill.manaCost
        ));

        skill.Execute(casterStats, castPoint);

        cooldownTimers[skillId] = skill.cooldown;

        EventBus.Publish(new SkillCooldownEvent(skillId, cooldownTimers[skillId]));
    }

    public float GetCooldownRemaining(string skillId)
    {
        return cooldownTimers.ContainsKey(skillId)
            ? cooldownTimers[skillId]
            : 0f;
    }
}
