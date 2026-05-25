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
        Debug.Log($"[Inventory] added: {item.itemName} — total: {_items.Count}");
        EventBus.Publish(new InventoryUpdatedEvent(this));
    }

    public void RemoveItem(ItemSO item)
    {
        if (_items.Remove(item))
            EventBus.Publish(new InventoryUpdatedEvent(this));
    }
}