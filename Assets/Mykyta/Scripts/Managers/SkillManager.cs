using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public StatComponent casterStats;
    public Transform castPoint;
    public List<AbilitySO> skills;

    private MovementBrain movementBrain;
    private Dictionary<string, AbilitySO> skillMap = new();
    private Dictionary<string, float> cooldownTimers = new();
    private Dictionary<string, int> comboIndices = new();
    private Dictionary<string, float> comboResetTimers = new();

    private void Awake()
    {
        movementBrain = GetComponent<MovementBrain>();
        foreach (var skill in skills)
        {
            skillMap[skill.skillId] = skill;
            cooldownTimers[skill.skillId] = 0f;
            if (skill is ComboAbilitySO) comboIndices[skill.skillId] = 0;
        }
    }

    private void Update()
    {
        UpdateTimers();
    }

    private void UpdateTimers()
    {
        List<string> keys = new(cooldownTimers.Keys);
        foreach (var key in keys)
        {
            if (cooldownTimers[key] > 0)
            {
                cooldownTimers[key] -= Time.deltaTime;
                if (cooldownTimers[key] <= 0) EventBus.Publish(new SkillCooldownEndedEvent(key));
                EventBus.Publish(new SkillCooldownEvent(key, cooldownTimers[key]));
            }
            if (comboResetTimers.ContainsKey(key) && comboResetTimers[key] > 0)
            {
                comboResetTimers[key] -= Time.deltaTime;
                if (comboResetTimers[key] <= 0) comboIndices[key] = 0;
            }
        }
    }

    public void CastSkill(string skillId)
    {
        if (!skillMap.TryGetValue(skillId, out AbilitySO skill)) return;
        if (cooldownTimers[skillId] > 0 || casterStats.getMP() < skill.manaCost) return;

        // Этап 2: Проверка состояния движения
        if (!skill.allowedStates.Contains(movementBrain.CurrentState)) return;

        ExecuteAbilityLogic(skill);

        cooldownTimers[skillId] = skill.cooldown;
        EventBus.Publish(new StatChangeEvent(casterStats, StatType.MP, -skill.manaCost));
    }

    private void ExecuteAbilityLogic(AbilitySO skill)
    {
        if (skill is ComboAbilitySO combo)
        {
            int index = comboIndices[skill.skillId];
            combo.comboSteps[index].Execute(casterStats, castPoint);

            comboIndices[skill.skillId] = (index + 1) % combo.comboSteps.Count;
            comboResetTimers[skill.skillId] = combo.resetTime;
        }
        else
        {
            skill.Execute(casterStats, castPoint);
        }
    }

    public float GetCooldownRemaining(string skillId) => cooldownTimers.GetValueOrDefault(skillId, 0f);
}