using System;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int saveVersion = 1;
    public int sceneBuildIndex;
    public Vector3 position;
    public Quaternion rotation;
    public PlayerStatsData playerStats = new PlayerStatsData();
    public EquippedItemEntry[] equippedItems = Array.Empty<EquippedItemEntry>();
    public InventoryItemSaveEntry[] inventoryItems = Array.Empty<InventoryItemSaveEntry>();
    public string[] pickedWorldItemIds = Array.Empty<string>();
}

[Serializable]
public class PlayerStatsData
{
    public float hp;
    public float mp;
    public float stamina;
    public StatModifierEntry[] statModifiers = Array.Empty<StatModifierEntry>();
}

[Serializable]
public class StatModifierEntry
{
    public StatType statType;
    public float value;
}

[Serializable]
public class EquippedItemEntry
{
    public EquipmentSlotType slot;
    public string itemId;
}

[Serializable]
public class InventoryItemSaveEntry
{
    public string itemId;
    public string itemName;
    public string description;
    public ItemType itemType;
    public StatType useStatType;
    public float useAmount;
    public bool isEquipment;
    public EquipmentSlotType slot;
    public StatModifierEntry[] statModifiers = Array.Empty<StatModifierEntry>();
}
