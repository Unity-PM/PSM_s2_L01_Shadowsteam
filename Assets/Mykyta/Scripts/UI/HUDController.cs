using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Targets")]
    public StatComponent playerStats;

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

    [System.Obsolete]
    private void Awake()
    {
        EventBus.Subscribe<StatUpdatedEvent>(OnStatUpdated);
        EventBus.Subscribe<SkillCooldownEvent>(OnSkillCooldownChanged);
        EventBus.Subscribe<InventoryItemAddedEvent>(OnItemAdded);
    }

    private void Start()
    {
        RefreshStatUI();
    }

    [System.Obsolete]
    private void OnDestroy()
    {
        EventBus.Unsubscribe<StatUpdatedEvent>(OnStatUpdated);
        EventBus.Unsubscribe<SkillCooldownEvent>(OnSkillCooldownChanged);
        EventBus.Unsubscribe<InventoryItemAddedEvent>(OnItemAdded);
    }

    private void OnStatUpdated(StatUpdatedEvent e)
    {
        if (e.target != playerStats) return;
        RefreshStatUI();
    }

    [System.Obsolete]
    private void OnSkillCooldownChanged(SkillCooldownEvent e)
    {
        if (e.SkillId == null) return;
        if (e.SkillId == "Fireball")
            perkOne.fillAmount = e.CooldownRemaining / GetSkillMaxCooldown(e.SkillId);
        if (e.SkillId == "Heal")
            perkTwo.fillAmount = e.CooldownRemaining / GetSkillMaxCooldown(e.SkillId);
        if (perkOne.fillAmount == 0)
            perkOne.fillAmount = 100;
        if (perkTwo.fillAmount == 0)
            perkTwo.fillAmount = 100;
    }
    private void OnItemAdded(InventoryItemAddedEvent e)
    {
        if (e.item == null) return;
        foreach(Image item in items)
        {
            if (item.color != Color.black) {
                item.color = Color.black; return;
            }
        }
    }
    private void RefreshStatUI()
    {
        if (playerStats == null) return;

        hpBar.fillAmount = playerStats.getHP()/ playerStats.getMaxHP();

        mpBar.fillAmount = playerStats.getMP()/ playerStats.getMaxMP();

        staminaBar.fillAmount = playerStats.getStamina() / playerStats.getMaxStamina();

        hpText.text = $"{(int)playerStats.getHP()} / {playerStats.getMaxHP()}";

        mpText.text = $"{(int)playerStats.getMP()} / {playerStats.getMaxMP()}";

        staminaText.text = $"{(int)playerStats.getStamina()} / {playerStats.getMaxStamina()}";
    }

    [System.Obsolete]
    private float GetSkillMaxCooldown(string skillId)
    {
        var skillManager = FindObjectOfType<SkillManager>();
        var skill = skillManager.skills.Find(s => s.skillId == skillId);
        return skill != null ? skill.cooldown : 1f;
    }
}
