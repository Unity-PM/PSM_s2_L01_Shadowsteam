using UnityEngine;

namespace Player.Movement.Modules2
{
    internal static class LocomotionKit
    {
        internal const float InputDeadzoneSqr = 0.04f;

        internal static bool WantsToMove(MovementBrain brain) =>
            brain.Input != null && brain.Input.MoveVector.sqrMagnitude > InputDeadzoneSqr;

        internal static bool WantsSprint(MovementBrain brain) =>
            brain.Input != null && brain.Input.IsRunPressed;

        internal static StatComponent Stats(MovementBrain brain) =>
            brain.GetComponent<StatComponent>();

        internal static bool CanSpendStamina(MovementBrain brain)
        {
            StatComponent stats = Stats(brain);
            return stats == null || (!stats.IsStaminaExhausted && stats.getStamina() > 0f);
        }

        internal static bool HasStaminaFor(MovementBrain brain, float cost)
        {
            if (cost <= 0f)
                return true;

            StatComponent stats = Stats(brain);
            return stats == null || stats.getStamina() >= cost;
        }

        internal static Vector3 CalculateTargetVelocity(MovementBrain brain, float speed)
        {
            if (!WantsToMove(brain) || brain.settings == null)
                return Vector3.zero;

            Vector3 direction = WorldDirection(brain, brain.Input.MoveVector);
            return direction * GetEffectiveMoveSpeed(brain, speed);
        }

        internal static float GetEffectiveMoveSpeed(MovementBrain brain, float baseSpeed)
        {
            StatComponent stats = Stats(brain);
            return stats != null
                ? baseSpeed * stats.GetPercentStatMultiplier(StatType.MS)
                : baseSpeed;
        }

        internal static void SpendStaminaFlat(StatComponent stats, float amount)
        {
            if (stats == null || amount <= 0f)
                return;

            EventBus.Publish(new StatChangeEvent(stats, StatType.Stamina, -amount));
        }

        internal static void SpendStaminaPerSecond(StatComponent stats, float perSecond)
        {
            if (stats == null || perSecond <= 0f || stats.getStamina() <= 0f)
                return;

            EventBus.Publish(new StatChangeEvent(
                stats,
                StatType.Stamina,
                -perSecond * Time.deltaTime));
        }

        internal static Vector3 WorldDirection(MovementBrain brain, Vector2 input)
        {
            if (brain.gameObject.CompareTag("Player") && brain.cameraTransform != null)
            {
                Vector3 forward = brain.cameraTransform.forward;
                Vector3 right = brain.cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                return (forward.normalized * input.y + right.normalized * input.x).normalized;
            }

            return new Vector3(input.x, 0f, input.y).normalized;
        }
    }
}
