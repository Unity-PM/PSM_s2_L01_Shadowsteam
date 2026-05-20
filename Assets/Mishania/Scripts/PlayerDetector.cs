using UnityEngine;
using Utilities;

namespace Platformer {
    public class PlayerDetector : MonoBehaviour {
        [SerializeField] float detectionAngle = 60f; // Cone in front of enemy
        [SerializeField] float detectionRadius = 10f; // Large circle around enemy
        [SerializeField] float innerDetectionRadius = 5f; // Small circle around enemy
        [SerializeField] float detectionCooldown = 1f; // Time between detections
        [SerializeField] float attackRange = 2f; // Distance from enemy to player to attack
        
        public Transform Player { get; private set; }
        public Health PlayerHealth { get; private set; }

        StatComponent cachedPlayerStats;

        CountdownTimer detectionTimer;

        IDetectionStrategy detectionStrategy;

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
            detectionTimer = new CountdownTimer(detectionCooldown);
            detectionStrategy = new ConeDetectionStrategy(detectionAngle, detectionRadius, innerDetectionRadius);
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

            if (Player == null || detectionStrategy == null)
                return false;

            if (IsPlayerDeadForCombat()) {
                detectionTimer.Stop();
                return false;
            }

            return detectionTimer.IsRunning || detectionStrategy.Execute(Player, transform, detectionTimer);
        }

        public bool CanAttackPlayer() {
            if (Player == null || IsPlayerDeadForCombat())
                return false;

            var detectorPosition = transform.position;
            var playerPosition = Player.position;
            detectorPosition.y = 0f;
            playerPosition.y = 0f;

            return Vector3.Distance(detectorPosition, playerPosition) <= attackRange;
        }
        
        public void SetDetectionStrategy(IDetectionStrategy detectionStrategy) => this.detectionStrategy = detectionStrategy;
        
        void OnDrawGizmos() {
            Gizmos.color = Color.red;

            // Draw a spheres for the radii
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            Gizmos.DrawWireSphere(transform.position, innerDetectionRadius);

            // Calculate our cone directions
            Vector3 forwardConeDirection = Quaternion.Euler(0, detectionAngle / 2, 0) * transform.forward * detectionRadius;
            Vector3 backwardConeDirection = Quaternion.Euler(0, -detectionAngle / 2, 0) * transform.forward * detectionRadius;

            // Draw lines to represent the cone
            Gizmos.DrawLine(transform.position, transform.position + forwardConeDirection);
            Gizmos.DrawLine(transform.position, transform.position + backwardConeDirection);
        }
    }
}