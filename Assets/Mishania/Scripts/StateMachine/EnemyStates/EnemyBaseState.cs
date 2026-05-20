using UnityEngine;

namespace Platformer {
    public abstract class EnemyBaseState : IState {
        protected readonly Enemy enemy;
        protected readonly DynamicAnimator clipAnimator;

        protected string IdleId => enemy.AnimIdleId;
        protected string WalkId => enemy.AnimWalkId;
        protected string RunId => enemy.AnimRunId;
        protected string AttackId => enemy.AnimAttackId;
        protected string DieId => enemy.AnimDieId;

        protected EnemyBaseState(Enemy enemy, DynamicAnimator clipAnimator) {
            this.enemy = enemy;
            this.clipAnimator = clipAnimator;
        }

        protected void SafePlay(string stateId) {
            if (clipAnimator == null || !clipAnimator.isActiveAndEnabled) return;
            clipAnimator.Play(stateId);
        }

        protected void SafeForcePlay(string stateId) {
            if (clipAnimator == null || !clipAnimator.isActiveAndEnabled) return;
            clipAnimator.ForcePlay(stateId);
        }
        
        public virtual void OnEnter() {
            // noop
        }

        public virtual void Update() {
            // noop
        }

        public virtual void FixedUpdate() {
            // noop
        }

        public virtual void OnExit() {
            // noop
        }
    }
}