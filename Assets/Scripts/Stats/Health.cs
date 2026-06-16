using UnityEngine;
using UnityEngine.Events;

namespace Platformer {
    public class Health : MonoBehaviour {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private FloatEventChannel playerHealthChannel;
        [SerializeField] private UnityEvent onDiedOnce;

        int currentHealth;

        public bool IsDead => currentHealth <= 0;

        void Awake() {
            currentHealth = maxHealth;
        }

        void Start() {
            PublishHealthPercentage();
        }

        internal void AddDiedOnceListener(UnityAction listener) {
            if (listener != null)
                onDiedOnce.AddListener(listener);
        }

        internal void RemoveDiedOnceListener(UnityAction listener) {
            if (listener != null)
                onDiedOnce.RemoveListener(listener);
        }

        internal void TakeDamage(int damage) {
            if (IsDead)
                return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            PublishHealthPercentage();
            if (currentHealth <= 0)
                onDiedOnce?.Invoke();
        }

        void PublishHealthPercentage() {
            if (playerHealthChannel != null)
                playerHealthChannel.Invoke(currentHealth / (float)maxHealth);
        }
    }
}
