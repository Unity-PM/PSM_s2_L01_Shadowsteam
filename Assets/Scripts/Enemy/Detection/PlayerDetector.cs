using UnityEngine;
using Utilities;

namespace Platformer {
    public class PlayerDetector : MonoBehaviour {
        [SerializeField] float detectionAngle = 120f; // Cone in front of enemy
        [SerializeField] float detectionRadius = 12f; // Large circle around enemy
        [SerializeField] float innerDetectionRadius = 6f; // Small circle around enemy
        [SerializeField] float detectionCooldown = 0.2f; // Time between detections
        [SerializeField] float attackRange = 2f; // Distance from enemy to player to attack
        [SerializeField] float attackRangePadding = 0.15f;
        [SerializeField] float minimumAttackRange = 1.25f;
        [SerializeField] float maximumEffectiveAttackRange = 1.75f;
        [SerializeField] float maximumAttackCommitBonus = 0.2f;
        [SerializeField] float aggroMemorySeconds = 2.5f;
        [SerializeField] float aggroMemoryRadiusPadding = 2f;
        [SerializeField] bool useColliderDistanceForCombatRanges = true;
        
        public Transform Player { get; private set; }
        public Health PlayerHealth { get; private set; }
        public float EffectiveAttackRange {
            get {
                float baseRange = Mathf.Max(attackRange, minimumAttackRange);
                baseRange = Mathf.Min(baseRange, Mathf.Max(0.5f, maximumEffectiveAttackRange));
                return baseRange + Mathf.Clamp(attackRangePadding, 0f, 0.2f);
            }
        }

        StatComponent cachedPlayerStats;

        CountdownTimer detectionTimer;
        bool hasDetectedPlayer;
        float lastPlayerSeenAt = float.NegativeInfinity;

        IDetectionStrategy detectionStrategy;

        float EffectiveDetectionAngle => Mathf.Clamp(Mathf.Max(detectionAngle, 90f), 0f, 360f);
        float EffectiveDetectionRadius => Mathf.Max(detectionRadius, innerDetectionRadius, 0.5f);
        float EffectiveInnerDetectionRadius => Mathf.Clamp(innerDetectionRadius, 0f, EffectiveDetectionRadius);
        float EffectiveDetectionCooldown => Mathf.Clamp(detectionCooldown, 0.05f, 0.35f);
        float ForgetDistance => EffectiveDetectionRadius + Mathf.Max(0f, aggroMemoryRadiusPadding);

        void Awake() {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null) {
                Debug.LogError("Player with tag 'Player' was not found.");
                return;
            }

            Player = playerObject.transform;
            PlayerHealth = Player.GetComponent<Health>();
            cachedPlayerStats = Player.GetComponent<StatComponent>();
        }

        void Start() {
            EnsureDetectionRuntime();
        }
        
        void Update() => detectionTimer.Tick(Time.deltaTime);

        void RefreshPlayerReferences() {
            if (Player != null)
                return;

            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
                return;

            Player = playerObject.transform;
            PlayerHealth = Player.GetComponent<Health>();
            cachedPlayerStats = Player.GetComponent<StatComponent>();
        }

        /// <summary>Игрок мёртв по <see cref="Health"/> или флагу <see cref="StatComponent.IsDead"/>.</summary>
        public bool IsPlayerDeadForCombat() {
            RefreshPlayerReferences();

            if (PlayerHealth != null && PlayerHealth.IsDead)
                return true;

            if (cachedPlayerStats == null && Player != null)
                cachedPlayerStats = Player.GetComponent<StatComponent>();

            return cachedPlayerStats != null && cachedPlayerStats.IsDead;
        }

        public bool CanDetectPlayer() {
            RefreshPlayerReferences();
            EnsureDetectionRuntime();

            if (Player == null || detectionStrategy == null)
                return false;

            if (IsPlayerDeadForCombat()) {
                detectionTimer.Stop();
                return false;
            }

            float flatDistance = GetFlatDistanceToPlayer();
            bool insideInnerRadius = flatDistance <= EffectiveInnerDetectionRadius;
            bool insideDetectionRadius = flatDistance <= EffectiveDetectionRadius;
            bool insideMemoryRadius = flatDistance <= ForgetDistance;

            if (insideInnerRadius) {
                RememberPlayer();
                return true;
            }

            if (hasDetectedPlayer && insideMemoryRadius) {
                if (insideDetectionRadius) {
                    RememberPlayer();
                    return true;
                }

                if (Time.time - lastPlayerSeenAt <= Mathf.Max(0f, aggroMemorySeconds))
                    return true;
            }

            if (hasDetectedPlayer && !insideMemoryRadius)
                ForgetPlayer();

            if (detectionTimer.IsRunning && insideMemoryRadius) {
                RememberPlayer();
                return true;
            }

            bool detectedNow = detectionStrategy.Execute(Player, transform, detectionTimer);
            if (detectedNow) {
                RememberPlayer();
                return true;
            }

            if (hasDetectedPlayer
                && insideMemoryRadius
                && Time.time - lastPlayerSeenAt <= Mathf.Max(0f, aggroMemorySeconds))
                return true;

            return false;
        }

