using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "Quest/Objectives/Break Object Objective", fileName = "BreakObjectObjective")]
    public class BreakObjectObjective : QuestObjective {
        [SerializeField] private string breakableId = "portal_boulder";
        [SerializeField] private int requiredAmount = 1;

        public string BreakableId => breakableId;
        public int RequiredAmount => requiredAmount;

        internal void Configure(string breakableId, int requiredAmount) {
            this.breakableId = breakableId;
            this.requiredAmount = Mathf.Max(1, requiredAmount);
        }

        protected internal override bool TryProgressBreakObject(string notifiedBreakableId, int amount,
            ref int slotProgress) {
            if (notifiedBreakableId != breakableId || amount <= 0)
                return false;
            if (slotProgress >= requiredAmount)
                return false;

            int delta = Mathf.Min(amount, requiredAmount - slotProgress);
            slotProgress += delta;
            return delta > 0;
        }

        protected internal override int GetProgressCap() => Mathf.Max(1, requiredAmount);
    }
}
