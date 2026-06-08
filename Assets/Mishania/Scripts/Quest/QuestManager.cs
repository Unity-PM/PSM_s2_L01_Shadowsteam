using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer {
    public class QuestManager : MonoBehaviour {
        [SerializeField] private QuestDefinition[] questRegistry = Array.Empty<QuestDefinition>();
        [SerializeField] private IntEventChannel experienceGrantedChannel;
        [SerializeField] private bool loadSavedProgressOnAwake = true;
        [SerializeField] private bool saveOnApplicationQuit = true;
        [SerializeField] private string[] initialQuestIdsIfNoSave = Array.Empty<string>();

        readonly List<QuestRuntimeState> activeQuests = new();
        readonly HashSet<string> completedQuestIds = new();
        readonly Dictionary<string, QuestDefinition> questById = new();

        void Awake() {
            BuildRegistryLookup();
            if (loadSavedProgressOnAwake)
                LoadFromDisk();
            else if (!QuestProgressSaveService.HasSave()) {
                foreach (string id in initialQuestIdsIfNoSave) {
                    if (!string.IsNullOrEmpty(id))
                        TryStartQuest(id);
                }
            }
        }

        void Start() {
            ResolveCompletedQuests();
        }

        internal void NotifyReachLocationEntered(string locationId) {
            if (string.IsNullOrEmpty(locationId) || activeQuests.Count == 0)
                return;

            bool TryReachSlot(QuestObjective o, ref int slot) =>
                o.TryProgressReach(locationId, ref slot);

            ApplyProgress(TryReachSlot);
        }

        void OnApplicationQuit() {
            if (saveOnApplicationQuit)
                PersistProgress();
        }

        public IReadOnlyList<QuestRuntimeState> ActiveQuests => activeQuests;

        public event Action QuestJournalChanged;

        public bool TryGetQuestDefinition(string questId, out QuestDefinition definition) =>
            questById.TryGetValue(questId, out definition);

        public void CopyCompletedQuestIds(List<string> destination) {
            destination.Clear();
            foreach (string id in completedQuestIds)
                destination.Add(id);
        }

        internal bool IsQuestCompleted(string questId) =>
            !string.IsNullOrEmpty(questId) && completedQuestIds.Contains(questId);

        internal bool TryStartQuest(string questId) => TryStartQuest(questId, null);

        internal bool TryStartQuest(string questId, QuestCategory? expectedCategory) {
            if (string.IsNullOrEmpty(questId))
                return false;

            if (!questById.TryGetValue(questId, out QuestDefinition definition))
                return false;

            if (expectedCategory.HasValue && definition.Category != expectedCategory.Value) {
                Debug.LogWarning(
                    $"QuestManager: quest '{questId}' category {definition.Category} does not match expected {expectedCategory.Value}.");
                return false;
            }

            if (completedQuestIds.Contains(questId))
                return false;

            if (IsQuestActive(questId))
                return false;

            IReadOnlyList<QuestObjective> objectives = definition.Objectives;
            int n = objectives.Count;
            if (n == 0) {
                Debug.LogWarning($"QuestManager: quest '{questId}' has no objectives.");
                return false;
            }

            var progress = new int[n];
            activeQuests.Add(new QuestRuntimeState(definition, progress));
            PersistProgress();
            QuestJournalChanged?.Invoke();
            return true;
        }

        /// <summary>Uses <see cref="GameObject.tag"/> of the dead enemy for <see cref="KillObjective"/> matching.</summary>
        internal void NotifyEnemyKilled(GameObject killedEnemy) {
            if (killedEnemy == null || activeQuests.Count == 0)
                return;

            NotifyEnemyKilledWithTag(killedEnemy.tag);
        }

        /// <summary>Same as <see cref="NotifyEnemyKilled(GameObject)"/> but when you only have the tag string.</summary>
        internal void NotifyEnemyKilledWithTag(string killedEnemyTag) {
            if (activeQuests.Count == 0)
                return;

            bool TryKillSlot(QuestObjective o, ref int slot) =>
                o.TryProgressKill(killedEnemyTag ?? string.Empty, ref slot);

            ApplyProgress(TryKillSlot);
        }

        internal void NotifyItemCollected(string itemId, int amount = 1) {
            if (string.IsNullOrEmpty(itemId) || activeQuests.Count == 0 || amount <= 0)
                return;

            bool TryCollectSlot(QuestObjective o, ref int slot) =>
                o.TryProgressCollect(itemId, amount, ref slot);

            ApplyProgress(TryCollectSlot);
        }

        internal void NotifyNpcTalked(string npcId) {
            if (string.IsNullOrEmpty(npcId) || activeQuests.Count == 0)
                return;

            bool TryTalkSlot(QuestObjective o, ref int slot) =>
                o.TryProgressTalk(npcId, ref slot);

            ApplyProgress(TryTalkSlot);
        }

        /// <summary>Same as <see cref="NotifyNpcTalked"/> — called when NPC dialogue opens (Talk objectives).</summary>
        internal void NotifyNpcDialogueOpened(string npcId) =>
            NotifyNpcTalked(npcId);

        internal NpcOfferedQuestPhase GetNpcQuestPhase(string questId) {
            if (string.IsNullOrEmpty(questId))
                return NpcOfferedQuestPhase.NotOffered;
            if (IsQuestCompleted(questId))
                return NpcOfferedQuestPhase.Completed;
            if (IsQuestActive(questId))
                return NpcOfferedQuestPhase.ActiveInProgress;
            return NpcOfferedQuestPhase.NotOffered;
        }

        internal bool HasActiveMainQuest() {
            foreach (QuestRuntimeState state in activeQuests) {
                if (state.Definition.Category == QuestCategory.Main)
                    return true;
            }

            return false;
        }

        internal bool TryGetActiveMainQuestId(out string activeId) {
            foreach (QuestRuntimeState state in activeQuests) {
                if (state.Definition.Category == QuestCategory.Main) {
                    activeId = state.Definition.QuestId;
                    return true;
                }
            }

            activeId = null;
            return false;
        }

        /// <summary>Starts a Main quest from an NPC when no other Main quest is active.</summary>
        internal bool TryAcceptMainQuestFromNpc(string questId) {
            if (string.IsNullOrEmpty(questId))
                return false;

            if (!questById.TryGetValue(questId, out QuestDefinition definition))
                return false;

            if (definition.Category != QuestCategory.Main) {
                Debug.LogWarning(
                    $"QuestManager: TryAcceptMainQuestFromNpc expects Main category ('{questId}' is {definition.Category}).");
                return false;
            }

            if (completedQuestIds.Contains(questId))
                return false;

            if (IsQuestActive(questId))
                return false;

            if (TryGetActiveMainQuestId(out string otherMainId) && otherMainId != questId)
                return false;

            return TryStartQuest(questId, QuestCategory.Main);
        }

        /// <summary>Writes current quest snapshot to persistent quest JSON.</summary>
        internal void PersistProgress() =>
            QuestProgressSaveService.Save(BuildSaveDataSnapshot());

        internal void LoadFromDisk() {
            var data = QuestProgressSaveService.Load();
            activeQuests.Clear();
            completedQuestIds.Clear();

            if (data != null) {
                if (data.completedQuestIDs != null) {
                    foreach (string id in data.completedQuestIDs) {
                        if (!string.IsNullOrEmpty(id))
                            completedQuestIds.Add(id);
                    }
                }

                if (data.activeQuests != null) {
                    foreach (ActiveQuestSaveEntry entry in data.activeQuests) {
                        if (entry == null || string.IsNullOrEmpty(entry.id))
                            continue;

                        if (!questById.TryGetValue(entry.id, out QuestDefinition definition)) {
                            Debug.LogWarning($"QuestManager: skipped unknown quest id '{entry.id}' when loading.");
                            continue;
                        }

                        int[] normalized = ClampProgressToDefinition(definition, entry.progress);
                        activeQuests.Add(new QuestRuntimeState(definition, normalized));
                    }
                }
            }

            if (activeQuests.Count == 0 && completedQuestIds.Count == 0 &&
                initialQuestIdsIfNoSave.Length > 0 && data == null) {
                foreach (string id in initialQuestIdsIfNoSave) {
                    if (!string.IsNullOrEmpty(id))
                        TryStartQuest(id);
                }
            }

            ResolveCompletedQuests();
        }

        bool IsQuestActive(string questId) {
            foreach (QuestRuntimeState s in activeQuests) {
                if (s.Definition.QuestId == questId)
                    return true;
            }

            return false;
        }

        void BuildRegistryLookup() {
            questById.Clear();
            foreach (QuestDefinition def in questRegistry) {
                if (def == null || string.IsNullOrEmpty(def.QuestId))
                    continue;

                if (questById.ContainsKey(def.QuestId)) {
                    Debug.LogWarning($"QuestManager: duplicate quest id '{def.QuestId}' in registry.");
                    continue;
                }

                questById.Add(def.QuestId, def);
            }
        }

        delegate bool ObjectiveAdvance(QuestObjective objective, ref int slot);

        void ApplyProgress(ObjectiveAdvance tryAdvance) {
            bool changed = false;
            foreach (QuestRuntimeState state in activeQuests) {
                IReadOnlyList<QuestObjective> objectives = state.Definition.Objectives;
                for (int i = 0; i < objectives.Count; i++) {
                    QuestObjective o = objectives[i];
                    if (o == null)
                        continue;

                    int slot = state.Progress[i];
                    if (tryAdvance(o, ref slot)) {
                        state.Progress[i] = slot;
                        changed = true;
                    }
                }
            }

            if (changed) {
                ResolveCompletedQuests();
                PersistProgress();
                QuestJournalChanged?.Invoke();
            }
        }

        void ResolveCompletedQuests() {
            for (int i = activeQuests.Count - 1; i >= 0; i--) {
                if (IsQuestFullyComplete(activeQuests[i]))
                    CompleteQuest(activeQuests[i]);
            }
        }

        void CompleteQuest(QuestRuntimeState state) {
            activeQuests.Remove(state);
            QuestDefinition def = state.Definition;
            completedQuestIds.Add(def.QuestId);

            int xp = def.ExperienceReward;
            if (experienceGrantedChannel != null && xp > 0)
                experienceGrantedChannel.Invoke(xp);

            QuestCategory category = def.Category;
            string nextId = def.NextQuestId;
            if (!string.IsNullOrEmpty(nextId))
                TryStartQuest(nextId, category);

            PersistProgress();
            QuestJournalChanged?.Invoke();
        }

        static bool IsQuestFullyComplete(QuestRuntimeState state) {
            QuestDefinition def = state.Definition;
            IReadOnlyList<QuestObjective> objectives = def.Objectives;
            if (objectives.Count == 0)
                return false;

            for (int i = 0; i < objectives.Count; i++) {
                QuestObjective o = objectives[i];
                if (o == null)
                    return false;

                int cap = o.GetProgressCap();
                if (state.Progress[i] < cap)
                    return false;
            }

            return true;
        }

        static int[] ClampProgressToDefinition(QuestDefinition definition, int[] loaded) {
            IReadOnlyList<QuestObjective> objectives = definition.Objectives;
            int n = objectives.Count;
            var result = new int[n];

            if (loaded == null || loaded.Length != n)
                Debug.LogWarning(
                    $"QuestManager: progress length for '{definition.QuestId}' does not match objective count; reset to zero.");

            for (int i = 0; i < n; i++) {
                QuestObjective o = objectives[i];
                int value = loaded != null && i < loaded.Length ? loaded[i] : 0;
                int cap = o != null ? o.GetProgressCap() : 0;
                result[i] = Mathf.Clamp(value, 0, cap);
            }

            return result;
        }

        QuestProgressSaveData BuildSaveDataSnapshot() {
            var activeEntries = new ActiveQuestSaveEntry[activeQuests.Count];
            for (int i = 0; i < activeQuests.Count; i++) {
                QuestRuntimeState s = activeQuests[i];
                activeEntries[i] = new ActiveQuestSaveEntry {
                    id = s.Definition.QuestId,
                    progress = (int[])s.Progress.Clone()
                };
            }

            var completed = new string[completedQuestIds.Count];
            completedQuestIds.CopyTo(completed);

            return new QuestProgressSaveData {
                activeQuests = activeEntries,
                completedQuestIDs = completed
            };
        }

    }

    public sealed class QuestRuntimeState {
        public QuestDefinition Definition { get; }

        readonly int[] progress;

        internal QuestRuntimeState(QuestDefinition definition, int[] progress) {
            Definition = definition;
            this.progress = progress;
        }

        internal int[] Progress => progress;
    }
}
