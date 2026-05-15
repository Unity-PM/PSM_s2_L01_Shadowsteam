using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer {
    [CreateAssetMenu(menuName = "NPC/Npc Dialogue Data", fileName = "NpcDialogueData")]
    public class NpcDialogueData : ScriptableObject {
        [SerializeField] private string npcDisplayName;
        [SerializeField] private string[] introPages = Array.Empty<string>();
        [SerializeField] private string[] reminderPages = Array.Empty<string>();
        [SerializeField] private string[] completionPages = Array.Empty<string>();

        public string NpcDisplayName => npcDisplayName;
        public IReadOnlyList<string> IntroPages => introPages ?? Array.Empty<string>();
        public IReadOnlyList<string> ReminderPages => reminderPages ?? Array.Empty<string>();
        public IReadOnlyList<string> CompletionPages => completionPages ?? Array.Empty<string>();
    }
}
