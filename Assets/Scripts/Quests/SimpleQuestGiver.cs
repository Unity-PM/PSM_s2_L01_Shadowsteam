using System;
using System.Collections.Generic;
using D;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Platformer {
    [DisallowMultipleComponent]
    [AddComponentMenu("Quest/Simple Quest Giver")]
    public class SimpleQuestGiver : MonoBehaviour {
        [Header("Interaction")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float interactionRadius = 3f;
        [SerializeField] private KeyCode acceptKey = KeyCode.E;

        [Header("Overhead Text")]
        [SerializeField] private bool createOverheadText = true;
        [SerializeField] private bool showOverheadOnlyInRange = true;
        [SerializeField] private string overheadText = "Take a quest";
        [SerializeField] private Vector3 overheadOffset = new Vector3(0f, 2.3f, 0f);
        [SerializeField] private Color overheadColor = Color.white;

        [Header("NPC Lines")]
        [SerializeField] private string offerLine =
            "The portal path is blocked. Clear the field, gather supplies, break the boulder, and enter the portal.";
        [SerializeField] private string acceptLine = "Good. Start with the enemies in the field.";
        [SerializeField] private string inProgressLine = "Follow the current objective. The path opens step by step.";
        [SerializeField] private string completedLine = "You did it. The boss is next.";
        [SerializeField] private float speechSeconds = 4f;

        [Header("Quest Chain")]
        [SerializeField] private string questIdPrefix = "main_shadowsteam";
        [SerializeField, HideInInspector] private string fieldEnemyKillId = "field_enemy";
        [Tooltip("Drag exact enemy GameObjects from the scene here. If filled, only these enemies count for the field kill quest.")]
        [SerializeField] private List<GameObject> fieldEnemyObjects = new();
        [Tooltip("Optional fallback amount used only when Field Enemy Objects is empty.")]
        [SerializeField] private int requiredEnemyKills;
        [Tooltip("Drag pickup GameObjects from the scene here. The quest will wait for these exact objects.")]
        [SerializeField] private List<GameObject> collectItemObjects = new();
        [Tooltip("Optional fallback ids if you do not want to drag scene objects.")]
        [SerializeField] private string[] fallbackCollectItemIds = Array.Empty<string>();
        [SerializeField] private string breakableId = "portal_boulder";
        [Tooltip("Drag boulder GameObjects with D Rigid here. The break quest counts when D Rigid demolishes them.")]
        [SerializeField] private List<GameObject> breakableObjects = new();
        [SerializeField] private string portalLocationId = "boss_portal";
        [Tooltip("Drag GameObjects with TeleportTrigger here. The quest counts when that TeleportTrigger starts teleporting the player.")]
        [SerializeField] private List<GameObject> portalTeleportTriggerObjects = new();
        [SerializeField, HideInInspector] private string bossKillId = "boss";
        [Tooltip("Drag exact boss/enemy GameObjects here. If filled, all listed enemies must be defeated for the boss quest.")]
        [SerializeField] private List<GameObject> bossEnemyObjects = new();
        [SerializeField] private int rewardXpPerStep = 50;
        [Tooltip("Skill id for the intro 'try the fireball' quest. Must match the SkillManager skill id cast by key 1.")]
        [SerializeField] private string fireballSkillId = "Fireball";

        [Header("Scene Auto Setup")]
        [SerializeField] private bool createQuestManagerIfMissing = true;
        [SerializeField] private bool keepQuestManagerAcrossScenes = true;
        [SerializeField] private bool createQuestHud = true;
        [SerializeField] private bool autoMarkCurrentSceneEnemies = true;
        [SerializeField] private bool autoMarkBreakableDRigids = true;
        [SerializeField] private string breakableObjectNameContains = "Boulder";
        [SerializeField] private bool autoAddPortalReachTrigger = true;
        [SerializeField] private string portalObjectNameContains = "Boss";

        QuestManager questManager;
        Transform player;
        GameObject overheadTextObject;
        bool playerInRange;
        bool chainRegistered;
        string speechLine;
        float speechUntil;
        GUIStyle promptBoxStyle;
        GUIStyle promptTitleStyle;
        GUIStyle promptBodyStyle;

        string FireballQuestId => $"{questIdPrefix}_00_try_fireball";
        string KillQuestId => $"{questIdPrefix}_01_clear_field";
        string CollectQuestId => $"{questIdPrefix}_02_collect_supplies";
        string BreakQuestId => $"{questIdPrefix}_03_break_boulder";
        string PortalQuestId => $"{questIdPrefix}_04_enter_portal";
        string BossQuestId => $"{questIdPrefix}_05_defeat_boss";

        void Awake() {
            EnsureQuestManager();
            CreateInteractionTrigger();
            CreateOverheadText();
        }

        void Start() {
            RegisterQuestChain();
            AutoSetupSceneHooks();

            if (createQuestHud)
                QuestRuntimeHud.Ensure(questManager);
        }

        void Update() {
            RefreshPlayerRange();
            UpdateOverheadTextVisibility();

            if (!playerInRange)
                return;

            if (!HasAnyChainProgress() && WasAcceptPressed())
                AcceptQuest();
        }

        void OnGUI() {
            if (!playerInRange)
                return;

            EnsurePromptStyles();

            bool hasProgress = HasAnyChainProgress();
            bool finalComplete = questManager != null && questManager.IsQuestCompleted(BossQuestId);
            string title = hasProgress ? "Quest" : "Take a quest";
            string body = finalComplete ? completedLine : hasProgress ? inProgressLine : offerLine;
            string action = hasProgress ? string.Empty : $"Press {acceptKey} to accept";

            float width = Mathf.Min(620f, Screen.width - 40f);
            float height = string.IsNullOrEmpty(action) ? 116f : 148f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 42f, width, height);
            GUI.Box(rect, GUIContent.none, promptBoxStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 16f, rect.width - 40f, 30f), title, promptTitleStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 52f, rect.width - 40f, 48f), body, promptBodyStyle);
            if (!string.IsNullOrEmpty(action))
                GUI.Label(new Rect(rect.x + 20f, rect.y + 108f, rect.width - 40f, 26f), action, promptBodyStyle);

            if (!string.IsNullOrEmpty(speechLine) && Time.time < speechUntil) {
                Rect speech = new Rect((Screen.width - width) * 0.5f, rect.y - 76f, width, 58f);
                GUI.Box(speech, GUIContent.none, promptBoxStyle);
                GUI.Label(new Rect(speech.x + 20f, speech.y + 17f, speech.width - 40f, 24f), speechLine,
                    promptBodyStyle);
            }
        }

        public void AcceptQuest() {
            EnsureQuestManager();
            RegisterQuestChain();

            if (questManager == null)
                return;

            if (questManager.TryAcceptMainQuestFromNpc(FireballQuestId)) {
                speechLine = acceptLine;
                speechUntil = Time.time + speechSeconds;
                UpdateOverheadTextVisibility();
            } else if (!HasAnyChainProgress()) {
                speechLine = "Another main quest is already active.";
                speechUntil = Time.time + speechSeconds;
            }
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

        void RegisterQuestChain() {
            if (questManager == null || chainRegistered)
                return;

            SetupCollectItemMarkers();
            SetupEnemyMarkersFromList(fieldEnemyObjects, fieldEnemyKillId);
            SetupEnemyMarkersFromList(bossEnemyObjects, bossKillId);
            SetupBreakableMarkersFromList();
            SetupPortalTeleportMarkersFromList();

            questManager.RegisterQuestDefinition(CreateQuest(FireballQuestId, "Try the Fireball",
                "Cast your fireball spell. Press 1 to launch it.",
                new QuestObjective[] { CreateCastAbilityObjective(fireballSkillId) }, KillQuestId));

            int kills = ResolveRequiredFieldEnemyKills();
            questManager.RegisterQuestDefinition(CreateQuest(KillQuestId, "Clear the Field",
                "Defeat the enemies guarding the field.",
                new QuestObjective[] { CreateKillObjective(fieldEnemyKillId, kills) }, CollectQuestId));

            questManager.RegisterQuestDefinition(CreateQuest(CollectQuestId, "Gather Supplies",
                "Pick up the useful items near the field.",
                CreateCollectObjectives(), BreakQuestId));

            questManager.RegisterQuestDefinition(CreateQuest(BreakQuestId, "Break the Boulder",
                "Destroy the boulder blocking the portal path.",
                new QuestObjective[] { CreateBreakObjective(breakableId) }, PortalQuestId));

            questManager.RegisterQuestDefinition(CreateQuest(PortalQuestId, "Enter the Portal",
                "Reach the portal and step through it.",
                new QuestObjective[] { CreateReachObjective(portalLocationId) }, BossQuestId));

            questManager.RegisterQuestDefinition(CreateQuest(BossQuestId, "Defeat the Boss",
                "Find the boss and defeat it.",
                new QuestObjective[] { CreateKillObjective(bossKillId, ResolveRequiredBossKills()) }, string.Empty));

            chainRegistered = true;
        }

        QuestDefinition CreateQuest(string id, string title, string description, QuestObjective[] objectives,
            string nextQuestId) {
            QuestDefinition quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.hideFlags = HideFlags.DontSave;
            quest.Configure(id, title, description, QuestCategory.Main, objectives, rewardXpPerStep, nextQuestId);
            return quest;
        }

        QuestObjective[] CreateCollectObjectives() {
            var objectives = new List<QuestObjective>();

            if (collectItemObjects != null) {
                for (int i = 0; i < collectItemObjects.Count; i++) {
                    GameObject itemObject = collectItemObjects[i];
                    if (itemObject == null)
                        continue;

                    objectives.Add(CreateCollectObjective(
                        GetCollectItemObjectiveId(i, itemObject),
                        1,
                        GetCollectItemDisplayName(itemObject)));
                }
            }

            if (objectives.Count == 0 && fallbackCollectItemIds != null) {
                for (int i = 0; i < fallbackCollectItemIds.Length; i++) {
                    string id = fallbackCollectItemIds[i];
                    if (!string.IsNullOrWhiteSpace(id))
                        objectives.Add(CreateCollectObjective(id, 1, id));
                }
            }

            if (objectives.Count == 0)
                objectives.Add(CreateCollectObjective("QuestItem", 1, "Quest Item"));

            return objectives.ToArray();
        }

        void SetupCollectItemMarkers() {
            if (collectItemObjects == null)
                return;

            for (int i = 0; i < collectItemObjects.Count; i++) {
                GameObject itemObject = collectItemObjects[i];
                if (itemObject == null)
                    continue;

                QuestCollectableItem marker = itemObject.GetComponent<QuestCollectableItem>();
                if (marker == null)
                    marker = itemObject.AddComponent<QuestCollectableItem>();

                marker.Configure(
                    GetCollectItemObjectiveId(i, itemObject),
                    GetCollectItemDisplayName(itemObject),
                    questManager);
            }
        }

        void SetupEnemyMarkersFromList(List<GameObject> enemyObjects, string killId) {
            if (enemyObjects == null)
                return;

            for (int i = 0; i < enemyObjects.Count; i++)
                SetupEnemyMarker(enemyObjects[i], killId);
        }

        void SetupEnemyMarker(GameObject enemyObject, string killId) {
            GameObject targetObject = ResolveKillTargetObject(enemyObject, true);
            if (targetObject == null)
                return;

            QuestKillTarget marker = targetObject.GetComponent<QuestKillTarget>();
            if (marker == null)
                marker = targetObject.AddComponent<QuestKillTarget>();

            marker.Configure(killId, questManager);
        }

        void SetupBreakableMarkersFromList() {
            if (breakableObjects == null)
                return;

            for (int i = 0; i < breakableObjects.Count; i++)
                SetupBreakableMarker(breakableObjects[i]);
        }

        void SetupBreakableMarker(GameObject breakableObject) {
            if (breakableObject == null)
                return;

            QuestBreakableNotifier marker = breakableObject.GetComponent<QuestBreakableNotifier>();
            if (marker == null)
                marker = breakableObject.AddComponent<QuestBreakableNotifier>();

            marker.Configure(breakableId, questManager);
        }

        void SetupPortalTeleportMarkersFromList() {
            if (portalTeleportTriggerObjects == null)
                return;

            for (int i = 0; i < portalTeleportTriggerObjects.Count; i++)
                SetupPortalTeleportMarker(portalTeleportTriggerObjects[i]);
        }

        void SetupPortalTeleportMarker(GameObject portalObject) {
            if (portalObject == null)
                return;

            TeleportTrigger teleportTrigger = portalObject.GetComponent<TeleportTrigger>();
            if (teleportTrigger == null) {
                Debug.LogWarning(
                    $"Quest portal object '{portalObject.name}' does not have TeleportTrigger.",
                    portalObject);
                return;
            }

            QuestTeleportTriggerNotifier marker = portalObject.GetComponent<QuestTeleportTriggerNotifier>();
            if (marker == null)
                marker = portalObject.AddComponent<QuestTeleportTriggerNotifier>();

            marker.Configure(portalLocationId, questManager, playerTag);
        }

        string GetCollectItemObjectiveId(int index, GameObject itemObject) {
            string objectPart = itemObject != null ? SanitizeIdPart(itemObject.name) : "item";
            return $"{questIdPrefix}_collect_{index}_{objectPart}";
        }

        static string GetCollectItemDisplayName(GameObject itemObject) {
            if (itemObject == null)
                return "Quest Item";

            ItemPickup pickup = itemObject.GetComponent<ItemPickup>();
            ItemSO itemData = pickup != null ? pickup.CurrentItemData : null;
            if (itemData != null && !string.IsNullOrWhiteSpace(itemData.itemName))
                return itemData.itemName;

            return itemObject.name;
        }

        static string SanitizeIdPart(string value) {
            if (string.IsNullOrWhiteSpace(value))
                return "item";

            char[] chars = value.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++) {
                char c = chars[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    continue;

                chars[i] = '_';
            }

            return new string(chars).Trim('_');
        }

        static GameObject ResolveKillTargetObject(GameObject enemyObject, bool warnIfMissingDeathSource) {
            if (enemyObject == null)
                return null;

            if (HasDeathSource(enemyObject))
                return enemyObject;

            StatComponent stats = enemyObject.GetComponentInParent<StatComponent>() ??
                                  enemyObject.GetComponentInChildren<StatComponent>();
            if (stats != null)
                return stats.gameObject;

            Health health = enemyObject.GetComponentInParent<Health>() ??
                            enemyObject.GetComponentInChildren<Health>();
            if (health != null)
                return health.gameObject;

            Enemy enemy = enemyObject.GetComponent<Enemy>() ??
                          enemyObject.GetComponentInParent<Enemy>() ??
                          enemyObject.GetComponentInChildren<Enemy>();
            if (enemy != null && HasDeathSource(enemy.gameObject))
                return enemy.gameObject;

            if (warnIfMissingDeathSource) {
                Debug.LogWarning(
                    $"Quest enemy object '{enemyObject.name}' does not have Health or StatComponent on itself, parent, or child.",
                    enemyObject);
            }

            return null;
        }

        static bool HasDeathSource(GameObject candidate) =>
            candidate.GetComponent<Health>() != null ||
            candidate.GetComponent<StatComponent>() != null;

        static CastAbilityObjective CreateCastAbilityObjective(string abilityId) {
            CastAbilityObjective objective = ScriptableObject.CreateInstance<CastAbilityObjective>();
            objective.hideFlags = HideFlags.DontSave;
            objective.Configure(abilityId);
            return objective;
        }

        static KillObjective CreateKillObjective(string killId, int amount) {
            KillObjective objective = ScriptableObject.CreateInstance<KillObjective>();
            objective.hideFlags = HideFlags.DontSave;
            objective.Configure(killId, amount);
            return objective;
        }

        static CollectObjective CreateCollectObjective(string itemId, int amount, string displayName) {
            CollectObjective objective = ScriptableObject.CreateInstance<CollectObjective>();
            objective.hideFlags = HideFlags.DontSave;
            objective.Configure(itemId, amount, displayName);
            return objective;
        }

        static BreakObjectObjective CreateBreakObjective(string id) {
            BreakObjectObjective objective = ScriptableObject.CreateInstance<BreakObjectObjective>();
            objective.hideFlags = HideFlags.DontSave;
            objective.Configure(id, 1);
            return objective;
        }

        static ReachLocationObjective CreateReachObjective(string id) {
            ReachLocationObjective objective = ScriptableObject.CreateInstance<ReachLocationObjective>();
            objective.hideFlags = HideFlags.DontSave;
            objective.Configure(id);
            return objective;
        }

        int ResolveRequiredFieldEnemyKills() {
            int listedEnemyCount = CountKillTargetsInList(fieldEnemyObjects);
            if (listedEnemyCount > 0)
                return listedEnemyCount;

            if (requiredEnemyKills > 0)
                return requiredEnemyKills;

            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return Mathf.Max(1, enemies.Length);
        }

        int ResolveRequiredBossKills() {
            int listedBossCount = CountKillTargetsInList(bossEnemyObjects);
            return listedBossCount > 0 ? listedBossCount : 1;
        }

        static int CountKillTargetsInList(List<GameObject> enemyObjects) {
            if (enemyObjects == null)
                return 0;

            var uniqueTargets = new HashSet<GameObject>();
            for (int i = 0; i < enemyObjects.Count; i++) {
                GameObject targetObject = ResolveKillTargetObject(enemyObjects[i], false);
                if (targetObject != null)
                    uniqueTargets.Add(targetObject);
            }

            return uniqueTargets.Count;
        }

        static bool HasConfiguredObjects(List<GameObject> objects) {
            if (objects == null)
                return false;

            for (int i = 0; i < objects.Count; i++) {
                if (objects[i] != null)
                    return true;
            }

            return false;
        }

        void AutoSetupSceneHooks() {
            if (questManager == null)
                return;

            if (autoMarkCurrentSceneEnemies && !HasConfiguredObjects(fieldEnemyObjects)) {
                Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < enemies.Length; i++) {
                    SetupEnemyMarker(enemies[i].gameObject, fieldEnemyKillId);
                }
            }

            if (autoMarkBreakableDRigids) {
                DRigid[] rigids = FindObjectsByType<DRigid>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < rigids.Length; i++) {
                    if (!NameMatches(rigids[i].name, breakableObjectNameContains))
                        continue;

                    SetupBreakableMarker(rigids[i].gameObject);
                }
            }

            if (autoAddPortalReachTrigger) {
                bool hasManualPortalTargets =
                    portalTeleportTriggerObjects != null && portalTeleportTriggerObjects.Count > 0;
                if (hasManualPortalTargets)
                    return;

                TeleportTrigger[] portals =
                    FindObjectsByType<TeleportTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < portals.Length; i++) {
                    if (!NameMatches(portals[i].name, portalObjectNameContains))
                        continue;

                    SetupPortalTeleportMarker(portals[i].gameObject);
                }
            }
        }

        static bool NameMatches(string objectName, string requiredPart) {
            if (string.IsNullOrWhiteSpace(requiredPart))
                return true;

            return objectName != null &&
                   objectName.IndexOf(requiredPart, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        bool HasAnyChainProgress() {
            if (questManager == null)
                return false;

            return questManager.IsQuestActive(FireballQuestId) ||
                   questManager.IsQuestActive(KillQuestId) ||
                   questManager.IsQuestActive(CollectQuestId) ||
                   questManager.IsQuestActive(BreakQuestId) ||
                   questManager.IsQuestActive(PortalQuestId) ||
                   questManager.IsQuestActive(BossQuestId) ||
                   questManager.IsQuestCompleted(FireballQuestId) ||
                   questManager.IsQuestCompleted(KillQuestId) ||
                   questManager.IsQuestCompleted(CollectQuestId) ||
                   questManager.IsQuestCompleted(BreakQuestId) ||
                   questManager.IsQuestCompleted(PortalQuestId) ||
                   questManager.IsQuestCompleted(BossQuestId);
        }

        void CreateInteractionTrigger() {
            var triggerObject = new GameObject("Quest Interaction Trigger");
            triggerObject.transform.SetParent(transform, false);

            SphereCollider sphere = triggerObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = Mathf.Max(0.25f, interactionRadius);

            SimpleQuestGiverTrigger trigger = triggerObject.AddComponent<SimpleQuestGiverTrigger>();
            trigger.Configure(this);
        }

        void CreateOverheadText() {
            if (!createOverheadText)
                return;

            overheadTextObject = new GameObject("Quest Overhead Text");
            overheadTextObject.transform.SetParent(transform, false);
            overheadTextObject.transform.localPosition = overheadOffset;

            TextMesh text = overheadTextObject.AddComponent<TextMesh>();
            text.text = overheadText;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.12f;
            text.fontSize = 64;
            text.color = overheadColor;

            QuestBillboard billboard = overheadTextObject.AddComponent<QuestBillboard>();
            billboard.Configure(overheadOffset);

            UpdateOverheadTextVisibility();
        }

        void UpdateOverheadTextVisibility() {
            if (overheadTextObject == null)
                return;

            bool visible = !HasAnyChainProgress();
            if (showOverheadOnlyInRange)
                visible &= playerInRange;

            overheadTextObject.SetActive(visible);
        }

        void RefreshPlayerRange() {
            if (player == null)
                player = FindPlayer();

            if (player != null) {
                float distance = Vector3.Distance(transform.position, player.position);
                playerInRange = distance <= interactionRadius;
            }
        }

        Transform FindPlayer() {
            if (!string.IsNullOrEmpty(playerTag)) {
                try {
                    GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);
                    if (taggedPlayer != null)
                        return taggedPlayer.transform;
                } catch (UnityException) {
                    // The configured tag does not exist. Fall back to player controller lookup.
                }
            }

            MovementBrain movementBrain = FindFirstObjectByType<MovementBrain>();
            return movementBrain != null ? movementBrain.transform : null;
        }

        internal void NotifyTriggerEnter(Collider other) {
            if (IsPlayerCollider(other))
                playerInRange = true;
        }

        internal void NotifyTriggerExit(Collider other) {
            if (IsPlayerCollider(other))
                playerInRange = false;
        }

        bool IsPlayerCollider(Collider other) {
            if (other == null)
                return false;

            Transform current = other.transform;
            while (current != null) {
                if (!string.IsNullOrEmpty(playerTag)) {
                    try {
                        if (current.CompareTag(playerTag)) {
                            player = current;
                            return true;
                        }
                    } catch (UnityException) {
                        break;
                    }
                }

                if (current.GetComponent<MovementBrain>() != null) {
                    player = current;
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        bool WasAcceptPressed() {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(acceptKey);
#else
            return false;
#endif
        }

        void EnsurePromptStyles() {
            if (promptBoxStyle != null)
                return;

            promptBoxStyle = new GUIStyle(GUI.skin.box) {
                padding = new RectOffset(16, 16, 12, 12)
            };
            promptTitleStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                wordWrap = true
            };
            promptBodyStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 16,
                normal = { textColor = Color.white },
                wordWrap = true
            };
        }
    }

    sealed class SimpleQuestGiverTrigger : MonoBehaviour {
        SimpleQuestGiver owner;

        public void Configure(SimpleQuestGiver owner) {
            this.owner = owner;
        }

        void OnTriggerEnter(Collider other) {
            owner?.NotifyTriggerEnter(other);
        }

        void OnTriggerExit(Collider other) {
            owner?.NotifyTriggerExit(other);
        }
    }

    sealed class QuestBillboard : MonoBehaviour {
        Vector3 baseLocalPosition;

        public void Configure(Vector3 baseLocalPosition) {
            this.baseLocalPosition = baseLocalPosition;
        }

        void Update() {
            Camera cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

            float bob = Mathf.Sin(Time.time * 2.2f) * 0.08f;
            transform.localPosition = baseLocalPosition + Vector3.up * bob;
        }
    }
}
