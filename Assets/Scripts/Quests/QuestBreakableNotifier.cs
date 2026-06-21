using D;
using UnityEngine;

namespace Platformer {
    [DisallowMultipleComponent]
    [AddComponentMenu("Quest/Quest Breakable Notifier")]
    public class QuestBreakableNotifier : MonoBehaviour {
        [SerializeField] private string breakableId = "portal_boulder";
        [SerializeField] private QuestManager questManager;
        [Tooltip("Counts the quest when this object's D Rigid demolition event fires.")]
        [SerializeField] private bool notifyOnDeconstructionDemolished = true;
        [Tooltip("Optional fallback. Keep off if only real D Rigid destruction should count.")]
        [SerializeField] private bool notifyOnDisable;
        [Tooltip("Optional fallback. Keep off if only real D Rigid destruction should count.")]
        [SerializeField] private bool notifyOnDestroy;

        DRigid rigid;
        bool notified;
        string registeredWaypointKey;

        void Awake() {
            ResolveRigid();
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();
        }

        void OnEnable() {
            ResolveRigid();
            if (notifyOnDeconstructionDemolished && rigid != null)
                rigid.demolitionEvent.LocalEvent += OnRigidDemolished;

            RegisterWaypoint();
        }

        void OnDisable() {
            if (notifyOnDeconstructionDemolished && rigid != null)
                rigid.demolitionEvent.LocalEvent -= OnRigidDemolished;

            if (notifyOnDisable)
                NotifyBroken();

            UnregisterWaypoint();
        }

        void OnDestroy() {
            if (notifyOnDestroy)
                NotifyBroken();

            UnregisterWaypoint();
        }

        public void Configure(string breakableId, QuestManager questManager) {
            UnregisterWaypoint();
            this.breakableId = breakableId;
            this.questManager = questManager;
            RegisterWaypoint();
        }

        void OnRigidDemolished(DRigid demolishedRigid) {
            if (demolishedRigid == rigid)
                NotifyBroken();
        }

        void ResolveRigid() {
            if (rigid != null)
                return;

            rigid = GetComponent<DRigid>();
            if (rigid == null)
                rigid = GetComponentInParent<DRigid>();
            if (rigid == null)
                rigid = GetComponentInChildren<DRigid>();
        }

        public void NotifyBroken() {
            if (notified || string.IsNullOrEmpty(breakableId))
                return;

            notified = true;
            UnregisterWaypoint();
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();

            if (questManager != null)
                questManager.NotifyObjectBroken(breakableId);
        }

        void RegisterWaypoint() {
            if (!isActiveAndEnabled || notified)
                return;

            string key = QuestWaypointRegistry.KeyForBreak(breakableId);
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
    }
}
