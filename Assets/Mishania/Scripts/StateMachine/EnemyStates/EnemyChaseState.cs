using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public class EnemyChaseState : EnemyBaseState {
        readonly NavMeshAgent agent;
        readonly Transform player;
        
        public EnemyChaseState(Enemy enemy, UniversalClipAnimator clipAnimator, NavMeshAgent agent, Transform player) : base(enemy, clipAnimator) {
            this.agent = agent;
            this.player = player;
        }
        
        public override void OnEnter() {
            Debug.Log("Chase");
            agent.isStopped = false;
            SafePlay(RunId);
        }
        
        public override void Update() {
            agent.SetDestination(player.position);
        }
    }
}