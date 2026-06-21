using UnityEngine;
using Utilities;

namespace Platformer {
    public class ConeDetectionStrategy : IDetectionStrategy {
        readonly float detectionAngle;
        readonly float detectionRadius;
        readonly float innerDetectionRadius;
        
        public ConeDetectionStrategy(float detectionAngle, float detectionRadius, float innerDetectionRadius) {
            this.detectionAngle = detectionAngle;
            this.detectionRadius = detectionRadius;
            this.innerDetectionRadius = innerDetectionRadius;
        }
        
        public bool Execute(Transform player, Transform detector, CountdownTimer timer) {
            if (player == null || detector == null || timer == null)
                return false;

            if (timer.IsRunning) return false;
            
            var directionToPlayer = player.position - detector.position;
            directionToPlayer.y = 0f;

            var detectorForward = detector.forward;
            detectorForward.y = 0f;
            if (detectorForward.sqrMagnitude < 0.001f)
                detectorForward = Vector3.forward;

            var angleToPlayer = Vector3.Angle(directionToPlayer, detectorForward);
            float flatDistance = directionToPlayer.magnitude;
            
            // If the player is not within the detection angle + outer radius (aka the cone in front of the enemy),
            // or is within the inner radius, return false
            if ((!(angleToPlayer < detectionAngle / 2f) || !(flatDistance < detectionRadius))
                && !(flatDistance < innerDetectionRadius))
                return false;
            
            timer.Start();
            return true;
        }
    }
}
