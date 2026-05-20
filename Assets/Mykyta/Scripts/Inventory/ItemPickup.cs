using Platformer;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemSO itemData;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || itemData == null)
            return;

        InventoryComponent inventory = other.GetComponent<InventoryComponent>();
        if (inventory == null)
            return;

        EventBus.Publish(new InventoryItemAddedEvent(itemData, inventory));

        QuestManager questManager = FindFirstObjectByType<QuestManager>();
        if (questManager != null && !string.IsNullOrEmpty(itemData.itemId))
            questManager.NotifyItemCollected(itemData.itemId);

        Destroy(gameObject);
    }
}
