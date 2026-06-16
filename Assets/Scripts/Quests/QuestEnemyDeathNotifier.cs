using UnityEngine;

namespace Platformer {
    /// <summary>
    /// Invokes <see cref="QuestManager.NotifyEnemyKilled"/> when the enemy dies via
    /// <see cref="Health"/> or <see cref="StatComponent"/> / <see cref="DeathEvent"/>.
    /// Progress uses the enemy <see cref="GameObject.tag"/> (see <see cref="KillObjective"/>).
    /// </summary>
    public class QuestEnemyDeathNotifier : MonoBehaviour {
        [SerializeField] private QuestManager questManager;

        StatComponent statComponent;
        bool notified;

        void Awake() {
            statComponent = GetComponent<StatComponent>();
        }

        void OnEnable() {
            Health health = GetComponent<Health>();
            if (health != null)
                health.AddDiedOnceListener(OnEnemyDied);

            EventBus.Subscribe<DeathEvent>(OnStatDeath);
        }

        void OnDisable() {
            Health health = GetComponent<Health>();
            if (health != null)
                health.RemoveDiedOnceListener(OnEnemyDied);

            EventBus.Unsubscribe<DeathEvent>(OnStatDeath);
        }

        void OnStatDeath(DeathEvent e) {
            if (statComponent == null || e.target != statComponent)
                return;

            OnEnemyDied();
        }

        void OnEnemyDied() {
            if (notified)
                return;

            notified = true;

            if (questManager != null)
                questManager.NotifyEnemyKilled(gameObject);
        }
    }
}
