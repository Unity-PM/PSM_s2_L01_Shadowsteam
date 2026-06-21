using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "Quest/Objectives/Talk Objective", fileName = "TalkObjective")]
    public class TalkObjective : QuestObjective {
        [SerializeField] private string npcId;
        [SerializeField] private string displayName;

        public string NpcId => npcId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? npcId : displayName;

        internal void Configure(string npcId) {
            Configure(npcId, null);
        }

        internal void Configure(string npcId, string displayName) {
            this.npcId = npcId;
            this.displayName = displayName;
        }

        protected internal override bool TryProgressTalk(string talkNpcId, ref int slotProgress) {
            if (talkNpcId != npcId)
                return false;
            if (slotProgress >= 1)
                return false;

            slotProgress = 1;
            return true;
        }

        protected internal override int GetProgressCap() => 1;
    }
}
