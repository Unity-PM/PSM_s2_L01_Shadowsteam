using UnityEngine;
using System.Collections.Generic;

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
}
