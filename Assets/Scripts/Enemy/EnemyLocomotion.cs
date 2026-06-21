using UnityEngine;
using UnityEngine.AI;

namespace Platformer {
    public static class EnemyLocomotion {
        public static void ConfigureAgent(NavMeshAgent agent, float walkSpeed) {
            if (agent == null)
                return;

            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.updateUpAxis = true;
            agent.autoBraking = true;
            agent.autoRepath = true;
            agent.stoppingDistance = 0.35f;
            agent.acceleration = 24f;
            agent.angularSpeed = 720f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
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

            if (!RecoverToNavMesh(agent, agent.transform.position, 3f))
                return false;

            if (!NavMesh.SamplePosition(worldTarget, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                return false;

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(agent.transform.position, hit.position, NavMesh.AllAreas, path)
                || path.status == NavMeshPathStatus.PathInvalid)
                return false;

            agent.isStopped = false;
            agent.SetPath(path);
            return true;
        }

        public static bool IsReadyForNavigation(NavMeshAgent agent) {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }

        public static bool HasReachedDestination(NavMeshAgent agent) {
            if (!IsReadyForNavigation(agent))
                return true;

            if (agent.pathPending)
                return false;

            if (agent.pathStatus is NavMeshPathStatus.PathInvalid or NavMeshPathStatus.PathPartial)
                return true;

            if (float.IsInfinity(agent.remainingDistance))
                return true;

            return agent.remainingDistance <= agent.stoppingDistance + 0.05f;
        }

        public static void StopAgent(NavMeshAgent agent, bool resetPath) {
            if (!IsReadyForNavigation(agent))
                return;

            agent.isStopped = true;
            if (resetPath)
                agent.ResetPath();

            agent.velocity = Vector3.zero;
            agent.nextPosition = agent.transform.position;
        }

        public static bool HasMoveIntent(NavMeshAgent agent, float extraDistance = 0.1f) {
            if (!IsReadyForNavigation(agent) || agent.isStopped)
                return false;

            if (agent.pathPending)
                return true;

            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.01f)
                return true;

            Vector3 desiredVelocity = agent.desiredVelocity;
            desiredVelocity.y = 0f;
            if (desiredVelocity.sqrMagnitude > 0.01f)
                return true;

            if (!agent.hasPath || float.IsInfinity(agent.remainingDistance))
                return false;

            return agent.remainingDistance > agent.stoppingDistance + extraDistance;
        }

        public static void ClampVelocity(NavMeshAgent agent, float maxPlanarSpeed) {
            if (!IsReadyForNavigation(agent) || maxPlanarSpeed <= 0f)
                return;

            Vector3 velocity = agent.velocity;
            Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
            if (planar.sqrMagnitude <= maxPlanarSpeed * maxPlanarSpeed)
                return;

            Vector3 clamped = planar.normalized * maxPlanarSpeed;
            agent.velocity = new Vector3(clamped.x, velocity.y, clamped.z);
        }

        public static bool RecoverToNavMesh(NavMeshAgent agent, Vector3 fallbackPosition, float sampleRadius) {
            if (agent == null || !agent.enabled)
                return false;

            if (agent.isOnNavMesh)
                return true;

            if (NavMesh.SamplePosition(agent.transform.position, out NavMeshHit agentHit, sampleRadius, NavMesh.AllAreas)) {
                agent.Warp(agentHit.position);
                return true;
            }

            if (NavMesh.SamplePosition(fallbackPosition, out NavMeshHit fallbackHit, sampleRadius, NavMesh.AllAreas)) {
                agent.Warp(fallbackHit.position);
                return true;
            }

            return false;
        }

        public static bool UpdateStuckTimer(NavMeshAgent agent, ref float stuckTimer, ref Vector3 lastPosition, float stuckSeconds) {
            if (!IsReadyForNavigation(agent)) {
                stuckTimer = 0f;
                if (agent != null)
                    lastPosition = agent.transform.position;
                return false;
            }

            if (agent.isStopped || !agent.hasPath || agent.pathPending) {
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
