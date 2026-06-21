using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public class EnemyWanderState : EnemyBaseState {
        readonly NavMeshAgent agent;
        readonly Vector3 startPoint;
        readonly float wanderRadius;

        float stuckTimer;
        Vector3 lastPosition;

        public EnemyWanderState(Enemy enemy, DynamicAnimator clipAnimator, NavMeshAgent agent, float wanderRadius) : base(enemy, clipAnimator) {
            this.agent = agent;
            this.startPoint = enemy.transform.position;
            this.wanderRadius = wanderRadius;
            lastPosition = enemy.transform.position;
        }

        public override void OnEnter() {
            if (agent == null)
                return;

            stuckTimer = 0f;
            lastPosition = enemy.transform.position;
            ResetLocomotionTracking();

            if (!EnemyLocomotion.RecoverToNavMesh(agent, startPoint, 4f)) {
                SafeForcePlay(IdleId);
                return;
            }

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.stoppingDistance = 0.35f;
            agent.speed = enemy.WalkSpeed;

            PickNewDestination();
            SafeForcePlay(WalkId);
        }

        public override void Update() {
            if (agent == null)
                return;

            if (!EnemyLocomotion.RecoverToNavMesh(agent, startPoint, 4f)) {
                SafePlay(IdleId);
                return;
            }

            if (EnemyLocomotion.UpdateStuckTimer(agent, ref stuckTimer, ref lastPosition, enemy.StuckResetSeconds)
                || EnemyLocomotion.HasReachedDestination(agent)) {
                PickNewDestination();
            }

            SyncLocomotion(agent);
        }

        void PickNewDestination() {
            stuckTimer = 0f;

            if (!EnemyLocomotion.TrySampleNavigablePoint(startPoint, wanderRadius, out Vector3 destination)) {
                SafePlay(IdleId);
                return;
            }

            if (!EnemyLocomotion.TrySetDestination(agent, destination))
                SafePlay(IdleId);
        }
    }
}
