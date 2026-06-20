using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public class EnemyChaseState : EnemyBaseState {
        readonly NavMeshAgent agent;
        readonly PlayerDetector playerDetector;

        float stuckTimer;
        Vector3 lastPosition;
        float repathTimer;
        Vector3 lastDestination;
        bool hasDestination;

        public EnemyChaseState(Enemy enemy, DynamicAnimator clipAnimator, NavMeshAgent agent, PlayerDetector playerDetector) : base(enemy, clipAnimator) {
            this.agent = agent;
            this.playerDetector = playerDetector;
            lastPosition = enemy.transform.position;
        }

        public override void OnEnter() {
            enemy.SetChasingPlayer(true);

            if (agent == null)
                return;

            stuckTimer = 0f;
            repathTimer = 0f;
            lastPosition = enemy.transform.position;
            hasDestination = false;
            ResetLocomotionTracking();

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = enemy.ChaseSpeed;
            if (playerDetector != null)
                agent.stoppingDistance = Mathf.Max(0.45f, playerDetector.EffectiveAttackRange * 0.85f);

            SafeForcePlay(RunId);
            UpdateChaseDestination(force: true);
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

        public override void OnExit() {
            enemy.SetChasingPlayer(false);
        }

        void UpdateChaseDestination(bool force = false) {
            Transform player = playerDetector?.Player;
            if (player == null)
                return;

            Vector3 destination = playerDetector.GetPlayerLookPosition(enemy.transform.position);
            if (!force
                && hasDestination
                && agent.hasPath
                && agent.pathStatus == NavMeshPathStatus.PathComplete
                && Vector3.Distance(lastDestination, destination) < 0.35f)
                return;

            if (EnemyLocomotion.TrySetDestination(agent, destination)) {
                lastDestination = destination;
                hasDestination = true;
            }
        }
    }
}
