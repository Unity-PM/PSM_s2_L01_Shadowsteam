using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "Quest/Objectives/Collect Objective", fileName = "CollectObjective")]
    public class CollectObjective : QuestObjective {
        [SerializeField] private string itemId;
        [SerializeField] private int requiredAmount = 1;

        public string ItemId => itemId;
        public int RequiredAmount => requiredAmount;

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
