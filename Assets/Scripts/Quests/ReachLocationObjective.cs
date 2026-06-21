using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "Quest/Objectives/Reach Location Objective",
        fileName = "ReachLocationObjective")]
    public class ReachLocationObjective : QuestObjective {
        [Tooltip("Same id as on QuestReachLocationTrigger in the scene.")]
        [SerializeField] private string locationId;

        public string LocationId => locationId;

        internal void Configure(string locationId) {
            this.locationId = locationId;
        }

        protected internal override bool TryProgressReach(string enteredLocationId, ref int slotProgress) {
            if (slotProgress >= 1)
                return false;
            if (string.IsNullOrEmpty(locationId) || string.IsNullOrEmpty(enteredLocationId))
                return false;
            if (enteredLocationId != locationId)
                return false;

            slotProgress = 1;
            return true;
        }

        protected internal override int GetProgressCap() => 1;
    }
}
