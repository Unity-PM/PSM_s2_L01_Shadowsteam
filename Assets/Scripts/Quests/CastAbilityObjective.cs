using System;
using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "Quest/Objectives/Cast Ability Objective", fileName = "CastAbilityObjective")]
    public class CastAbilityObjective : QuestObjective {
        [Tooltip("Skill id passed to SkillManager.CastSkill (e.g. \"Fireball\").")]
        [SerializeField] private string abilityId = "Fireball";
        [SerializeField] private string displayName;
        [SerializeField] private int requiredCasts = 1;

        public string AbilityId => abilityId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? abilityId : displayName;

        internal void Configure(string abilityId) => Configure(abilityId, null, 1);

        internal void Configure(string abilityId, string displayName, int requiredCasts) {
            this.abilityId = abilityId;
            this.displayName = displayName;
            this.requiredCasts = Mathf.Max(1, requiredCasts);
        }

        protected internal override bool TryProgressAbilityCast(string castAbilityId, ref int slotProgress) {
            if (string.IsNullOrEmpty(abilityId) ||
                !string.Equals(castAbilityId, abilityId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (slotProgress >= requiredCasts)
                return false;

            slotProgress++;
            return true;
        }

        protected internal override int GetProgressCap() => Mathf.Max(1, requiredCasts);
    }
}
