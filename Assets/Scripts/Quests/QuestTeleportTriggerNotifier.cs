using UnityEngine;

namespace Platformer {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TeleportTrigger))]
    [AddComponentMenu("Quest/Quest Teleport Trigger Notifier")]
    public class QuestTeleportTriggerNotifier : MonoBehaviour {
        [SerializeField] private string locationId = "boss_portal";
        [SerializeField] private QuestManager questManager;
        [SerializeField] private string playerTag = "Player";

        TeleportTrigger teleportTrigger;
        string registeredWaypointKey;
        bool notified;

        void Awake() {
            teleportTrigger = GetComponent<TeleportTrigger>();
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();
        }

        void OnEnable() {
            if (teleportTrigger == null)
                teleportTrigger = GetComponent<TeleportTrigger>();

            if (teleportTrigger != null)
                teleportTrigger.TeleportStarted += OnTeleportStarted;

            RegisterWaypoint();
        }

        void OnDisable() {
            if (teleportTrigger != null)
                teleportTrigger.TeleportStarted -= OnTeleportStarted;

            UnregisterWaypoint();
        }

        internal void Configure(string locationId, QuestManager questManager, string playerTag = "Player") {
            UnregisterWaypoint();
            this.locationId = locationId;
            this.questManager = questManager;
            this.playerTag = playerTag;
            RegisterWaypoint();
        }

        void OnTeleportStarted(GameObject target) {
            if (notified || string.IsNullOrEmpty(locationId))
                return;
            if (!IsPlayerTarget(target))
                return;

            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();
            if (questManager == null)
                return;

            notified = true;
            UnregisterWaypoint();
            questManager.NotifyReachLocationEntered(locationId);
        }

        bool IsPlayerTarget(GameObject target) {
            if (target == null)
                return false;
            if (string.IsNullOrEmpty(playerTag))
                return true;

            Transform current = target.transform;
            while (current != null) {
                if (current.CompareTag(playerTag))
                    return true;

                current = current.parent;
            }

            return false;
        }

        void RegisterWaypoint() {
            if (!isActiveAndEnabled || notified)
                return;

            string key = QuestWaypointRegistry.KeyForReach(locationId);
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
