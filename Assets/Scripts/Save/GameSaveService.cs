using System;
using System.IO;
using UnityEngine;

public static class GameSaveService
{
    public const int CurrentSaveVersion = 1;
    const string FileName = "save.json";

    static string SavePath => GameSavePathProvider.GetSavePath(FileName);

    public static bool HasSave() => File.Exists(SavePath);

    public static void Save(GameSaveData data)
    {
        if (data == null)
            return;

        data.saveVersion = CurrentSaveVersion;
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }

    public static GameSaveData Load()
    {
        if (!HasSave())
            return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null || data.saveVersion != CurrentSaveVersion)
                return null;

            if (data.playerStats == null)
                data.playerStats = new PlayerStatsData();

            if (data.equippedItems == null)
                data.equippedItems = Array.Empty<EquippedItemEntry>();

            if (data.inventoryItems == null)
                data.inventoryItems = Array.Empty<InventoryItemSaveEntry>();

            if (data.pickedWorldItemIds == null)
                data.pickedWorldItemIds = Array.Empty<string>();

            if (data.playerStats.statModifiers == null)
                data.playerStats.statModifiers = Array.Empty<StatModifierEntry>();

            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GameSaveService.Load failed: {e.Message}");
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (HasSave())
            File.Delete(SavePath);
    }
}
