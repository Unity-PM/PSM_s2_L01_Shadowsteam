using UnityEngine;
using System.Collections.Generic;
public enum EquipmentSlotType
{
    Head,
    Chest,
    Weapon,
    Ring,
    Amulet
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "DGD/Items/Equipment")]
public class EquipmentSO : ItemSO
{
    public EquipmentSlotType slotType;
    [System.Serializable]
    public class StatModifier
    {
        public StatType statType;
        public float value;
    }

    public List<StatModifier> statModifiers = new List<StatModifier>();
}