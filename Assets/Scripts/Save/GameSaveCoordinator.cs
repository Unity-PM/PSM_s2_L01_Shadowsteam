using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSaveCoordinator : MonoBehaviour
{
    public static GameSaveCoordinator Instance { get; private set; }

    [SerializeField] ItemSO[] itemCatalog;
    [SerializeField] bool autoSaveOnQuit = true;

    readonly Dictionary<string, EquipmentSO> equipmentById = new Dictionary<string, EquipmentSO>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RebuildEquipmentLookup();
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
        equipmentById.Clear();

        if (itemCatalog != null)
        {
            foreach (var item in itemCatalog)
                TryRegisterEquipment(item);
        }

        var fromResources = Resources.LoadAll<ItemSO>(string.Empty);
        foreach (var item in fromResources)
            TryRegisterEquipment(item);
    }

    void TryRegisterEquipment(ItemSO item)
    {
        if (item is not EquipmentSO equipment)
            return;

        if (string.IsNullOrEmpty(equipment.itemId))
            return;

        equipmentById[equipment.itemId] = equipment;
    }

    public static GameObject FindPlayerObject()
    {
        var playerDeath = Object.FindFirstObjectByType<PlayerDeathHandler>();
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
        var equipment = player.GetComponent<EquipmentComponent>();
        var t = player.transform;

        var data = new GameSaveData
        {
            sceneBuildIndex = SceneManager.GetActiveScene().buildIndex,
            position = t.position,
            rotation = t.rotation
        };

        if (stats != null)
            data.playerStats = stats.ExportStatsForSave();

        if (equipment != null)
            data.equippedItems = equipment.ExportEquippedForSave();

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

    public void LoadGame()
    {
        var data = GameSaveService.Load();
        if (data == null)
        {
            Debug.LogWarning("GameSaveCoordinator.LoadGame: no valid save file.");
            return;
        }

        GameSession.PendingApply = data;
        SceneManager.LoadScene(data.sceneBuildIndex);
    }

    public void DeleteSave()
    {
        GameSaveService.DeleteSave();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameSession.PendingApply == null)
            return;

        var data = GameSession.PendingApply;
        GameSession.PendingApply = null;

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
        var equipment = player.GetComponent<EquipmentComponent>();

        if (stats != null)
            stats.ApplySaveData(data.playerStats);

        if (equipment != null)
            equipment.HydrateEquippedFromSave(data.equippedItems, equipmentById);

        player.transform.SetPositionAndRotation(data.position, data.rotation);
    }
}
