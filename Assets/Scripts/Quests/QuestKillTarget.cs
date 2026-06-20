using UnityEngine;

namespace Platformer {
    [AddComponentMenu("Quest/Quest Kill Target")]
    public class QuestKillTarget : MonoBehaviour {
        [SerializeField] private string killId = "field_enemy";
        [SerializeField] private QuestManager questManager;

        Health health;
        StatComponent stats;
        bool notified;
        string registeredWaypointKey;

        public string KillId => killId;

        void Awake() {
            health = GetComponent<Health>();
            stats = GetComponent<StatComponent>();
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();
        }

        void OnEnable() {
            if (health != null)
                health.AddDiedOnceListener(NotifyKilled);

            EventBus.Subscribe<DeathEvent>(OnStatDeath);
            RegisterWaypoint();
        }

        void OnDisable() {
            if (health != null)
                health.RemoveDiedOnceListener(NotifyKilled);

            EventBus.Unsubscribe<DeathEvent>(OnStatDeath);
            UnregisterWaypoint();
        }

        public void Configure(string killId, QuestManager questManager) {
            UnregisterWaypoint();
            this.killId = killId;
            this.questManager = questManager;
            RegisterWaypoint();
        }

        void OnStatDeath(DeathEvent e) {
            if (stats == null || e.target != stats)
                return;

            NotifyKilled();
        }

        public void NotifyKilled() {
            if (notified || string.IsNullOrEmpty(killId))
                return;

            notified = true;
            UnregisterWaypoint();
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();

            if (questManager != null)
                questManager.NotifyEnemyKilledWithTag(killId);
        }

        void RegisterWaypoint() {
            if (!isActiveAndEnabled || notified)
                return;

            string key = QuestWaypointRegistry.KeyForKill(killId);
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
