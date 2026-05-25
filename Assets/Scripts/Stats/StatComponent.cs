using System.Collections.Generic;
using UnityEngine;

public class StatComponent : MonoBehaviour
{
    public StatSO baseStatsTemplate;

    private float currentHP;
    private float currentMP;
    private float currentStamina;

    private bool isStaminaExhausted;
    private bool isDead;

    public bool StaminaRegenPaused { get; set; }
    public bool HPRegenPaused { get; set; }
    public bool IsDead => isDead;


    private Dictionary<StatType, float> modifiers = new Dictionary<StatType, float>();


    public float getHP() => currentHP;
    public float getMP() => currentMP;
    public float getStamina() => currentStamina;
    public bool IsStaminaExhausted => isStaminaExhausted;

    public float getMaxHP() => baseStatsTemplate != null ? baseStatsTemplate.MaxHP + getModifier(StatType.HP) : 0f;
    public float getMaxMP() => baseStatsTemplate != null ? baseStatsTemplate.MaxMP + getModifier(StatType.MP) : 0f;
    public float getMaxStamina() => baseStatsTemplate != null ? baseStatsTemplate.MaxStamina + getModifier(StatType.Stamina) : 0f;

    private void EnsureModifiersInitialized()
    {
        foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
        {
            if (!modifiers.ContainsKey(type))
                modifiers[type] = 0f;
        }
    }

    private void Start()
    {
        if (baseStatsTemplate == null)
        {
            Debug.LogError($"StatComponent on {name} is missing baseStatsTemplate.", this);
            enabled = false;
            return;
        }

        EnsureModifiersInitialized();

        currentHP = getMaxHP();
        currentMP = getMaxMP();
        currentStamina = getMaxStamina();

        EventBus.Subscribe<StatChangeEvent>(OnStatChangeRequested);
        EventBus.Subscribe<StatModifierEvent>(OnModifier);
    }

    public void ClearDeadState()
    {
        isDead = false;
        HPRegenPaused = false;
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
                if (isDead && e.amount < 0f)
                    break;

                currentHP = Mathf.Clamp(
                    currentHP + e.amount,
                    0,
                    getMaxHP()
                );

                if (currentHP <= 0 && !isDead)
                {
                    isDead = true;
                    EventBus.Publish(new DeathEvent(this));
                }
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
        EnsureModifiersInitialized();
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
        EnsureModifiersInitialized();
        return modifiers.ContainsKey(type) ? modifiers[type] : 0f;
    }

    public PlayerStatsData ExportStatsForSave()
    {
        EnsureModifiersInitialized();

        var entries = new List<StatModifierEntry>();
        foreach (var pair in modifiers)
        {
            entries.Add(new StatModifierEntry
            {
                statType = pair.Key,
                value = pair.Value
            });
        }

        return new PlayerStatsData
        {
            hp = currentHP,
            mp = currentMP,
            stamina = currentStamina,
            statModifiers = entries.ToArray()
        };
    }

    public void ApplySaveData(PlayerStatsData data)
    {
        EnsureModifiersInitialized();

        foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
            modifiers[type] = 0f;

        if (data?.statModifiers != null)
        {
            foreach (var entry in data.statModifiers)
                modifiers[entry.statType] = entry.value;
        }

        currentHP = Mathf.Clamp(data?.hp ?? getMaxHP(), 0, getMaxHP());
        currentMP = Mathf.Clamp(data?.mp ?? getMaxMP(), 0, getMaxMP());
        currentStamina = Mathf.Clamp(data?.stamina ?? getMaxStamina(), 0, getMaxStamina());
        isStaminaExhausted = currentStamina <= 0f;

        EventBus.Publish(new StatUpdatedEvent(this));
    }


    private void RegenerateStats()
    {
        if (baseStatsTemplate == null)
            return;

        float previousHP = currentHP;
        float previousMP = currentMP;
        float previousStamina = currentStamina;

        if (!isDead && !HPRegenPaused)
            currentHP = Mathf.Min(currentHP + baseStatsTemplate.HPRegen * Time.deltaTime, getMaxHP());

        currentMP = Mathf.Min(currentMP + baseStatsTemplate.MPRegen * Time.deltaTime, getMaxMP());

        if (!StaminaRegenPaused)
        {
            currentStamina = Mathf.Min(currentStamina + baseStatsTemplate.StaminaRegen * Time.deltaTime, getMaxStamina());

            if (isStaminaExhausted && currentStamina >= getMaxStamina() / 6f) isStaminaExhausted = false;
        }

        if (Mathf.Approximately(previousHP, currentHP)
            && Mathf.Approximately(previousMP, currentMP)
            && Mathf.Approximately(previousStamina, currentStamina))
            return;

        EventBus.Publish(new StatUpdatedEvent(this));
    }

}
