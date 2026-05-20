using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Targets")]
    public StatComponent playerStats;
    public SkillManager skillManager;

    [Header("UI Stat Bars")]
    public Image hpBar;
    public Image mpBar;
    public Image staminaBar;
    [Header("UI Stat Text")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;
    public TextMeshProUGUI staminaText;
    [Header("UI Cooldown Bars")]
    public Image perkOne;
    public Image perkTwo;
    [Header("UI Item Bars")]
    public List<Image> items;

    void Awake()
    {
        if (skillManager == null)
            skillManager = FindFirstObjectByType<SkillManager>();
    }

    void OnEnable()
    {
        EventBus.Subscribe<StatUpdatedEvent>(OnStatUpdated);
        EventBus.Subscribe<SkillCooldownEvent>(OnSkillCooldownChanged);
        EventBus.Subscribe<InventoryItemAddedEvent>(OnItemAdded);
    }

    void Start()
    {
        RefreshStatUI();
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<StatUpdatedEvent>(OnStatUpdated);
        EventBus.Unsubscribe<SkillCooldownEvent>(OnSkillCooldownChanged);
        EventBus.Unsubscribe<InventoryItemAddedEvent>(OnItemAdded);
    }

    void OnStatUpdated(StatUpdatedEvent e)
    {
        if (e.target != playerStats) return;
        RefreshStatUI();
    }

    void OnSkillCooldownChanged(SkillCooldownEvent e)
    {
        if (e.SkillId == null)
            return;

        float maxCooldown = GetSkillMaxCooldown(e.SkillId);
        float fill = maxCooldown > 0f ? 1f - (e.CooldownRemaining / maxCooldown) : 1f;
        fill = Mathf.Clamp01(fill);

        if (e.SkillId == "Fireball" && perkOne != null)
            perkOne.fillAmount = fill;

        if (e.SkillId == "Heal" && perkTwo != null)
            perkTwo.fillAmount = fill;
    }

    void OnItemAdded(InventoryItemAddedEvent e)
    {
        if (e.item == null || items == null)
            return;

        foreach (Image item in items)
        {
            if (item == null)
                continue;

            if (item.color != Color.black) {
                item.color = Color.black;
                return;
            }
        }
    }

    void RefreshStatUI()
    {
        if (playerStats == null)
            return;

        float maxHp = playerStats.getMaxHP();
        float maxMp = playerStats.getMaxMP();
        float maxStamina = playerStats.getMaxStamina();

        if (hpBar != null)
            hpBar.fillAmount = maxHp > 0f ? playerStats.getHP() / maxHp : 0f;
        if (mpBar != null)
            mpBar.fillAmount = maxMp > 0f ? playerStats.getMP() / maxMp : 0f;
        if (staminaBar != null)
            staminaBar.fillAmount = maxStamina > 0f ? playerStats.getStamina() / maxStamina : 0f;

        if (hpText != null)
            hpText.text = $"{(int)playerStats.getHP()} / {maxHp}";
        if (mpText != null)
            mpText.text = $"{(int)playerStats.getMP()} / {maxMp}";
        if (staminaText != null)
            staminaText.text = $"{(int)playerStats.getStamina()} / {maxStamina}";
    }

    float GetSkillMaxCooldown(string skillId)
    {
        if (skillManager == null || skillManager.skills == null)
            return 1f;

        var skill = skillManager.skills.Find(s => s != null && s.skillId == skillId);
        return skill != null ? skill.cooldown : 1f;
    }
}
