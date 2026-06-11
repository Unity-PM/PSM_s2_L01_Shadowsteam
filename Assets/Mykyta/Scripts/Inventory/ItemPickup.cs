using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemSO itemData;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        EventBus.Publish(new InventoryItemAddedEvent(itemData, other.GetComponent<InventoryComponent>()));
        Destroy(gameObject);
    }
}
