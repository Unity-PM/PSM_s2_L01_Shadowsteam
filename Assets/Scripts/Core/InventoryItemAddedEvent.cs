public struct InventoryItemAddedEvent
{
    public ItemSO item;
    public InventoryComponent inventory;

    public InventoryItemAddedEvent(ItemSO item, InventoryComponent inventory)
    {
        this.item = item;
        this.inventory = inventory;
    }
}
