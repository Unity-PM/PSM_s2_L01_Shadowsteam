using UnityEngine;

namespace Platformer {
    public abstract class EnemyBaseState : IState {
        protected readonly Enemy enemy;
        protected readonly UniversalClipAnimator clipAnimator;

        /// <summary>Ids must match entries on the enemy's UniversalClipAnimator.</summary>
        protected const string IdleId = "IdleNormal";
        protected const string WalkId = "WalkFWD";
        protected const string RunId = "RunFWD";
        protected const string AttackId = "Attack01";
        protected const string DieId = "Die";

        protected EnemyBaseState(Enemy enemy, UniversalClipAnimator clipAnimator) {
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