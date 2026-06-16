using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "Quest/Objectives/Kill Objective", fileName = "KillObjective")]
    public class KillObjective : QuestObjective {
        [Tooltip("Unity Tag on enemy GameObjects that count toward this objective.")]
        [SerializeField] private string targetTag = "Enemy";
        [SerializeField] private int requiredKills = 1;

        public string TargetTag => targetTag;
        public int RequiredKills => requiredKills;

        protected internal override bool TryProgressKill(string killedEnemyTag, ref int slotProgress) {
            if (string.IsNullOrEmpty(targetTag) || killedEnemyTag != targetTag)
                return false;
            if (slotProgress >= requiredKills)
                return false;

            slotProgress++;
            return true;
        }

        protected internal override int GetProgressCap() => Mathf.Max(1, requiredKills);
    }
}
