// --- FILE SkillManager.cs ---
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public StatComponent casterStats;
    public Transform castPoint;
    public List<ComboAbilitySO> combos; // Список доступных комбо

    private MovementBrain movementBrain;
    private Dictionary<string, float> comboCooldowns = new();
    private Dictionary<string, int> currentStepIndex = new();
    private Dictionary<string, float> lastStepTime = new();

    private void Awake()
    {
        movementBrain = GetComponent<MovementBrain>();
        foreach (var combo in combos)
        {
            comboCooldowns[combo.skillId] = 0f;
            currentStepIndex[combo.skillId] = -1; // -1 = комбо не начато
        }
    }

    private void Update()
    {
        UpdateCooldowns();
        CheckComboExpirations();
        CheckInterruption();
    }

    private void UpdateCooldowns()
    {
        foreach (var combo in combos)
        {
            string id = combo.skillId;
            if (comboCooldowns[id] > 0)
            {
                comboCooldowns[id] -= Time.deltaTime;
                // Кадр за кадром обновляем UI через шину событий
                EventBus.Publish(new SkillCooldownEvent(id, comboCooldowns[id]));
            }
        }
    }

    private void CheckComboExpirations()
    {
        foreach (var combo in combos)
        {
            int index = currentStepIndex[combo.skillId];
            if (index == -1) continue;

            // Если время с последнего удара превысило допустимое окно
            if (Time.time - lastStepTime[combo.skillId] > combo.steps[index].windowToNext)
            { ResetCombo(combo.skillId, false); }
        }
    }

    private void CheckInterruption()
    {
        // Прерываем комбо при прыжке или полете (Пункт 3)
        if (movementBrain.CurrentState == MovementState.Airborne ||
            movementBrain.CurrentState == MovementState.Gliding)
        {
            foreach (var combo in combos)
            { if (currentStepIndex[combo.skillId] != -1) ResetCombo(combo.skillId, false); }
        }
    }

    public void TryExecuteCombo(int slotIndex)
    {
        if (slotIndex >= combos.Count) return;
        ComboAbilitySO combo = combos[slotIndex];

        if (comboCooldowns[combo.skillId] > 0) return;

        int currentIndex = currentStepIndex[combo.skillId];

        // ЕСЛИ КОМБО ЕЩЕ НЕ НАЧАТО
        if (currentIndex == -1)
        {
            ExecuteStep(combo, 0);
            return;
        }

        // ЕСЛИ ПРОДОЛЖАЕМ ЦЕПОЧКУ
        float timeSinceLast = Time.time - lastStepTime[combo.skillId];
        var currentStep = combo.steps[currentIndex];

        // Проверка: не слишком ли рано? (minDelayBeforeNext — Пункт 3)
        if (timeSinceLast < currentStep.minDelayBeforeNext) return;

        // Проверка: не слишком ли поздно?
        if (timeSinceLast > currentStep.windowToNext) return;

        ExecuteStep(combo, currentIndex + 1);
    }

    private void ExecuteStep(ComboAbilitySO combo, int index)
    {
        if (index >= combo.steps.Count) return;

        var step = combo.steps[index];
        if (casterStats.getMP() < step.ability.manaCost) return;
        if (!step.ability.allowedStates.Contains(movementBrain.CurrentState)) return;

        step.ability.Execute(casterStats, castPoint, movementBrain);
        EventBus.Publish(new StatChangeEvent(casterStats, StatType.MP, -step.ability.manaCost));

        lastStepTime[combo.skillId] = Time.time;
        currentStepIndex[combo.skillId] = index;

        // Если это финальный удар — запускаем общий КД
        if (index == combo.steps.Count - 1)
        { ResetCombo(combo.skillId, true); }
    }

    public void ResetCombo(string skillId, bool applyCooldown)
    {
        if (applyCooldown)
        {
            var combo = combos.Find(c => c.skillId == skillId);
            comboCooldowns[skillId] = combo.finalCooldown;
        }
        currentStepIndex[skillId] = -1;
    }

    public float GetComboMaxCooldown(string skillId)
    {
        var combo = combos.Find(c => c.skillId == skillId);
        return combo != null ? combo.finalCooldown : 1f;
    }
}