using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Platformer {
    /// <summary>
    /// Диалог с NPC и выдача только main-квеста; UI подключается через UnityEvents и кнопки (Accept / Next / Close).
    /// </summary>
    public class NpcMainQuestDialogueController : MonoBehaviour {
        [Header("References")]
        [SerializeField] private NpcInteractionZone interactionZone;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private NpcDialogueData dialogueData;
        [SerializeField] private string offeredMainQuestId;
        [Tooltip("Передаётся в QuestManager при открытии диалога (TalkObjective), если не пусто.")]
        [SerializeField] private string npcIdForTalkObjective;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private bool useKeyboardEIfNoAction = true;

        [Header("Blocked by other main quest")]
        [SerializeField] private string blockedByOtherMainMessage =
            "Сначала завершите текущий сюжетный квест.";

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

        /// <summary>Кнопка «начать диалог» / первое нажатие E уже обрабатывается в Update; можно вызывать с UI.</summary>
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

        /// <summary>Следующая страница; с кнопки UI или повторное E пока диалог открыт.</summary>
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

        /// <summary>Кнопка «Принять квест» — только main, один активный сюжетный.</summary>
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