using UnityEngine;

public struct ItemPickedEvent
{
    public ItemSO item;
    public GameObject picker;

    public ItemPickedEvent(ItemSO item, GameObject picker)
    {
        this.item = item;
        this.picker = picker;
    }
}
