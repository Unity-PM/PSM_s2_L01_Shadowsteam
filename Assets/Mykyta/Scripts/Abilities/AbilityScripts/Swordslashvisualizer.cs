using System.Collections.Generic;
using UnityEngine;

public class SwordSlashVisualizer : MonoBehaviour
{
    [Header("Ability Reference")]
    public SwordSlashAbility ability;
    public Transform castPoint;

    [Header("Runtime Visual")]
    public float lineDuration = 0.5f;
    public Color hitColor = Color.red;
    public Color missColor = Color.yellow;

    [Header("Gizmos")]
    public bool showGizmosAlways = true;
    public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);

    private struct SlashRecord
    {
        public Vector3 origin;
        public Vector3 direction;
        public float range;
        public bool hit;
        public float expireTime;
    }

    private List<SlashRecord> records = new();

    public void RecordSlash(Vector3 origin, Vector3 direction, float range, bool hit)
    {
        records.Add(new SlashRecord
        {
            origin = origin,
            direction = direction,
            range = range,
            hit = hit,
            expireTime = Time.time + lineDuration
        });
    }

    private void Update()
    {
        records.RemoveAll(r => Time.time > r.expireTime);

        foreach (var r in records)
            Debug.DrawRay(r.origin, r.direction * r.range, r.hit ? hitColor : missColor);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmosAlways) return;
        DrawArcGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (showGizmosAlways) return;
        DrawArcGizmo();
    }

    private void DrawArcGizmo()
    {
        if (ability == null || castPoint == null) return;

        int count = ability.arcRayCount;
        float halfArc = ability.arcAngle * 0.5f;
        float stepAngle = count > 1 ? ability.arcAngle / (count - 1) : 0f;
        float range = ability.range;
        float tilt = ability.slashTiltAngle;
        Vector3 forward = castPoint.forward;

        Gizmos.color = gizmoColor;

        Vector3 prevPoint = Vector3.zero;
        bool hasPrev = false;

        for (int i = 0; i < count; i++)
        {
            float angle = -halfArc + stepAngle * i;
            Quaternion horizontal = Quaternion.AngleAxis(angle, Vector3.up);
            Quaternion tiltRot = Quaternion.AngleAxis(tilt, forward);
            Vector3 dir = tiltRot * horizontal * forward;
            Vector3 endPoint = castPoint.position + dir * range;

            Gizmos.DrawLine(castPoint.position, endPoint);
            Gizmos.DrawWireSphere(endPoint, 0.3f);

            if (hasPrev)
                Gizmos.DrawLine(prevPoint, endPoint);

            prevPoint = endPoint;
            hasPrev = true;
        }
    }
}