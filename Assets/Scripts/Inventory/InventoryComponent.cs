using UnityEngine;

public class InventoryComponent : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe<ItemPickedEvent>(OnItemPicked);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ItemPickedEvent>(OnItemPicked);
    }

    private void OnItemPicked(ItemPickedEvent e)
    {
        Debug.Log($"Picked item: {e.item.itemName}");
    }

}
