using System.Collections.Generic;
using UnityEngine;

public class StatComponent : MonoBehaviour
{
    public StatSO baseStatsTemplate;

    [SerializeField]
    private float currentHP;
    [SerializeField]
    private float currentMP;
    [SerializeField]
    private float currentStamina;

    private bool isStaminaExhausted;

    public bool StaminaRegenPaused { get; set; }


    private Dictionary<StatType, float> modifiers = new Dictionary<StatType, float>();


    public float getHP() => currentHP;
    public float getMP() => currentMP;
    public float getStamina() => currentStamina;
    public bool IsStaminaExhausted => isStaminaExhausted;

    public float getMaxHP() => baseStatsTemplate.MaxHP + getModifier(StatType.HP);
    public float getMaxMP() => baseStatsTemplate.MaxMP + getModifier(StatType.MP);
    public float getMaxStamina() => baseStatsTemplate.MaxStamina + getModifier(StatType.Stamina);


    private void Start()
    {
        foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
            modifiers[type] = 0f;

        currentHP = getMaxHP();
        currentMP = getMaxMP();
        currentStamina = getMaxStamina();

        EventBus.Subscribe<StatChangeEvent>(OnStatChangeRequested);
        EventBus.Subscribe<StatModifierEvent>(OnModifier);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<StatChangeEvent>(OnStatChangeRequested);
        EventBus.Unsubscribe<StatModifierEvent>(OnModifier);
    }

    private void Update()
    {
        RegenerateStats();
    }


    private void OnStatChangeRequested(StatChangeEvent e)
    {
        if (e.target != this) return;

        switch (e.statType)
        {
            case StatType.HP:
                currentHP = Mathf.Clamp(
                    currentHP + e.amount,
                    0,
                    getMaxHP()
                );

                if (currentHP <= 0)
                    EventBus.Publish(new DeathEvent(this));
                break;

            case StatType.MP:
                currentMP = Mathf.Clamp(
                    currentMP + e.amount,
                    0,
                    getMaxMP()
                );
                break;

            case StatType.Stamina:
                currentStamina = Mathf.Clamp(currentStamina + e.amount, 0, getMaxStamina());
                if (currentStamina <= 0) isStaminaExhausted = true;
                break;

        }

        EventBus.Publish(new StatUpdatedEvent(this));
    }

    private void OnModifier(StatModifierEvent e)
    {
        if (e.target != this) return;

        ApplyModifier(e.statType, e.value);
    }


    private void ApplyModifier(StatType type, float value)
    {
        modifiers[type] += value;

        switch (type)
        {
            case StatType.HP:
                currentHP += value;
                break;

            case StatType.MP:
                currentMP += value;
                break;

            case StatType.Stamina:
                currentStamina += value;
                break;
        }

        currentHP = Mathf.Clamp(currentHP, 0, getMaxHP());
        currentMP = Mathf.Clamp(currentMP, 0, getMaxMP());
        currentStamina = Mathf.Clamp(currentStamina, 0, getMaxStamina());

        EventBus.Publish(new StatUpdatedEvent(this));
    }

    private float getModifier(StatType type)
    {
        return modifiers.ContainsKey(type) ? modifiers[type] : 0f;
    }


    private void RegenerateStats()
    {
        currentHP = Mathf.Min(currentHP + baseStatsTemplate.HPRegen * Time.deltaTime, getMaxHP());
        currentMP = Mathf.Min(currentMP + baseStatsTemplate.MPRegen * Time.deltaTime, getMaxMP());

        // Регеним стамину только если нет паузы
        if (!StaminaRegenPaused)
        {
            currentStamina = Mathf.Min(currentStamina + baseStatsTemplate.StaminaRegen * Time.deltaTime, getMaxStamina());

            // Логика из прошлого шага: если мы восстановили достаточно, снимаем истощение
            if (isStaminaExhausted && currentStamina >= getMaxStamina() / 6f) isStaminaExhausted = false;
        }

        EventBus.Publish(new StatUpdatedEvent(this));
    }

}
