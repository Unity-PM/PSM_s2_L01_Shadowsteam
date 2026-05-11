using UnityEngine;

namespace Platformer {
    /// <summary>
    /// Invokes <see cref="QuestManager.NotifyEnemyKilled"/> when <see cref="Health"/> dies.
    /// Progress uses the enemy <see cref="GameObject.tag"/> (see <see cref="KillObjective"/>).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class QuestEnemyDeathNotifier : MonoBehaviour {
        [SerializeField] private QuestManager questManager;

        void OnEnable() {
            Health health = GetComponent<Health>();
            if (health != null)
                health.AddDiedOnceListener(OnEnemyDied);
        }

        void OnDisable() {
            Health health = GetComponent<Health>();
            if (health != null)
                health.RemoveDiedOnceListener(OnEnemyDied);
        }

        void OnEnemyDied() {
            if (questManager != null)
                questManager.NotifyEnemyKilled(gameObject);
        }
    }
}
