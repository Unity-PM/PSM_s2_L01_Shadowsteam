using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SwordSlashAbility", menuName = "Scriptable Objects/Abilities/SwordSlash")]
public class SwordSlashAbility : MeleeAbilitySO
{
    public int arcRayCount = 5;
    public LayerMask hitMask;

    [Tooltip("Total time for all hitboxes to appear")]
    public float sweepDuration = 0.3f;
    [Range(-0.9f, 0.9f)]
    [Tooltip("0 = even, > 0 = accelerates, < 0 = decelerates")]
    public float sweepAcceleration = 0f;

    public override void Execute(StatComponent caster, Transform castPoint, MovementBrain brain)
    {
        SwordSlashVisualizer visualizer = Object.FindObjectOfType<SwordSlashVisualizer>();
        CoroutineRunner.Instance.Run(SweepRoutine(caster, castPoint, visualizer));

        if (lockMovement)
            brain.LockMovement(sweepDuration);
    }

    private IEnumerator SweepRoutine(StatComponent caster, Transform castPoint, SwordSlashVisualizer visualizer)
    {
        float halfArc = arcAngle * 0.5f;
        float stepAngle = arcRayCount > 1 ? arcAngle / (arcRayCount - 1) : 0f;
        float sphereRadius = 0.3f;

        float[] timestamps = BuildTimestamps(arcRayCount);

        int nextIndex = 0;
        float elapsed = 0f;

        while (nextIndex < arcRayCount)
        {
            elapsed += Time.deltaTime;

            while (nextIndex < arcRayCount && elapsed >= timestamps[nextIndex])
            {
                float angle = -halfArc + stepAngle * nextIndex;
                Vector3 dir = BuildDirection(castPoint.forward, angle, slashTiltAngle);

                bool didHit = Physics.SphereCast(castPoint.position, sphereRadius, dir, out RaycastHit hit, range, hitMask);

                visualizer?.RecordSlash(castPoint.position, dir, range, didHit);

                if (didHit)
                {
                    StatComponent target = hit.collider.GetComponent<StatComponent>();
                    if (target != null && target != caster)
                        EventBus.Publish(new StatChangeEvent(target, StatType.HP, -damage));
                }

                nextIndex++;
            }

            yield return null;
        }
    }

    private float[] BuildTimestamps(int count)
    {
        float[] timestamps = new float[count];
        if (count <= 1)
        {
            timestamps[0] = 0f;
            return timestamps;
        }

        float[] weights = new float[count];
        weights[0] = 1f;
        for (int i = 1; i < count; i++)
            weights[i] = weights[i - 1] * (1f - sweepAcceleration);

        float sum = 0f;
        for (int i = 0; i < count; i++) sum += weights[i];

        float accumulated = 0f;
        timestamps[0] = 0f;
        for (int i = 1; i < count; i++)
        {
            accumulated += weights[i - 1] / sum * sweepDuration;
            timestamps[i] = accumulated;
        }

        return timestamps;
    }

    private Vector3 BuildDirection(Vector3 forward, float horizontalAngle, float tiltAngle)
    {
        Quaternion horizontal = Quaternion.AngleAxis(horizontalAngle, Vector3.up);
        Quaternion tilt = Quaternion.AngleAxis(tiltAngle, forward);
        return tilt * horizontal * forward;
    }
}