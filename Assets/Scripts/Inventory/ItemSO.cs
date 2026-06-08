using UnityEngine;

public enum ItemType
{
    Consumable,
    Equipment,
    Quest
}

[CreateAssetMenu(fileName = "NewItem", menuName = "DGD/Items/Item")]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public string itemName;
    [TextArea] public string description;

    public Sprite icon;
    public ItemType itemType;

    [Header("Consumable")]
    public StatType useStatType = StatType.HP;
    public float useAmount;
}