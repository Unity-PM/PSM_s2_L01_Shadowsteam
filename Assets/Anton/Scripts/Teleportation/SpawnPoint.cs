using UnityEngine;

public class TeleporterSpawnPoint : MonoBehaviour
{
    [Header("Optional ID (used for cross-scene teleports)")]
    public string spawnID = "Default";

    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.green;
    public float gizmoSize = 0.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        Gizmos.DrawSphere(transform.position, gizmoSize);

        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}