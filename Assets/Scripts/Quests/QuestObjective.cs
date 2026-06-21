using UnityEngine;

namespace Platformer {
    public abstract class QuestObjective : ScriptableObject {
        /// <summary>Typically <see cref="GameObject.tag"/> of the killed enemy (see <see cref="KillObjective"/>).</summary>
        protected internal virtual bool TryProgressKill(string killedEnemyTag, ref int slotProgress) => false;

        protected internal virtual bool TryProgressCollect(string itemId, int amount, ref int slotProgress) =>
            false;

        protected internal virtual bool TryProgressTalk(string npcId, ref int slotProgress) => false;

        protected internal virtual bool TryProgressReach(string enteredLocationId, ref int slotProgress) =>
            false;

        protected internal virtual bool TryProgressBreakObject(string breakableId, int amount, ref int slotProgress) =>
            false;

        protected internal virtual bool TryProgressAbilityCast(string abilityId, ref int slotProgress) => false;

        protected internal abstract int GetProgressCap();
    }
}
