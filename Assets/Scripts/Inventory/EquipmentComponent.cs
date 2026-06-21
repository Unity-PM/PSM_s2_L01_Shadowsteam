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
    }

    private void OnDisable()
    {
    }

    private void OnItemAdded(InventoryItemAddedEvent e)
    {
        if (e.inventory.gameObject != gameObject)
            return;

        if (e.item is not EquipmentSO equipment)
            return;

        Equip(equipment);
    }
    public void Equip(EquipmentSO equipment)
    {
        if (equipment == null) return;

        if (equippedItems.TryGetValue(equipment.slotType, out var current) && current != null)
            RemoveModifiers(current);

        equippedItems[equipment.slotType] = equipment;
        ApplyModifiers(equipment);

        Debug.Log($"[Equipment] equipped: {equipment.itemName} in slot {equipment.slotType}");
        EventBus.Publish(new EquipmentChangedEvent(this));
    }

    public void Unequip(EquipmentSlotType slot)
    {
        if (!equippedItems.TryGetValue(slot, out var current) || current == null) return;
        RemoveModifiers(current);
        equippedItems.Remove(slot);
        Debug.Log($"[Equipment] unequipped: {current.itemName} from slot {slot}");
        EventBus.Publish(new EquipmentChangedEvent(this));
    }

    public EquipmentSO GetEquipped(EquipmentSlotType slot)
    {
        return equippedItems.TryGetValue(slot, out var current) ? current : null;
    }

    public bool IsEquipped(EquipmentSO equipment)
    {
        return equipment != null
            && equippedItems.TryGetValue(equipment.slotType, out var current)
            && current == equipment;
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
