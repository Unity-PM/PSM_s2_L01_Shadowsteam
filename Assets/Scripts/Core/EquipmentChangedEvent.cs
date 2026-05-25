public struct EquipmentChangedEvent
{
    public EquipmentComponent equipment;
    public EquipmentChangedEvent(EquipmentComponent eq) { equipment = eq; }
}