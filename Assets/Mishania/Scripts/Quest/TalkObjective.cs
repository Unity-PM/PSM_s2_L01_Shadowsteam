using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "Quest/Objectives/Talk Objective", fileName = "TalkObjective")]
    public class TalkObjective : QuestObjective {
        [SerializeField] private string npcId;

        public string NpcId => npcId;

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
