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

        [Header("Clip ids (как в DynamicAnimator на этом враге)")]
        [SerializeField] string animIdleId = "Idle";
        [SerializeField] string animWalkId = "WalkFWD";
        [SerializeField] string animRunId = "Run";
        [SerializeField] string animAttackId = "Attack01";
        [SerializeField] string animDieId = "Die";

        [Header("Поворот к цели в атаке")]
        [SerializeField] float attackTurnSpeedDegrees = 720f;

        internal string AnimIdleId => animIdleId;
        internal string AnimWalkId => animWalkId;
        internal string AnimRunId => animRunId;
        internal string AnimAttackId => animAttackId;
        internal string AnimDieId => animDieId;
        internal float AttackTurnSpeedDegrees => attackTurnSpeedDegrees;

        StateMachine stateMachine;
        
        CountdownTimer attackTimer;

        void Awake() {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (playerDetector == null) playerDetector = GetComponent<PlayerDetector>();
            if (clipAnimator == null)
                clipAnimator = GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();
        }

        void Start() {
            attackTimer = new CountdownTimer(timeBetweenAttacks);
            
            stateMachine = new StateMachine();
            
            var wanderState = new EnemyWanderState(this, clipAnimator, agent, wanderRadius);
            var chaseState = new EnemyChaseState(this, clipAnimator, agent, playerDetector.Player);
            var attackState = new EnemyAttackState(this, clipAnimator, agent, playerDetector.Player);
            
            At(wanderState, chaseState, new FuncPredicate(() => playerDetector.CanDetectPlayer()));
            At(chaseState, wanderState, new FuncPredicate(() => !playerDetector.CanDetectPlayer()));
            At(chaseState, attackState, new FuncPredicate(() => playerDetector.CanAttackPlayer()));
            At(attackState, chaseState, new FuncPredicate(() => !playerDetector.CanAttackPlayer()));

            stateMachine.SetState(wanderState);
        }
        
        void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);

        void Update() {
            stateMachine.Update();
            attackTimer.Tick(Time.deltaTime);
        }
        
        void FixedUpdate() {
            stateMachine.FixedUpdate();
        }
        
        public void Attack() {
            if (attackTimer.IsRunning)
                return;

            if (!playerDetector.CanAttackPlayer())
                return;

            if (playerDetector.PlayerHealth != null) {
                attackTimer.Start();
                playerDetector.PlayerHealth.TakeDamage(Mathf.RoundToInt(damageToPlayer));
                return;
            }

            if (playerDetector.Player == null)
                return;

            StatComponent playerStats = playerDetector.Player.GetComponent<StatComponent>();
            if (playerStats == null)
                return;

            attackTimer.Start();
            EventBus.Publish(new StatChangeEvent(playerStats, StatType.HP, -damageToPlayer));
        }
    }
}