        public bool CanAttackPlayer() => CanAttackPlayer(0f);

        public bool CanAttackPlayer(float extraRange) {
            RefreshPlayerReferences();

            if (Player == null || IsPlayerDeadForCombat())
                return false;

            float allowedRange = EffectiveAttackRange + Mathf.Clamp(extraRange, 0f, maximumAttackCommitBonus);
            return GetFlatDistanceToPlayer() <= allowedRange;
        }

        public Vector3 GetPlayerLookPosition(Vector3 fromPosition) {
            RefreshPlayerReferences();
            return Player != null ? GetClosestPoint(Player, fromPosition) : fromPosition;
        }

        float GetFlatDistanceToPlayer() {
            if (Player == null)
                return float.PositiveInfinity;

            Vector3 enemyPoint = useColliderDistanceForCombatRanges
                ? GetClosestPoint(transform, Player.position)
                : transform.position;
            Vector3 playerPoint = useColliderDistanceForCombatRanges
                ? GetClosestPoint(Player, enemyPoint)
                : Player.position;

            enemyPoint.y = 0f;
            playerPoint.y = 0f;
            return Vector3.Distance(enemyPoint, playerPoint);
        }

        static Vector3 GetClosestPoint(Transform root, Vector3 fromPosition) {
            if (root == null)
                return fromPosition;

            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            Vector3 bestPoint = root.position;
            float bestDistance = float.PositiveInfinity;
            bool foundSolidCollider = false;

            for (int pass = 0; pass < 2; pass++) {
                bool allowTriggers = pass == 1;
                for (int i = 0; i < colliders.Length; i++) {
                    Collider col = colliders[i];
                    if (col == null || !col.enabled)
                        continue;
                    if (!allowTriggers && col.isTrigger)
                        continue;
                    if (foundSolidCollider && col.isTrigger)
                        continue;

                    Vector3 point = col.ClosestPoint(fromPosition);
                    float distance = (point - fromPosition).sqrMagnitude;
                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    bestPoint = point;
                    foundSolidCollider = !col.isTrigger;
                }

                if (foundSolidCollider || bestDistance < float.PositiveInfinity)
                    return bestPoint;
            }

            return root.position;
        }

        void RememberPlayer() {
            hasDetectedPlayer = true;
            lastPlayerSeenAt = Time.time;
        }

        void ForgetPlayer() {
            hasDetectedPlayer = false;
            lastPlayerSeenAt = float.NegativeInfinity;
        }

        void EnsureDetectionRuntime() {
            if (detectionTimer == null)
                detectionTimer = new CountdownTimer(EffectiveDetectionCooldown);

            if (detectionStrategy == null)
                detectionStrategy = new ConeDetectionStrategy(
                    EffectiveDetectionAngle,
                    EffectiveDetectionRadius,
                    EffectiveInnerDetectionRadius);
        }
        
        public void SetDetectionStrategy(IDetectionStrategy detectionStrategy) => this.detectionStrategy = detectionStrategy;
        
        void OnDrawGizmos() {
            Gizmos.color = Color.red;

            // Draw a spheres for the radii
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            Gizmos.DrawWireSphere(transform.position, innerDetectionRadius);
            Gizmos.DrawWireSphere(transform.position, EffectiveAttackRange);

            // Calculate our cone directions
            Vector3 forwardConeDirection = Quaternion.Euler(0, detectionAngle / 2, 0) * transform.forward * detectionRadius;
            Vector3 backwardConeDirection = Quaternion.Euler(0, -detectionAngle / 2, 0) * transform.forward * detectionRadius;

            // Draw lines to represent the cone
            Gizmos.DrawLine(transform.position, transform.position + forwardConeDirection);
            Gizmos.DrawLine(transform.position, transform.position + backwardConeDirection);
        }
    }
}
