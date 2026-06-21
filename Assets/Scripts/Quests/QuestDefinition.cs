using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "Quest/Quest Definition", fileName = "QuestDefinition")]
    public class QuestDefinition : ScriptableObject {
        [SerializeField] private string questId;
        [SerializeField] private string title;
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [SerializeField] private QuestCategory category = QuestCategory.Side;
        [SerializeField] private QuestObjective[] objectives = { };
        [SerializeField] private int experienceReward;
        [SerializeField] private string nextQuestId;

        public string QuestId => questId;
        public string Title => title;
        public string Description => description;
        public QuestCategory Category => category;
        public IReadOnlyList<QuestObjective> Objectives => objectives ?? Array.Empty<QuestObjective>();
        public int ExperienceReward => experienceReward;
        public string NextQuestId => nextQuestId;

        internal void Configure(
            string questId,
            string title,
            string description,
            QuestCategory category,
            QuestObjective[] objectives,
            int experienceReward,
            string nextQuestId) {
            this.questId = questId;
            this.title = title;
            this.description = description;
            this.category = category;
            this.objectives = objectives ?? Array.Empty<QuestObjective>();
            this.experienceReward = experienceReward;
            this.nextQuestId = nextQuestId;
        }
    }
}
