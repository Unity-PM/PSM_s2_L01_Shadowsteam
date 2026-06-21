using System;

namespace Platformer {
    /// <summary>
    /// DTO for <see cref="UnityEngine.JsonUtility"/>. Unity serializes only public fields —
    /// they are public on purpose; field names match the JSON keys.
    /// </summary>
    [Serializable]
    public class ActiveQuestSaveEntry {
        public string id;
        public int[] progress;
    }

    [Serializable]
    public class QuestProgressSaveData {
        public ActiveQuestSaveEntry[] activeQuests;
        public string[] completedQuestIDs;
    }
}
