using System;

namespace Platformer {
    /// <summary>
    /// DTO для <see cref="UnityEngine.JsonUtility"/>. Unity сериализует только публичные поля —
    /// здесь они оставлены намеренно; имена полей совпадают с ключами в JSON.
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
