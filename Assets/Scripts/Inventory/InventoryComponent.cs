using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryComponent : MonoBehaviour
{
    private readonly List<ItemSO> _items = new();
    public IReadOnlyList<ItemSO> Items => _items;

    private void OnEnable()
    {
        EventBus.Subscribe<ItemPickedEvent>(OnItemPicked);
        EventBus.Subscribe<InventoryItemAddedEvent>(OnItemAdded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ItemPickedEvent>(OnItemPicked);
        EventBus.Unsubscribe<InventoryItemAddedEvent>(OnItemAdded);
    }

    private void OnItemPicked(ItemPickedEvent e)
    {
        AddItem(e.item);
    }

    private void OnItemAdded(InventoryItemAddedEvent e)
    {
        if (e.inventory != this) return;
        AddItem(e.item);
    }

    private void AddItem(ItemSO item)
    {
        if (item == null) return;
        _items.Add(item);
        // Keep the collected instance (and its runtime icon) discoverable after a reload.
        GameSession.RegisterSessionItem(item);
        Debug.Log($"[Inventory] added: {item.itemName} - total: {_items.Count}");
        EventBus.Publish(new InventoryUpdatedEvent(this));
    }

    public InventoryItemSaveEntry[] ExportInventoryForSave()
    {
        var entries = new List<InventoryItemSaveEntry>();
        for (int i = 0; i < _items.Count; i++)
        {
            InventoryItemSaveEntry entry = CreateSaveEntry(_items[i]);
            if (entry != null)
                entries.Add(entry);
        }

        return entries.ToArray();
    }

    public void HydrateInventoryFromSave(
        InventoryItemSaveEntry[] savedItems,
        Dictionary<string, ItemSO> itemLookup)
    {
        _items.Clear();

        if (savedItems != null)
        {
            for (int i = 0; i < savedItems.Length; i++)
            {
                ItemSO item = ResolveSavedItem(savedItems[i], itemLookup);
                if (item != null)
                    _items.Add(item);
            }
        }

        EventBus.Publish(new InventoryUpdatedEvent(this));
    }

    public void RegisterItemsInLookup(Dictionary<string, ItemSO> itemLookup)
    {
        if (itemLookup == null)
            return;

        for (int i = 0; i < _items.Count; i++)
        {
            ItemSO item = _items[i];
            if (item == null || string.IsNullOrEmpty(item.itemId))
                continue;

            itemLookup[item.itemId] = item;
        }
    }

    public void RemoveItem(ItemSO item)
    {
        if (_items.Remove(item))
            EventBus.Publish(new InventoryUpdatedEvent(this));
    }

    public bool TryUseItem(ItemSO item)
    {
        if (item == null || item.itemType != ItemType.Consumable)
            return false;

        if (!_items.Contains(item))
            return false;

        if (item.useAmount != 0f)
        {
            StatComponent stats = GetComponent<StatComponent>();
            if (stats != null)
                EventBus.Publish(new StatChangeEvent(stats, item.useStatType, item.useAmount));
        }

        RemoveItem(item);
        Debug.Log($"[Inventory] used: {item.itemName}");
        return true;
    }

    static InventoryItemSaveEntry CreateSaveEntry(ItemSO item)
    {
        if (item == null)
            return null;

        var entry = new InventoryItemSaveEntry
        {
            itemId = item.itemId,
            itemName = item.itemName,
            description = item.description,
            itemType = item.itemType,
            useStatType = item.useStatType,
            useAmount = item.useAmount,
            statModifiers = Array.Empty<StatModifierEntry>()
        };

        if (item is EquipmentSO equipment)
        {
            entry.isEquipment = true;
            entry.slot = equipment.slotType;

            if (equipment.statModifiers != null)
            {
                var modifiers = new List<StatModifierEntry>();
                for (int i = 0; i < equipment.statModifiers.Count; i++)
                {
                    EquipmentSO.StatModifier modifier = equipment.statModifiers[i];
                    if (modifier == null)
                        continue;

                    modifiers.Add(new StatModifierEntry
                    {
                        statType = modifier.statType,
                        value = modifier.value
                    });
                }

                entry.statModifiers = modifiers.ToArray();
            }
        }

        return entry;
    }

    static ItemSO ResolveSavedItem(InventoryItemSaveEntry entry, Dictionary<string, ItemSO> itemLookup)
    {
        if (entry == null)
            return null;

        if (!string.IsNullOrEmpty(entry.itemId)
            && itemLookup != null
            && itemLookup.TryGetValue(entry.itemId, out ItemSO item)
            && item != null)
            return item;

        return CreateRuntimeItem(entry);
    }

    static ItemSO CreateRuntimeItem(InventoryItemSaveEntry entry)
    {
        ItemSO item = entry.isEquipment
            ? ScriptableObject.CreateInstance<EquipmentSO>()
            : ScriptableObject.CreateInstance<ItemSO>();

        item.hideFlags = HideFlags.DontSave;
        item.itemId = entry.itemId;
        item.itemName = entry.itemName;
        item.description = entry.description;
        item.itemType = entry.itemType;
        item.useStatType = entry.useStatType;
        item.useAmount = entry.useAmount;
        item.name = string.IsNullOrEmpty(item.itemName) ? item.itemId : item.itemName;

        if (item is EquipmentSO equipment)
        {
            equipment.slotType = entry.slot;
            equipment.statModifiers = new List<EquipmentSO.StatModifier>();

            if (entry.statModifiers != null)
            {
                for (int i = 0; i < entry.statModifiers.Length; i++)
                {
                    StatModifierEntry modifier = entry.statModifiers[i];
                    if (modifier == null)
                        continue;

                    equipment.statModifiers.Add(new EquipmentSO.StatModifier
                    {
                        statType = modifier.statType,
                        value = modifier.value
                    });
                }
            }
        }

        return item;
    }
}
