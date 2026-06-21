using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSaveCoordinator : MonoBehaviour
{
    public static GameSaveCoordinator Instance { get; private set; }

    [SerializeField] ItemSO[] itemCatalog;
    [SerializeField] bool autoSaveOnQuit = true;

    readonly Dictionary<string, ItemSO> itemById = new Dictionary<string, ItemSO>();
    readonly Dictionary<string, EquipmentSO> equipmentById = new Dictionary<string, EquipmentSO>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureInstance();
    }

    public static GameSaveCoordinator EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject(nameof(GameSaveCoordinator));
        return go.AddComponent<GameSaveCoordinator>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RebuildItemLookup();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnApplicationQuit()
    {
        if (autoSaveOnQuit)
            TrySaveActivePlayer();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoSaveOnQuit)
            TrySaveActivePlayer();
    }

    public void RebuildEquipmentLookup()
    {
        RebuildItemLookup();
    }

    public void RebuildItemLookup()
    {
        itemById.Clear();
        equipmentById.Clear();

        if (itemCatalog != null)
        {
            foreach (var item in itemCatalog)
                TryRegisterItem(item);
        }

        var fromResources = Resources.LoadAll<ItemSO>(string.Empty);
        foreach (var item in fromResources)
            TryRegisterItem(item);

        // Include inactive pickups: a picked-up pickup deactivates and self-destroys in
        // its Awake, but it is still alive for this frame and is the only source of its
        // runtime icon. Excluding it would lose the icon on every load.
        ItemPickup[] pickups = FindObjectsByType<ItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
            TryRegisterItem(pickups[i] != null ? pickups[i].CurrentItemData : null);

        InventoryComponent[] inventories =
            FindObjectsByType<InventoryComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < inventories.Length; i++)
            inventories[i]?.RegisterItemsInLookup(itemById);

        // Runtime items the player already collected this session carry their icons only
        // in memory, so re-register them last to take precedence over degraded fallbacks.
        foreach (ItemSO item in GameSession.SessionItems)
            TryRegisterItem(item);
    }

    void TryRegisterItem(ItemSO item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemId))
            return;

        itemById[item.itemId] = item;

        if (item is EquipmentSO equipment)
            equipmentById[equipment.itemId] = equipment;
    }

    public static GameObject FindPlayerObject()
    {
        var playerDeath = UnityEngine.Object.FindFirstObjectByType<PlayerDeathHandler>();
        if (playerDeath != null)
            return playerDeath.gameObject;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        return tagged;
    }

    public GameSaveData BuildSaveDataFromPlayer(GameObject player)
    {
        if (player == null)
            return null;

        var stats = player.GetComponent<StatComponent>();
        var inventory = player.GetComponent<InventoryComponent>();
        var equipment = player.GetComponent<EquipmentComponent>();
        var t = player.transform;

        var data = new GameSaveData
        {
            sceneBuildIndex = SceneManager.GetActiveScene().buildIndex,
            position = t.position,
            rotation = t.rotation
        };

        if (stats != null)
            data.playerStats = ExportPlayerStatsForSave(stats);

        if (inventory != null)
            data.inventoryItems = inventory.ExportInventoryForSave();

        if (equipment != null)
            data.equippedItems = equipment.ExportEquippedForSave();

        data.pickedWorldItemIds = GameSession.ExportPickedWorldItemIds();

        return data;
    }

    static PlayerStatsData ExportPlayerStatsForSave(StatComponent stats)
    {
        PlayerStatsData data = stats.ExportStatsForSave();
        data.statModifiers = Array.Empty<StatModifierEntry>();
        return data;
    }

    public void TrySaveActivePlayer()
    {
        var player = FindPlayerObject();
        if (player == null)
            return;

        var data = BuildSaveDataFromPlayer(player);
        if (data != null)
            GameSaveService.Save(data);
    }

    public void SaveGame()
    {
        TrySaveActivePlayer();
    }

    public void RegisterPickedWorldItem(string pickupId)
    {
        GameSession.MarkWorldItemPicked(pickupId);
        TrySaveActivePlayer();
    }

    public void LoadGame()
    {
        var data = GameSaveService.Load();
        if (data == null)
        {
            Debug.LogWarning("GameSaveCoordinator.LoadGame: no valid save file.");
            return;
        }

        GameSession.PendingApply = data;
        GameSession.LoadPickedWorldItems(data.pickedWorldItemIds);
        SceneManager.LoadScene(data.sceneBuildIndex);
    }

    public void DeleteSave()
    {
        GameSaveService.DeleteSave();
        Platformer.QuestProgressCommands.DeleteSave();
        GameSession.ClearRuntimeState();
        ResetLiveQuestManagers();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebuildItemLookup();

        if (GameSession.PendingApply == null)
        {
            ApplyPickedWorldItemState(GameSession.ExportPickedWorldItemIds());
            return;
        }

        var data = GameSession.PendingApply;
        GameSession.PendingApply = null;
        GameSession.LoadPickedWorldItems(data.pickedWorldItemIds);
        ApplyPickedWorldItemState(data.pickedWorldItemIds);

        var player = FindPlayerObject();
        if (player == null)
        {
            Debug.LogWarning("GameSaveCoordinator: player not found after load; save not applied.");
            return;
        }

        ApplySaveToPlayer(player, data);
    }

    public void ApplySaveToPlayer(GameObject player, GameSaveData data)
    {
        if (player == null || data == null)
            return;

        var stats = player.GetComponent<StatComponent>();
        var inventory = player.GetComponent<InventoryComponent>();
        var equipment = player.GetComponent<EquipmentComponent>();
        PlayerStatsData savedStats = CreateStatsWithoutSavedModifiers(data.playerStats);

        if (stats != null)
            stats.ApplySaveData(savedStats);

        if (inventory != null)
        {
            inventory.HydrateInventoryFromSave(data.inventoryItems, itemById);
            inventory.RegisterItemsInLookup(itemById);
            RebuildEquipmentLookupFromItems();
        }

        if (equipment != null)
            equipment.HydrateEquippedFromSave(data.equippedItems, equipmentById);

        if (stats != null)
            stats.ApplySavedResourceValues(savedStats);

        player.transform.SetPositionAndRotation(data.position, data.rotation);
    }

    void ApplyPickedWorldItemState(string[] pickedIds)
    {
        if (pickedIds == null || pickedIds.Length == 0)
            return;

        var picked = new HashSet<string>(pickedIds);
        ItemPickup[] pickups = FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
        {
            ItemPickup pickup = pickups[i];
            if (pickup == null || !picked.Contains(pickup.PersistentPickupId))
                continue;

            Destroy(pickup.gameObject);
        }
    }

    void RebuildEquipmentLookupFromItems()
    {
        equipmentById.Clear();
        foreach (ItemSO item in itemById.Values)
        {
            if (item is EquipmentSO equipment && !string.IsNullOrEmpty(equipment.itemId))
                equipmentById[equipment.itemId] = equipment;
        }
    }

    static void ResetLiveQuestManagers()
    {
        Platformer.QuestManager[] managers =
            FindObjectsByType<Platformer.QuestManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < managers.Length; i++)
            managers[i]?.ResetProgressForNewGame();
    }

    static PlayerStatsData CreateStatsWithoutSavedModifiers(PlayerStatsData source)
    {
        if (source == null)
            return new PlayerStatsData { statModifiers = Array.Empty<StatModifierEntry>() };

        return new PlayerStatsData
        {
            hp = source.hp,
            mp = source.mp,
            stamina = source.stamina,
            statModifiers = Array.Empty<StatModifierEntry>()
        };
    }
}
