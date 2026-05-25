public struct InventoryUpdatedEvent
{
    public InventoryComponent inventory;
    public InventoryUpdatedEvent(InventoryComponent inv) { inventory = inv; }
}