using System.Collections.Generic;
using Platformer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ItemPickup : MonoBehaviour
{
    public ItemSO itemData;
    [Header("Direct Icon Item")]
    [Tooltip("Drop a PNG texture here. If Item Data is empty, a simple runtime item is created.")]
    public Texture2D itemIconPng;
    [Tooltip("Optional Sprite alternative.")]
    public Sprite itemIcon;
    [Tooltip("Optional. Empty uses Item Data name, icon name, or GameObject name.")]
    public string itemNameOverride;
    [Tooltip("Optional. Empty uses Item Data id, icon name, or GameObject name.")]
    public string itemIdOverride;
    [TextArea] public string descriptionOverride;
    public ItemType directItemType = ItemType.Quest;

    [Header("Persistence")]
    [Tooltip("Stable id for this exact world pickup. Empty uses scene path + hierarchy path + item id.")]
    [SerializeField] private string persistentPickupId;

    [Header("Idle Animation")]
    [SerializeField] private bool floatAnimation = true;
    [SerializeField, Min(0f)] private float floatHeight = 0.15f;
    [SerializeField, Min(0f)] private float floatSpeed = 1.25f;
    [SerializeField] private bool randomizeFloatPhase = true;

    [Header("Pickup Physics")]
    [SerializeField] private bool disablePickupPhysics = true;
    [SerializeField] private bool disableSolidPhysicsColliders = true;

    ItemSO runtimeItemData;
    Sprite runtimeIconSprite;
    Vector3 floatStartLocalPosition;
    float floatPhase;

    public ItemSO CurrentItemData => ResolveItemData();
    public string PersistentPickupId => ResolvePersistentPickupId();

    void Awake()
    {
        if (GameSession.IsWorldItemPicked(ResolvePersistentPickupId()))
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        StabilizePickupPhysics();
        floatStartLocalPosition = transform.localPosition;
        floatPhase = randomizeFloatPhase ? Mathf.Abs(GetInstanceID() % 1000) * 0.01f : 0f;
    }

    void Start()
    {
        if (GameSession.IsWorldItemPicked(ResolvePersistentPickupId()))
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!floatAnimation || floatHeight <= 0f || floatSpeed <= 0f)
            return;

        float offsetY = Mathf.Sin(Time.time * floatSpeed * Mathf.PI * 2f + floatPhase) * floatHeight;
        transform.localPosition = floatStartLocalPosition + Vector3.up * offsetY;
    }

    void OnTriggerEnter(Collider other)
    {
        ItemSO pickedItem = ResolveItemData();
        if (!other.CompareTag("Player") || pickedItem == null)
            return;

        InventoryComponent inventory = other.GetComponent<InventoryComponent>();
        if (inventory == null)
            return;

        EventBus.Publish(new InventoryItemAddedEvent(pickedItem, inventory));

        QuestCollectableItem questCollectable = GetComponent<QuestCollectableItem>();
        if (questCollectable != null)
            questCollectable.NotifyCollected();

        QuestManager questManager = FindFirstObjectByType<QuestManager>();
        if (questManager != null && !string.IsNullOrEmpty(pickedItem.itemId))
            questManager.NotifyItemCollected(pickedItem.itemId);

        string pickupId = ResolvePersistentPickupId();
        if (!string.IsNullOrEmpty(pickupId))
        {
            GameSession.MarkWorldItemPicked(pickupId);
            GameSaveCoordinator.Instance?.RegisterPickedWorldItem(pickupId);
        }

        Destroy(gameObject);
    }

    void StabilizePickupPhysics()
    {
        if (!disablePickupPhysics)
            return;

        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody body = rigidbodies[i];
            if (body == null)
                continue;

            body.useGravity = false;
            body.isKinematic = true;
        }

        if (!disableSolidPhysicsColliders)
            return;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider itemCollider = colliders[i];
            if (itemCollider == null || itemCollider.isTrigger)
                continue;

            itemCollider.enabled = false;
        }
    }

    ItemSO ResolveItemData()
    {
        if (itemData == null && !HasDirectItemConfig())
            return null;

        if (!HasDirectItemConfig())
            return itemData;

        if (runtimeItemData == null)
            runtimeItemData = CreateRuntimeItem();

        return runtimeItemData;
    }

    bool HasDirectItemConfig()
    {
        return itemIconPng != null
            || itemIcon != null
            || !string.IsNullOrWhiteSpace(itemNameOverride)
            || !string.IsNullOrWhiteSpace(itemIdOverride)
            || !string.IsNullOrWhiteSpace(descriptionOverride);
    }

    ItemSO CreateRuntimeItem()
    {
        ItemSO item = itemData is EquipmentSO ? ScriptableObject.CreateInstance<EquipmentSO>() : ScriptableObject.CreateInstance<ItemSO>();
        item.hideFlags = HideFlags.DontSave;

        CopyBaseItemData(item, itemData);

        if (item is EquipmentSO equipment && itemData is EquipmentSO sourceEquipment)
            CopyEquipmentData(equipment, sourceEquipment);

        ApplyDirectItemData(item);
        item.name = string.IsNullOrWhiteSpace(item.itemName) ? name : item.itemName;
        return item;
    }

    void CopyBaseItemData(ItemSO target, ItemSO source)
    {
        if (source == null)
        {
            target.itemType = directItemType;
            return;
        }

        target.itemId = source.itemId;
        target.itemName = source.itemName;
        target.description = source.description;
        target.icon = source.icon;
        target.itemType = source.itemType;
        target.useStatType = source.useStatType;
        target.useAmount = source.useAmount;
    }

    void CopyEquipmentData(EquipmentSO target, EquipmentSO source)
    {
        target.slotType = source.slotType;
        target.statModifiers = source.statModifiers != null
            ? new List<EquipmentSO.StatModifier>(source.statModifiers)
            : new List<EquipmentSO.StatModifier>();
    }

    void ApplyDirectItemData(ItemSO item)
    {
        Sprite directIcon = ResolveDirectIcon();
        if (directIcon != null)
            item.icon = directIcon;

        if (!string.IsNullOrWhiteSpace(itemNameOverride))
            item.itemName = itemNameOverride.Trim();
        else if (string.IsNullOrWhiteSpace(item.itemName))
            item.itemName = GetDefaultItemName();

        if (!string.IsNullOrWhiteSpace(itemIdOverride))
            item.itemId = itemIdOverride.Trim();
        else if (string.IsNullOrWhiteSpace(item.itemId))
            item.itemId = GetDefaultItemId();

        if (!string.IsNullOrWhiteSpace(descriptionOverride))
            item.description = descriptionOverride;
    }

    string GetDefaultItemName()
    {
        string iconName = GetDirectIconName();
        if (!string.IsNullOrWhiteSpace(iconName))
            return iconName;

        return name;
    }

    string GetDefaultItemId()
    {
        string iconName = GetDirectIconName();
        if (!string.IsNullOrWhiteSpace(iconName))
            return iconName;

        return name;
    }

    Sprite ResolveDirectIcon()
    {
        if (itemIcon != null)
            return itemIcon;

        if (itemIconPng == null)
            return null;

        if (runtimeIconSprite == null)
        {
            runtimeIconSprite = Sprite.Create(
                itemIconPng,
                new Rect(0f, 0f, itemIconPng.width, itemIconPng.height),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeIconSprite.hideFlags = HideFlags.DontSave;
            runtimeIconSprite.name = itemIconPng.name;
        }

        return runtimeIconSprite;
    }

    string GetDirectIconName()
    {
        if (itemIcon != null && !string.IsNullOrWhiteSpace(itemIcon.name))
            return itemIcon.name;

        if (itemIconPng != null && !string.IsNullOrWhiteSpace(itemIconPng.name))
            return itemIconPng.name;

        return null;
    }

    string ResolvePersistentPickupId()
    {
        if (!string.IsNullOrWhiteSpace(persistentPickupId))
            return persistentPickupId.Trim();

        Scene scene = gameObject.scene;
        string scenePart = !string.IsNullOrEmpty(scene.path) ? scene.path : scene.name;
        string itemPart = ResolveItemData()?.itemId;
        if (string.IsNullOrWhiteSpace(itemPart))
            itemPart = string.IsNullOrWhiteSpace(itemIdOverride) ? name : itemIdOverride.Trim();

        return $"{scenePart}:{GetHierarchyPath(transform)}:{itemPart}";
    }

    static string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return string.Empty;

        var parts = new List<string>();
        Transform current = target;
        while (current != null)
        {
            parts.Add($"{current.name}[{current.GetSiblingIndex()}]");
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }
}
