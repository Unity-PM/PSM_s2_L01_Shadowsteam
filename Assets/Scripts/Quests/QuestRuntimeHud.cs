using System.Collections.Generic;
using UnityEngine;

namespace Platformer {
    [AddComponentMenu("Quest/Quest Runtime HUD")]
    public class QuestRuntimeHud : MonoBehaviour {
        [SerializeField] private QuestManager questManager;
        [SerializeField] private bool showWhenQuestActive = true;
        [Header("Layout")]
        [SerializeField] private float rightMargin = 16f;
        [SerializeField] private float topOffset = 96f;
        [SerializeField] private float panelWidth = 208f;
        [Header("Navigation")]
        [SerializeField] private bool showQuestWaypoint = true;
        [SerializeField] private float waypointTopOffset = 18f;
        [SerializeField] private float waypointWidth = 360f;
        [SerializeField] private float waypointHeight = 30f;
        [SerializeField] private float waypointDotSize = 8f;
        [SerializeField] private float waypointVisibleAngle = 110f;

        Texture2D panelTexture;
        Texture2D borderTexture;
        Texture2D accentTexture;
        Texture2D navigationTexture;
        Texture2D waypointTexture;
        Texture2D waypointEdgeTexture;
        GUIStyle titleStyle;
        GUIStyle bodyStyle;
        GUIStyle metaStyle;
        GUIStyle waypointStyle;
        string toastText;
        float toastUntil;
        QuestManager subscribedManager;

        public static QuestRuntimeHud Ensure(QuestManager manager) {
            QuestRuntimeHud hud = FindFirstObjectByType<QuestRuntimeHud>();
            if (hud == null) {
                var go = new GameObject("Quest Runtime HUD");
                hud = go.AddComponent<QuestRuntimeHud>();
                DontDestroyOnLoad(go);
            }

            hud.SetQuestManager(manager);
            return hud;
        }

        void OnEnable() {
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();

            BindManager();
        }

        void OnDisable() {
            UnbindManager();
        }

        public void SetQuestManager(QuestManager manager) {
            if (questManager == manager)
                return;

            UnbindManager();
            questManager = manager;
            BindManager();
        }

        void BindManager() {
            if (questManager == null || subscribedManager == questManager)
                return;

            questManager.QuestJournalChanged += OnQuestJournalChanged;
            subscribedManager = questManager;
        }

        void UnbindManager() {
            if (subscribedManager == null)
                return;

            subscribedManager.QuestJournalChanged -= OnQuestJournalChanged;
            subscribedManager = null;
        }

        void OnQuestJournalChanged() {
            if (TryGetDisplayQuest(out QuestRuntimeState state, out int objectiveIndex))
                toastText = objectiveIndex >= 0
                    ? FormatObjective(state.Definition.Objectives[objectiveIndex], state.Progress[objectiveIndex])
                    : state.Definition.Title;
            else
                toastText = string.Empty;

            toastUntil = Time.time + 4f;
        }

