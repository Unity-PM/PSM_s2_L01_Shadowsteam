using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public class EnemyAttackState : EnemyBaseState {
        readonly NavMeshAgent agent;
        readonly PlayerDetector playerDetector;

        public EnemyAttackState(Enemy enemy, DynamicAnimator clipAnimator, NavMeshAgent agent, PlayerDetector playerDetector) : base(enemy, clipAnimator) {
            this.agent = agent;
            this.playerDetector = playerDetector;
        }

        public override void OnEnter() {
            if (agent == null)
                return;

            ResetLocomotionTracking();
            agent.updateRotation = false;
            agent.isStopped = true;
            agent.ResetPath();
            enemy.TryStartAttack();
        }

        public override void Update() {
            if (agent == null)
                return;

            FacePlayerGrounded(enemy.AttackTurnSpeedDegrees);
            enemy.UpdateAttack();
            enemy.TryStartAttack();
        }

        void FacePlayerGrounded(float turnSpeedDegrees) {
            Transform player = playerDetector?.Player;
            if (player == null)
                return;

            Vector3 toPlayer = player.position - enemy.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f)
                return;

            Quaternion target = Quaternion.LookRotation(toPlayer.normalized);
            float step = turnSpeedDegrees * Time.deltaTime;
            enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, target, step);
        }

        public override void OnExit() {
            enemy.CancelPendingAttack();

            if (agent == null)
                return;

            agent.updateRotation = true;
            agent.isStopped = false;
        }
    }
}
