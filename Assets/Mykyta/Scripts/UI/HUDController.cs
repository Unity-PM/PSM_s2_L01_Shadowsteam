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

    private void OnSkillCooldownChanged(SkillCooldownEvent e)
    {
        if (string.IsNullOrEmpty(e.SkillId)) return;

        var sm = playerStats.GetComponent<SkillManager>();
        if (sm == null || sm.combos.Count == 0) return;

        // Используем метод из SkillManager, который мы добавили на прошлом этапе
        float maxCD = sm.GetComboMaxCooldown(e.SkillId);
        if (maxCD <= 0) maxCD = 1f;

        float fill = e.CooldownRemaining / maxCD;

        // Связываем слоты UI с первыми двумя комбо в списке менеджера
        if (e.SkillId == sm.combos[0].skillId)
        { perkOne.fillAmount = fill > 0 ? fill : 1; }
        else if (sm.combos.Count > 1 && e.SkillId == sm.combos[1].skillId)
        { perkTwo.fillAmount = fill > 0 ? fill : 1; }
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
        // Исправляем обращение: теперь ищем в списке combos
        var skillManager = playerStats.GetComponent<SkillManager>();
        var combo = skillManager.combos.Find(s => s.skillId == skillId);
        return combo != null ? combo.finalCooldown : 1f;
    }
}
