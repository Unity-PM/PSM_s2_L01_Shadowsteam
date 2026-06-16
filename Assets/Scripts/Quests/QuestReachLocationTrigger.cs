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

        void OnValidate() {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other) {
            if (questManager == null || string.IsNullOrEmpty(locationId))
                return;

            if (!other.CompareTag(playerTag))
                return;

            questManager.NotifyReachLocationEntered(locationId);
        }
    }
}
