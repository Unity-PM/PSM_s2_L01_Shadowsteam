using System;
using System.IO;
using UnityEngine;

namespace Platformer {
    internal static class QuestProgressSaveService {
        const string FileName = "quest_progress.json";

        static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        internal static bool HasSave() => File.Exists(SavePath);

        internal static void Save(QuestProgressSaveData data) {
            if (data == null)
                return;

            if (data.activeQuests == null)
                data.activeQuests = Array.Empty<ActiveQuestSaveEntry>();

            if (data.completedQuestIDs == null)
                data.completedQuestIDs = Array.Empty<string>();

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }

        internal static QuestProgressSaveData Load() {
            if (!HasSave())
                return null;

            try {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<QuestProgressSaveData>(json);
                if (data == null)
                    return null;

                if (data.activeQuests == null)
                    data.activeQuests = Array.Empty<ActiveQuestSaveEntry>();

                if (data.completedQuestIDs == null)
                    data.completedQuestIDs = Array.Empty<string>();

                return data;
            }
            catch (Exception e) {
                Debug.LogWarning($"QuestProgressSaveService.Load failed: {e.Message}");
                return null;
            }
        }

        internal static void DeleteSave() {
            if (HasSave())
                File.Delete(SavePath);
        }
    }
}