        void OnGUI() {
            if (!showWhenQuestActive || questManager == null)
                return;

            EnsureStyles();
            if (!TryGetDisplayQuest(out QuestRuntimeState state, out int objectiveIndex))
                return;

            string title = string.IsNullOrEmpty(state.Definition.Title)
                ? state.Definition.QuestId
                : state.Definition.Title;
            string objective = objectiveIndex >= 0
                ? FormatObjective(state.Definition.Objectives[objectiveIndex], state.Progress[objectiveIndex])
                : "Quest complete";

            if (objectiveIndex >= 0)
                DrawQuestWaypoint(state.Definition.Objectives[objectiveIndex]);

            float width = Mathf.Min(panelWidth, Screen.width - rightMargin * 2f);
            float height = 92f;
            Rect panel = new Rect(Screen.width - rightMargin - width, topOffset, width, height);
            DrawPanel(panel);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 16f), "QUEST", metaStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 28f, panel.width - 28f, 24f), title, titleStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 56f, panel.width - 28f, 28f), objective, bodyStyle);

            if (!string.IsNullOrEmpty(toastText) && Time.time < toastUntil) {
                float toastWidth = Mathf.Min(360f, Screen.width - rightMargin * 2f);
                Rect toast = new Rect(Screen.width - rightMargin - toastWidth, panel.y + panel.height + 8f, toastWidth,
                    54f);
                DrawPanel(toast);
                GUI.Label(new Rect(toast.x + 14f, toast.y + 15f, toast.width - 28f, 24f),
                    $"New objective: {toastText}", bodyStyle);
            }
        }

        bool TryGetDisplayQuest(out QuestRuntimeState state, out int objectiveIndex) {
            state = null;
            objectiveIndex = -1;

            IReadOnlyList<QuestRuntimeState> active = questManager.ActiveQuests;
            for (int i = 0; i < active.Count; i++) {
                if (active[i]?.Definition?.Category == QuestCategory.Main) {
                    state = active[i];
                    break;
                }
            }

            if (state == null && active.Count > 0)
                state = active[0];
            if (state == null)
                return false;

            IReadOnlyList<QuestObjective> objectives = state.Definition.Objectives;
            for (int i = 0; i < objectives.Count; i++) {
                QuestObjective objective = objectives[i];
                if (objective == null)
                    continue;

                if (state.Progress[i] < objective.GetProgressCap()) {
                    objectiveIndex = i;
                    break;
                }
            }

            return true;
        }

        void DrawQuestWaypoint(QuestObjective objective) {
            if (!showQuestWaypoint || objective == null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            Transform cameraTransform = mainCamera.transform;
            if (!QuestWaypointRegistry.TryGetBestTarget(objective, cameraTransform.position, out Transform target))
                return;

            Vector3 toTarget = target.position - cameraTransform.position;
            Vector3 flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (flatDirection.sqrMagnitude < 0.01f || flatForward.sqrMagnitude < 0.01f)
                return;

            float signedAngle = Vector3.SignedAngle(flatForward.normalized, flatDirection.normalized, Vector3.up);
            bool targetBehind = Mathf.Abs(signedAngle) > waypointVisibleAngle;
            float clampedAngle = Mathf.Clamp(signedAngle, -waypointVisibleAngle, waypointVisibleAngle);
            Rect rect = GetWaypointRect();
            float normalized = Mathf.InverseLerp(-waypointVisibleAngle, waypointVisibleAngle, clampedAngle);
            float dotX = Mathf.Lerp(rect.x + 12f, rect.xMax - 12f, normalized);

            if (targetBehind)
                dotX = signedAngle < 0f ? rect.x + 12f : rect.xMax - 12f;

            GUI.DrawTexture(rect, navigationTexture);
            GUI.DrawTexture(new Rect(rect.center.x - 1f, rect.y + 6f, 2f, rect.height - 12f), accentTexture);

            float dotSize = Mathf.Max(4f, waypointDotSize);
            Rect dot = new Rect(dotX - dotSize * 0.5f, rect.y + 5f, dotSize, dotSize);
            GUI.DrawTexture(dot, targetBehind ? waypointEdgeTexture : waypointTexture);

            string distance = $"{Mathf.RoundToInt(toTarget.magnitude)}m";
            Vector2 labelSize = waypointStyle.CalcSize(new GUIContent(distance));
            float labelX = Mathf.Clamp(dotX - labelSize.x * 0.5f, rect.x + 6f, rect.xMax - labelSize.x - 6f);
            GUI.Label(new Rect(labelX, rect.y + 14f, labelSize.x, 13f), distance, waypointStyle);
        }

        Rect GetWaypointRect() {
            const float screenMargin = 16f;
            float maxScreenWidth = Mathf.Max(96f, Screen.width - screenMargin * 2f);
            float width = Mathf.Min(waypointWidth, maxScreenWidth);
            float x = (Screen.width - width) * 0.5f;

            float reservedRightStart = Screen.width - rightMargin - panelWidth - 12f;
            if (reservedRightStart > screenMargin + 140f && x + width > reservedRightStart) {
                width = Mathf.Min(width, reservedRightStart - screenMargin);
                width = Mathf.Clamp(width, 120f, maxScreenWidth);
                x = screenMargin;
            }

            return new Rect(x, waypointTopOffset, width, waypointHeight);
        }

        static string FormatObjective(QuestObjective objective, int current) {
            int cap = objective.GetProgressCap();
            switch (objective) {
                case KillObjective kill:
                    return $"Defeat {kill.TargetTag}: {current}/{cap}";
                case CollectObjective collect:
                    return $"Collect {collect.DisplayName}: {current}/{cap}";
                case BreakObjectObjective breakObject:
                    return $"Break {breakObject.BreakableId}: {current}/{cap}";
                case ReachLocationObjective reach:
                    return $"Reach {reach.LocationId}: {current}/{cap}";
                case TalkObjective talk:
                    return $"Talk to {talk.NpcId}: {current}/{cap}";
                default:
                    return $"Objective: {current}/{cap}";
            }
        }

        void EnsureStyles() {
            if (panelTexture != null)
                return;

            panelTexture = MakeTexture(new Color(0.04f, 0.047f, 0.098f, 0.86f));
            borderTexture = MakeTexture(new Color(0.83f, 0.69f, 0.22f, 0.38f));
            accentTexture = MakeTexture(new Color(0.83f, 0.69f, 0.22f, 0.78f));
            navigationTexture = MakeTexture(new Color(0.04f, 0.047f, 0.098f, 0.70f));
            waypointTexture = MakeTexture(new Color(0.83f, 0.69f, 0.22f, 0.96f));
            waypointEdgeTexture = MakeTexture(new Color(1f, 0.45f, 0.15f, 0.96f));

            titleStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                wordWrap = false,
                clipping = TextClipping.Clip
            };

            bodyStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 12,
                normal = { textColor = new Color(1f, 1f, 1f, 0.78f) },
                wordWrap = true
            };

            metaStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.83f, 0.69f, 0.22f, 0.95f) }
            };

            waypointStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.88f) }
            };
        }

        void DrawPanel(Rect rect) {
            GUI.DrawTexture(rect, borderTexture);
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), panelTexture);
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, 3f, rect.height - 2f), accentTexture);
        }

        static Texture2D MakeTexture(Color color) {
            var texture = new Texture2D(1, 1) {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
