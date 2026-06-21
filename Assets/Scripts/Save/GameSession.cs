using System.Collections.Generic;

public static class GameSession
{
    public static GameSaveData PendingApply { get; set; }

    static readonly HashSet<string> pickedWorldItemIds = new HashSet<string>();

    // Runtime ItemSO instances the player has collected this session, keyed by item id.
    // Holding strong references keeps their (runtime-built) icons alive across scene
    // loads, so a save reloaded mid-session can restore the real item, not a stub.
    static readonly Dictionary<string, ItemSO> sessionItemsById = new Dictionary<string, ItemSO>();

    public static IEnumerable<ItemSO> SessionItems => sessionItemsById.Values;

    public static void RegisterSessionItem(ItemSO item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemId))
            return;

        sessionItemsById[item.itemId] = item;
    }

    public static void LoadPickedWorldItems(string[] itemIds)
    {
        pickedWorldItemIds.Clear();

        if (itemIds == null)
            return;

        for (int i = 0; i < itemIds.Length; i++)
            MarkWorldItemPicked(itemIds[i]);
    }

    public static void MarkWorldItemPicked(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
            pickedWorldItemIds.Add(itemId);
    }

    public static bool IsWorldItemPicked(string itemId) =>
        !string.IsNullOrEmpty(itemId) && pickedWorldItemIds.Contains(itemId);

    public static string[] ExportPickedWorldItemIds()
    {
        var ids = new string[pickedWorldItemIds.Count];
        pickedWorldItemIds.CopyTo(ids);
        return ids;
    }

    public static void ClearRuntimeState()
    {
        PendingApply = null;
        pickedWorldItemIds.Clear();
        sessionItemsById.Clear();
    }
}
