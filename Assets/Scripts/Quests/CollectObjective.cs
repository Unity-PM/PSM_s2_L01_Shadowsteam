using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "Quest/Objectives/Collect Objective", fileName = "CollectObjective")]
    public class CollectObjective : QuestObjective {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private int requiredAmount = 1;

        public string ItemId => itemId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? itemId : displayName;
        public int RequiredAmount => requiredAmount;

        internal void Configure(string itemId, int requiredAmount, string displayName = null) {
            this.itemId = itemId;
            this.displayName = displayName;
            this.requiredAmount = Mathf.Max(1, requiredAmount);
        }

        protected internal override bool TryProgressCollect(string collectItemId, int amount, ref int slotProgress) {
            if (collectItemId != itemId || amount <= 0)
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
