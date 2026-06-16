using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public class EnemyChaseState : EnemyBaseState {
        readonly NavMeshAgent agent;
        readonly PlayerDetector playerDetector;

        float stuckTimer;
        Vector3 lastPosition;
        float repathTimer;

        public EnemyChaseState(Enemy enemy, DynamicAnimator clipAnimator, NavMeshAgent agent, PlayerDetector playerDetector) : base(enemy, clipAnimator) {
            this.agent = agent;
            this.playerDetector = playerDetector;
            lastPosition = enemy.transform.position;
        }

        public override void OnEnter() {
            if (agent == null)
                return;

            stuckTimer = 0f;
            repathTimer = 0f;
            lastPosition = enemy.transform.position;
            ResetLocomotionTracking();

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = enemy.ChaseSpeed;

            SafeForcePlay(RunId);
            UpdateChaseDestination();
        }

        public override void Update() {
            if (agent == null)
                return;

            repathTimer += Time.deltaTime;
            if (repathTimer >= 0.15f) {
                repathTimer = 0f;
                UpdateChaseDestination();
            }

            if (EnemyLocomotion.UpdateStuckTimer(agent, ref stuckTimer, ref lastPosition, enemy.StuckResetSeconds))
                UpdateChaseDestination(force: true);

            SyncLocomotion(agent, forceRun: true);
        }

        void UpdateChaseDestination(bool force = false) {
            Transform player = playerDetector?.Player;
            if (player == null)
                return;

            if (!force && agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance > agent.stoppingDistance)
                return;

            EnemyLocomotion.TrySetDestination(agent, player.position);
        }
    }
}
