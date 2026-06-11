using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace Platformer {
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(PlayerDetector))]
    public class Enemy : Entity {
        [SerializeField] NavMeshAgent agent;
        [SerializeField] PlayerDetector playerDetector;
        [SerializeField] UniversalClipAnimator clipAnimator;
        
        [SerializeField] float wanderRadius = 10f;
        [SerializeField] float timeBetweenAttacks = 1f;
        
        StateMachine stateMachine;
        
        CountdownTimer attackTimer;

        void Awake() {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (playerDetector == null) playerDetector = GetComponent<PlayerDetector>();
            if (clipAnimator == null)
                clipAnimator = GetComponent<UniversalClipAnimator>() ?? GetComponentInChildren<UniversalClipAnimator>();
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
            if (attackTimer.IsRunning) return;
            if (playerDetector.PlayerHealth == null) return;
            
            attackTimer.Start();
            playerDetector.PlayerHealth.TakeDamage(10);
        }
    }
}