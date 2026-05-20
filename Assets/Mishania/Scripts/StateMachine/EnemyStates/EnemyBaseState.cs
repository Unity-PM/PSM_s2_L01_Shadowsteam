using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public abstract class EnemyBaseState : IState {
        protected readonly Enemy enemy;
        protected readonly DynamicAnimator clipAnimator;

        protected string IdleId => enemy.AnimIdleId;
        protected string WalkId => enemy.AnimWalkId;
        protected string RunId => enemy.AnimRunId;
        protected string AttackId => enemy.AnimAttackId;
        protected string DieId => enemy.AnimDieId;

        string currentLocomotionId;

        protected EnemyBaseState(Enemy enemy, DynamicAnimator clipAnimator) {
            this.enemy = enemy;
            this.clipAnimator = clipAnimator;
        }

        protected void SafePlay(string stateId) {
            if (clipAnimator == null || !clipAnimator.isActiveAndEnabled)
                return;

            clipAnimator.Play(stateId);
        }

        protected void SafeForcePlay(string stateId) {
            if (clipAnimator == null || !clipAnimator.isActiveAndEnabled)
                return;

            clipAnimator.ForcePlay(stateId);
        }

        protected void ResetLocomotionTracking() => currentLocomotionId = null;

        protected void SyncLocomotion(NavMeshAgent agent, bool forceRun = false) {
            if (agent == null || clipAnimator == null || clipAnimator.IsMovementLocked)
                return;

            float speed = agent.velocity.magnitude;
            string nextId;

            if (speed < 0.05f)
                nextId = IdleId;
            else if (forceRun || speed >= enemy.ChaseSpeed * 0.75f)
                nextId = RunId;
            else
                nextId = WalkId;

            if (currentLocomotionId == nextId)
                return;

            currentLocomotionId = nextId;
            SafePlay(nextId);
        }

        public virtual void OnEnter() { }

        public virtual void Update() { }

        public virtual void FixedUpdate() { }

        public virtual void OnExit() { }
    }
}
