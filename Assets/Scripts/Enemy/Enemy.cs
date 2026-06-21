using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace Platformer {
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(PlayerDetector))]
    public class Enemy : Entity {
        [SerializeField] NavMeshAgent agent;
        [SerializeField] PlayerDetector playerDetector;
        [SerializeField] DynamicAnimator clipAnimator;
        
        [SerializeField] float wanderRadius = 10f;
        [SerializeField] float timeBetweenAttacks = 1f;
        [SerializeField] float damageToPlayer = 10f;
        [SerializeField] [Range(0f, 1f)] float attackDamageNormalizedTime = 0.5f;

        [Header("Clip ids (as in DynamicAnimator on this enemy)")]
        [SerializeField] string animIdleId = "Idle";
        [SerializeField] string animWalkId = "WalkFWD";
        [SerializeField] string animRunId = "Run";
        [SerializeField] string animAttackId = "Attack01";
        [SerializeField] string animDieId = "Die";

        [Header("Movement")]
        [SerializeField] float walkSpeed = 2.2f;
        [SerializeField] float chaseSpeed = 4.8f;
        [SerializeField] float stuckResetSeconds = 1.25f;

        [Header("Stability")]
        [SerializeField] bool stabilizeRootRigidbody = true;
        [SerializeField] float maxAgentSpeedMultiplier = 2.5f;
        [SerializeField] float navMeshRecoveryRadius = 4f;

        [Header("Turn toward target during attack")]
        [SerializeField] float attackTurnSpeedDegrees = 720f;
        [SerializeField] float attackCommitRangeBonus = 0.15f;

        internal string AnimIdleId => animIdleId;
        internal string AnimWalkId => animWalkId;
        internal string AnimRunId => animRunId;
        internal string AnimAttackId => animAttackId;
        internal string AnimDieId => animDieId;
        internal float AttackTurnSpeedDegrees => attackTurnSpeedDegrees;
        internal float WalkSpeed => walkSpeed;
        internal float ChaseSpeed => chaseSpeed;
        internal float StuckResetSeconds => stuckResetSeconds;
        internal bool IsAttackingPlayer => pendingAttackDamage;
        internal bool IsChasingPlayer => isChasingPlayer;
        internal bool HasActiveOrRecentAttack(float seconds)
        {
            return pendingAttackDamage || Time.time - attackStartedAt <= Mathf.Max(0f, seconds);
        }
        internal bool HasActiveOrRecentChase(float seconds)
        {
            return isChasingPlayer || Time.unscaledTime - lastChaseActivityTime <= Mathf.Max(0f, seconds);
        }
        internal void SetChasingPlayer(bool chasing)
        {
            isChasingPlayer = chasing;
            if (chasing)
                lastChaseActivityTime = Time.unscaledTime;
        }

        StateMachine stateMachine;
        CountdownTimer attackTimer;
        EnemyAudioController audioController;
        Rigidbody rootRigidbody;

        bool pendingAttackDamage;
        bool isChasingPlayer;
        float attackStartedAt = float.NegativeInfinity;
        float lastChaseActivityTime = float.NegativeInfinity;
        Vector3 spawnPosition;

        void Awake() {
            spawnPosition = transform.position;
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (playerDetector == null) playerDetector = GetComponent<PlayerDetector>();
            if (clipAnimator == null)
                clipAnimator = GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();
            if (audioController == null)
                audioController = GetComponent<EnemyAudioController>() ?? GetComponentInChildren<EnemyAudioController>();
            rootRigidbody = GetComponent<Rigidbody>();

            StabilizePhysicsBody();
            EnemyLocomotion.ConfigureAgent(agent, walkSpeed);
        }

        void Start() {
            attackTimer = new CountdownTimer(timeBetweenAttacks);
            
            stateMachine = new StateMachine();
            
            var wanderState = new EnemyWanderState(this, clipAnimator, agent, wanderRadius);
            var chaseState = new EnemyChaseState(this, clipAnimator, agent, playerDetector);
            var attackState = new EnemyAttackState(this, clipAnimator, agent, playerDetector);
            
            At(wanderState, chaseState, new FuncPredicate(() => playerDetector.CanDetectPlayer()));
            At(chaseState, wanderState, new FuncPredicate(() => !playerDetector.CanDetectPlayer()));
            At(chaseState, attackState, new FuncPredicate(() => playerDetector.CanAttackPlayer()));
            At(attackState, chaseState, new FuncPredicate(() => !playerDetector.CanAttackPlayer()));
            Any(wanderState, new FuncPredicate(() => playerDetector.IsPlayerDeadForCombat()));

            stateMachine.SetState(wanderState);
        }
        
        void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);

        void Update() {
            if (stateMachine == null)
                return;

            StabilizeAgent();
            stateMachine.Update();
            attackTimer.Tick(Time.deltaTime);
            StabilizeAgent();
        }
        
        void FixedUpdate() {
            if (stateMachine == null)
                return;

            stateMachine.FixedUpdate();
        }

        void OnDisable() {
            isChasingPlayer = false;
        }

        void StabilizePhysicsBody() {
            if (!stabilizeRootRigidbody || rootRigidbody == null)
                return;

            rootRigidbody.useGravity = false;

            // Zero out motion while the body is still dynamic. A kinematic body
            // rejects velocity assignments and logs a warning, so do this first.
            if (!rootRigidbody.isKinematic) {
                rootRigidbody.linearVelocity = Vector3.zero;
                rootRigidbody.angularVelocity = Vector3.zero;
            }

            rootRigidbody.isKinematic = true;
            rootRigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        void StabilizeAgent() {
            if (agent == null || !agent.enabled)
                return;

            EnemyLocomotion.RecoverToNavMesh(agent, spawnPosition, navMeshRecoveryRadius);
            EnemyLocomotion.ClampVelocity(agent, chaseSpeed * Mathf.Max(1f, maxAgentSpeedMultiplier));
        }

        public void TryStartAttack() {
            if (attackTimer.IsRunning || pendingAttackDamage)
                return;
            if (IsAttackAnimationStillPlaying())
                return;

            if (!playerDetector.CanAttackPlayer())
                return;

            attackTimer.Start();
            pendingAttackDamage = true;
            attackStartedAt = Time.time;
            PlayAttackAnimation();
            audioController?.PlayAttackSwing();
        }

        bool IsAttackAnimationStillPlaying() {
            if (clipAnimator == null || string.IsNullOrEmpty(animAttackId))
                return false;
            if (!string.Equals(clipAnimator.CurrentStateId, animAttackId, System.StringComparison.Ordinal))
                return false;

            return clipAnimator.IsMovementLocked;
        }

        public void UpdateAttack() {
            if (!pendingAttackDamage)
                return;

            if (!playerDetector.CanAttackPlayer(attackCommitRangeBonus)) {
                pendingAttackDamage = false;
                return;
            }

            if (!HasReachedAttackDamagePoint())
                return;

            ApplyAttackDamage();
            pendingAttackDamage = false;
        }

        public void CancelPendingAttack() {
            pendingAttackDamage = false;
        }

        bool HasReachedAttackDamagePoint() {
            if (clipAnimator != null
                && clipAnimator.TryGetStateNormalizedTime(animAttackId, out float normalizedTime))
                return normalizedTime >= attackDamageNormalizedTime;

            if (clipAnimator != null && clipAnimator.TryGetClipLength(animAttackId, out float clipLength))
                return Time.time - attackStartedAt >= clipLength * attackDamageNormalizedTime;

            return Time.time - attackStartedAt >= 0.25f;
        }

        void ApplyAttackDamage() {
            if (playerDetector.PlayerHealth != null) {
                playerDetector.PlayerHealth.TakeDamage(Mathf.RoundToInt(damageToPlayer));
                return;
            }

            if (playerDetector.Player == null)
                return;

            StatComponent playerStats = playerDetector.Player.GetComponent<StatComponent>();
            if (playerStats == null)
                return;

            EventBus.Publish(new StatChangeEvent(playerStats, StatType.HP, -damageToPlayer));
        }

        void PlayAttackAnimation() {
            if (clipAnimator == null || string.IsNullOrEmpty(animAttackId))
                return;

            if (clipAnimator.IsMovementLocked
                && string.Equals(clipAnimator.CurrentStateId, animAttackId, System.StringComparison.Ordinal))
                return;

            clipAnimator.ForcePlay(animAttackId);
        }
    }
}
