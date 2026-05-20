using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public static class EnemyLocomotion {
        public static void ConfigureAgent(NavMeshAgent agent, float walkSpeed) {
            if (agent == null)
                return;

            agent.updateRotation = true;
            agent.autoBraking = true;
            agent.autoRepath = true;
            agent.stoppingDistance = 0.35f;
            agent.acceleration = 16f;
            agent.angularSpeed = 420f;
            agent.speed = walkSpeed;
        }

        public static bool TrySampleNavigablePoint(Vector3 origin, float radius, out Vector3 result) {
            result = origin;

            for (int i = 0; i < 10; i++) {
                Vector3 randomPoint = origin + Random.insideUnitSphere * radius;
                randomPoint.y = origin.y;

                if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
                    continue;

                result = hit.position;
                return true;
            }

            return false;
        }

        public static bool TrySetDestination(NavMeshAgent agent, Vector3 worldTarget) {
            if (agent == null)
                return false;

            if (!NavMesh.SamplePosition(worldTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return false;

            agent.isStopped = false;
            agent.SetDestination(hit.position);
            return agent.pathStatus != NavMeshPathStatus.PathInvalid;
        }

        public static bool HasReachedDestination(NavMeshAgent agent) {
            if (agent == null || !agent.isOnNavMesh)
                return true;

            if (agent.pathPending)
                return false;

            if (agent.pathStatus is NavMeshPathStatus.PathInvalid or NavMeshPathStatus.PathPartial)
                return true;

            if (float.IsInfinity(agent.remainingDistance))
                return true;

            return agent.remainingDistance <= agent.stoppingDistance + 0.05f;
        }

        public static bool UpdateStuckTimer(NavMeshAgent agent, ref float stuckTimer, ref Vector3 lastPosition, float stuckSeconds) {
            if (agent == null || agent.isStopped || !agent.hasPath || agent.pathPending) {
                stuckTimer = 0f;
                lastPosition = agent.transform.position;
                return false;
            }

            float moved = Vector3.Distance(agent.transform.position, lastPosition);
            lastPosition = agent.transform.position;

            bool barelyMoved = moved < 0.03f;
            bool stillFar = agent.remainingDistance > agent.stoppingDistance + 0.25f;

            if (barelyMoved && stillFar)
                stuckTimer += Time.deltaTime;
            else
                stuckTimer = 0f;

            return stuckTimer >= stuckSeconds;
        }
    }
}
