using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public class EnemyAttackState : EnemyBaseState {
        readonly NavMeshAgent agent;
        readonly Transform player;
        
        public EnemyAttackState(Enemy enemy, DynamicAnimator clipAnimator, NavMeshAgent agent, Transform player) : base(enemy, clipAnimator) {
            this.agent = agent;
            this.player = player;
        }
        
        public override void OnEnter() {
            agent.updateRotation = false;
            agent.isStopped = true;
            agent.ResetPath();
            SafeForcePlay(AttackId);
        }

        public override void Update() {
            FacePlayerGrounded(enemy.AttackTurnSpeedDegrees);
            enemy.Attack();
        }

        void FacePlayerGrounded(float turnSpeedDegrees) {
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
            agent.updateRotation = true;
        }
    }
}