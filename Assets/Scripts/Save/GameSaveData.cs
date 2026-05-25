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
