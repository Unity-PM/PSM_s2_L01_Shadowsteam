using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EquipmentComponent : MonoBehaviour
{
    public StatComponent stats;

    private Dictionary<EquipmentSlotType, EquipmentSO> equippedItems =
        new Dictionary<EquipmentSlotType, EquipmentSO>();
    private void Awake()
    {
        stats = GetComponent<StatComponent>();
    }
    private void OnEnable()
    {
        EventBus.Subscribe<InventoryItemAddedEvent>(OnItemAdded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InventoryItemAddedEvent>(OnItemAdded);
    }

    private void OnItemAdded(InventoryItemAddedEvent e)
    {
        if (e.inventory.gameObject != gameObject)
            return;

        if (e.item is not EquipmentSO equipment)
            return;

        Equip(equipment);
    }

    private void Equip(EquipmentSO equipment)
    {
        if (equipment == null)
            return;

        if (equippedItems.TryGetValue(equipment.slotType, out var currentEquipment) && currentEquipment != null)
            RemoveModifiers(currentEquipment);

        equippedItems[equipment.slotType] = equipment;
        ApplyModifiers(equipment);

        Debug.Log($"Equipped {equipment.itemName}");
    }

    private void Unequip(EquipmentSO equipment)
    {
        RemoveModifiers(equipment);
    }

    private void ApplyModifiers(EquipmentSO equipment)
    {
        foreach (var mod in equipment.statModifiers)
        {
            EventBus.Publish(new StatModifierEvent(
                stats,
                mod.statType,
                mod.value
            ));
        }
    }

    private void RemoveModifiers(EquipmentSO equipment)
    {
        foreach (var mod in equipment.statModifiers)
        {
            EventBus.Publish(new StatModifierEvent(
                stats,
                mod.statType,
                -mod.value
            ));
        }
    }

    public EquippedItemEntry[] ExportEquippedForSave()
    {
        return equippedItems
            .Where(pair => pair.Value != null && !string.IsNullOrEmpty(pair.Value.itemId))
            .Select(pair => new EquippedItemEntry
            {
                slot = pair.Key,
                itemId = pair.Value.itemId
            })
            .ToArray();
    }

    public void HydrateEquippedFromSave(EquippedItemEntry[] savedItems, Dictionary<string, EquipmentSO> equipmentLookup)
    {
        foreach (var equippedItem in equippedItems.Values)
        {
            if (equippedItem != null)
                RemoveModifiers(equippedItem);
        }

        equippedItems.Clear();

        if (savedItems == null || equipmentLookup == null)
            return;

        foreach (var savedItem in savedItems)
        {
            if (savedItem == null || string.IsNullOrEmpty(savedItem.itemId))
                continue;

            if (!equipmentLookup.TryGetValue(savedItem.itemId, out var equipment) || equipment == null)
                continue;

            equippedItems[savedItem.slot] = equipment;
            ApplyModifiers(equipment);
        }
    }
}
