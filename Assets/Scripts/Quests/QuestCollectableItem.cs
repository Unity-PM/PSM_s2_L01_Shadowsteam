using UnityEngine;

namespace Platformer {
    [DisallowMultipleComponent]
    [AddComponentMenu("Quest/Quest Collectable Item")]
    public class QuestCollectableItem : MonoBehaviour {
        [SerializeField] private string collectId;
        [SerializeField] private string displayName;
        [SerializeField] private QuestManager questManager;

        bool notified;
        string registeredWaypointKey;

        public string CollectId => collectId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        void Awake() {
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();
        }

        void OnEnable() {
            RegisterWaypoint();
        }

        void OnDisable() {
            UnregisterWaypoint();
        }

        public void Configure(string collectId, string displayName, QuestManager questManager) {
            UnregisterWaypoint();
            this.collectId = collectId;
            this.displayName = displayName;
            this.questManager = questManager;
            RegisterWaypoint();
        }

        public void NotifyCollected() {
            if (notified || string.IsNullOrEmpty(collectId))
                return;

            notified = true;
            UnregisterWaypoint();
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();

            if (questManager != null)
                questManager.NotifyItemCollected(collectId);
        }

        void RegisterWaypoint() {
            if (!isActiveAndEnabled || notified)
                return;

            string key = QuestWaypointRegistry.KeyForCollect(collectId);
            if (string.IsNullOrEmpty(key) || registeredWaypointKey == key)
                return;

            UnregisterWaypoint();
            QuestWaypointRegistry.Register(key, transform);
            registeredWaypointKey = key;
        }

        void UnregisterWaypoint() {
            if (string.IsNullOrEmpty(registeredWaypointKey))
                return;

            QuestWaypointRegistry.Unregister(registeredWaypointKey, transform);
            registeredWaypointKey = null;
        }
    }
}
