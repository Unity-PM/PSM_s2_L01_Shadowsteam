using UnityEngine;
using UnityEngine.Events;

namespace Platformer {
    /// <summary>
    /// Триггер «игрок рядом» — вызывайте <see cref="OnPlayerInRangeChanged"/> для показа «Press E» на своём UI (см. префаб Mishania).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NpcInteractionZone : MonoBehaviour {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private UnityEvent<bool> onPlayerInRangeChanged;

        public bool PlayerInRange { get; private set; }

        void OnValidate() {
            Collider c = GetComponent<Collider>();
            if (c != null)
                c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other) {
            if (!other.CompareTag(playerTag))
                return;

            PlayerInRange = true;
            onPlayerInRangeChanged?.Invoke(true);
        }

        void OnTriggerExit(Collider other) {
            if (!other.CompareTag(playerTag))
                return;

            PlayerInRange = false;
            onPlayerInRangeChanged?.Invoke(false);
        }
    }
}
