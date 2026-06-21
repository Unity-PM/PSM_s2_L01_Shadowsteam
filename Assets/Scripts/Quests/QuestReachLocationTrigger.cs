using UnityEngine;

namespace Platformer {
    /// <summary>
    /// Put on a trigger collider in the world. When the player enters,
    /// notifies <see cref="QuestManager"/> — must match <see cref="ReachLocationObjective.LocationId"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class QuestReachLocationTrigger : MonoBehaviour {
        [SerializeField] private string locationId = "location_village_gate";
        [SerializeField] private QuestManager questManager;
        [SerializeField] private string playerTag = "Player";

        string registeredWaypointKey;

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

        internal void Configure(string locationId, QuestManager questManager, string playerTag = "Player") {
            UnregisterWaypoint();
            this.locationId = locationId;
            this.questManager = questManager;
            this.playerTag = playerTag;
            RegisterWaypoint();
        }

        void OnValidate() {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other) {
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();
            if (questManager == null || string.IsNullOrEmpty(locationId))
                return;

            if (!other.CompareTag(playerTag))
                return;

            questManager.NotifyReachLocationEntered(locationId);
        }

        void RegisterWaypoint() {
            if (!isActiveAndEnabled)
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
