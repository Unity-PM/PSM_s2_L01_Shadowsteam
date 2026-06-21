using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Platformer {
    /// <summary>
    /// NPC dialogue that offers only the main quest; UI is wired via UnityEvents and buttons (Accept / Next / Close).
    /// </summary>
    public class NpcMainQuestDialogueController : MonoBehaviour {
        [Header("References")]
        [SerializeField] private NpcInteractionZone interactionZone;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private NpcDialogueData dialogueData;
        [SerializeField] private string offeredMainQuestId;
        [Tooltip("Passed to QuestManager when the dialogue opens (TalkObjective), if not empty.")]
        [SerializeField] private string npcIdForTalkObjective;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private bool useKeyboardEIfNoAction = true;

        [Header("Blocked by other main quest")]
        [SerializeField] private string blockedByOtherMainMessage =
            "Finish your current story quest first.";

        [Header("Talk objective")]
        [SerializeField] private bool notifyTalkObjectiveWhenDialogueOpens = true;

        [Header("Events (hook UI here)")]
        [SerializeField] private UnityEvent<bool> onDialogueSessionChanged;
        [SerializeField] private UnityEvent<string> onDialogueLineChanged;
        [SerializeField] private UnityEvent<string> onBlockedByOtherMainQuest;
        [SerializeField] private UnityEvent onQuestAcceptedSuccessfully;

        readonly List<string> currentPages = new();
        int pageIndex;
        bool dialogueOpen;

        void OnEnable() {
            if (interactAction != null && interactAction.action != null)
                interactAction.action.Enable();
        }

        void Awake() {
            if (interactionZone == null)
                interactionZone = GetComponent<NpcInteractionZone>();
        }

        void Update() {
            if (interactionZone == null || !interactionZone.PlayerInRange)
                return;

            if (!WasInteractPressed())
                return;

            if (dialogueOpen)
                ShowNextPage();
            else
                TryOpenDialogue();
        }

        bool WasInteractPressed() {
            if (interactAction != null && interactAction.action != null)
                return interactAction.action.WasPressedThisFrame();

#if ENABLE_INPUT_SYSTEM
            if (useKeyboardEIfNoAction && Keyboard.current != null)
                return Keyboard.current.eKey.wasPressedThisFrame;
#else
            if (useKeyboardEIfNoAction)
                return Input.GetKeyDown(KeyCode.E);
#endif
            return false;
        }

        /// <summary>"Start dialogue" button / the first E press is already handled in Update; can be called from UI.</summary>
        public void TryOpenDialogue() {
            if (questManager == null || dialogueData == null || dialogueOpen || interactionZone == null ||
                !interactionZone.PlayerInRange)
                return;

            NpcOfferedQuestPhase phase = questManager.GetNpcQuestPhase(offeredMainQuestId);
            bool blocked = false;
            if (phase == NpcOfferedQuestPhase.NotOffered && questManager.HasActiveMainQuest()) {
                if (questManager.TryGetActiveMainQuestId(out string activeId))
                    blocked = activeId != offeredMainQuestId;
            }

            if (blocked) {
                onBlockedByOtherMainQuest?.Invoke(blockedByOtherMainMessage);
                return;
            }

            FillPagesForPhase(phase);
            if (currentPages.Count == 0) {
                onDialogueLineChanged?.Invoke(string.Empty);
                return;
            }

            dialogueOpen = true;
            pageIndex = 0;
            onDialogueSessionChanged?.Invoke(true);

            if (notifyTalkObjectiveWhenDialogueOpens && !string.IsNullOrEmpty(npcIdForTalkObjective))
                questManager.NotifyNpcDialogueOpened(npcIdForTalkObjective);

            EmitCurrentLine();
        }

        void FillPagesForPhase(NpcOfferedQuestPhase phase) {
            currentPages.Clear();
            IReadOnlyList<string> src = phase switch {
                NpcOfferedQuestPhase.Completed => dialogueData.CompletionPages,
                NpcOfferedQuestPhase.ActiveInProgress => dialogueData.ReminderPages,
                _ => dialogueData.IntroPages
            };

            if (src == null)
                return;

            for (int i = 0; i < src.Count; i++) {
                string line = src[i];
                if (!string.IsNullOrEmpty(line))
                    currentPages.Add(line);
            }
        }

        /// <summary>Next page; from a UI button or a repeated E press while the dialogue is open.</summary>
        public void ShowNextPage() {
            if (!dialogueOpen)
                return;

            pageIndex++;
            if (pageIndex >= currentPages.Count) {
                CloseDialogue();
                return;
            }

            EmitCurrentLine();
        }

        public void CloseDialogue() {
            if (!dialogueOpen)
                return;

            dialogueOpen = false;
            currentPages.Clear();
            pageIndex = 0;
            onDialogueSessionChanged?.Invoke(false);
            onDialogueLineChanged?.Invoke(string.Empty);
        }

        /// <summary>"Accept quest" button — main only, a single active story quest.</summary>
        public void AcceptQuest() {
            if (questManager == null || string.IsNullOrEmpty(offeredMainQuestId))
                return;

            if (questManager.TryAcceptMainQuestFromNpc(offeredMainQuestId))
                onQuestAcceptedSuccessfully?.Invoke();
        }

        void EmitCurrentLine() {
            if (pageIndex >= 0 && pageIndex < currentPages.Count)
                onDialogueLineChanged?.Invoke(currentPages[pageIndex]);
        }

        void OnDisable() {
            CloseDialogue();
        }

#if UNITY_EDITOR
        void OnValidate() {
            if (interactionZone == null)
                interactionZone = GetComponent<NpcInteractionZone>();
        }
#endif
    }
}