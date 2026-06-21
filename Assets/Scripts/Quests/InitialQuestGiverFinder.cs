using UnityEngine;

namespace Platformer {
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Quest/Initial Quest Giver Finder")]
    public class InitialQuestGiverFinder : MonoBehaviour {
        const string TriggerObjectName = "Initial Quest Approach Trigger";

        [Header("Quest")]
        [SerializeField] private QuestManager questManager;
        [SerializeField] private string questId = "main_find_wizard";
        [SerializeField] private string questTitle = "Find the Wizard";
        [TextArea(2, 5)]
        [SerializeField] private string questDescription = "Find the wizard quest giver.";
        [SerializeField] private QuestCategory questCategory = QuestCategory.Main;
        [SerializeField] private int experienceReward;
        [SerializeField] private string nextQuestId;

        [Header("Wizard NPC")]
        [SerializeField] private string npcId = "wizard";
        [SerializeField] private string npcDisplayName = "Wizard";

        [Header("New Game")]
        [SerializeField] private bool startWhenNoQuestSave = true;
        [SerializeField] private bool createQuestManagerIfMissing = true;
        [SerializeField] private bool keepQuestManagerAcrossScenes = true;
        [SerializeField] private bool createQuestHud = true;

        [Header("Approach Trigger")]
        [SerializeField] private bool completeOnApproach = true;
        [SerializeField] private bool createTriggerZone = true;
        [SerializeField] private float approachRadius = 3f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool registerWaypoint = true;

        bool questRegistered;
        string registeredWaypointKey;

        void Awake() {
            EnsureQuestManager();
            RegisterQuest();
            EnsureTriggerZone();
        }

        void Start() {
            EnsureQuestManager();
            RegisterQuest();

            if (createQuestHud && questManager != null)
                QuestRuntimeHud.Ensure(questManager);

            if (ShouldStartQuest())
                questManager.TryStartQuest(questId);
        }

        void OnEnable() {
            RegisterWaypoint();
        }

        void OnDisable() {
            UnregisterWaypoint();
        }

        void OnTriggerEnter(Collider other) {
            HandleTriggerEnter(other);
        }

        public void CompleteQuestByApproach() {
            if (!completeOnApproach || string.IsNullOrEmpty(npcId))
                return;

            EnsureQuestManager();
            RegisterQuest();

            if (questManager == null)
                return;

            questManager.NotifyNpcTalked(npcId);
        }

        internal void HandleTriggerEnter(Collider other) {
            if (!IsPlayerCollider(other))
                return;

            CompleteQuestByApproach();
        }

        void EnsureQuestManager() {
            if (questManager != null)
                return;

            questManager = FindFirstObjectByType<QuestManager>();
            if (questManager == null && createQuestManagerIfMissing) {
                var go = new GameObject("QuestManager");
                questManager = go.AddComponent<QuestManager>();
            }

            if (questManager != null && keepQuestManagerAcrossScenes && questManager.transform.parent == null)
                DontDestroyOnLoad(questManager.gameObject);
        }

        void RegisterQuest() {
            if (questRegistered || questManager == null || string.IsNullOrEmpty(questId))
                return;

            TalkObjective talkObjective = ScriptableObject.CreateInstance<TalkObjective>();
            talkObjective.hideFlags = HideFlags.DontSave;
            talkObjective.Configure(npcId, npcDisplayName);

            QuestDefinition quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.hideFlags = HideFlags.DontSave;
            quest.Configure(
                questId,
                questTitle,
                questDescription,
                questCategory,
                new QuestObjective[] { talkObjective },
                experienceReward,
                nextQuestId);

            questManager.RegisterQuestDefinition(quest);
            questRegistered = true;
            RegisterWaypoint();
        }

        bool ShouldStartQuest() {
            if (questManager == null || string.IsNullOrEmpty(questId))
                return false;
            if (questManager.IsQuestActive(questId) || questManager.IsQuestCompleted(questId))
                return false;
            if (startWhenNoQuestSave && QuestProgressSaveService.HasSave())
                return false;

            return true;
        }

        void EnsureTriggerZone() {
            if (!createTriggerZone)
                return;

            Transform triggerTransform = transform.Find(TriggerObjectName);
            if (triggerTransform == null) {
                var triggerObject = new GameObject(TriggerObjectName);
                triggerTransform = triggerObject.transform;
                triggerTransform.SetParent(transform, false);
            }

            SphereCollider sphere = triggerTransform.GetComponent<SphereCollider>();
            if (sphere == null)
                sphere = triggerTransform.gameObject.AddComponent<SphereCollider>();

            sphere.isTrigger = true;
            sphere.radius = Mathf.Max(0.25f, approachRadius);

            InitialQuestGiverFinderTrigger trigger = triggerTransform.GetComponent<InitialQuestGiverFinderTrigger>();
            if (trigger == null)
                trigger = triggerTransform.gameObject.AddComponent<InitialQuestGiverFinderTrigger>();

            trigger.Configure(this);
        }

        bool IsPlayerCollider(Collider other) {
            if (other == null)
                return false;

            Transform current = other.transform;
            while (current != null) {
                if (!string.IsNullOrEmpty(playerTag)) {
                    try {
                        if (current.CompareTag(playerTag))
                            return true;
                    } catch (UnityException) {
                        break;
                    }
                }

                if (current.GetComponent<MovementBrain>() != null)
                    return true;

                current = current.parent;
            }

            return false;
        }

        void RegisterWaypoint() {
            if (!registerWaypoint || !isActiveAndEnabled || string.IsNullOrEmpty(npcId))
                return;

            string key = QuestWaypointRegistry.KeyForTalk(npcId);
            if (string.IsNullOrEmpty(key) || registeredWaypointKey == key)
                return;

            UnregisterWaypoint();
            QuestWaypointRegistry.Register(key, transform);
            registeredWaypointKey = key;
        }

        void UnregisterWaypoint() {
            if (string.IsNullOrEmpty(registeredWaypointKey))
                return;

            QuestWaypointRegistry.Unregister(registeredWaypointKey, transform);
            registeredWaypointKey = null;
        }

#if UNITY_EDITOR
        void OnValidate() {
            approachRadius = Mathf.Max(0.25f, approachRadius);
        }
#endif
    }

    sealed class InitialQuestGiverFinderTrigger : MonoBehaviour {
        InitialQuestGiverFinder owner;

        public void Configure(InitialQuestGiverFinder owner) {
            this.owner = owner;
        }

        void OnTriggerEnter(Collider other) {
            owner?.HandleTriggerEnter(other);
        }
    }
}
