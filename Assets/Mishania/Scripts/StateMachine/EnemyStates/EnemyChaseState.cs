using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public class EnemyChaseState : EnemyBaseState {
        readonly NavMeshAgent agent;
        readonly Transform player;
        
        public EnemyChaseState(Enemy enemy, DynamicAnimator clipAnimator, NavMeshAgent agent, Transform player) : base(enemy, clipAnimator) {
            this.agent = agent;
            this.player = player;
        }
        
        public override void OnEnter() {
            agent.isStopped = false;
            // ForcePlay: прерывает зацикленную атаку, когда вышли из дистанции удара
            SafeForcePlay(RunId);
        }
        
        public override void Update() {
            agent.SetDestination(player.position);
        }
    }
}