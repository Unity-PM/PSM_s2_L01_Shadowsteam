using UnityEngine;

public class TeleportSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "Default";

    public string SpawnId => spawnId;

    public bool Matches(string requestedId)
    {
        return string.IsNullOrEmpty(requestedId) || spawnId == requestedId;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.9f);
    }
#endif
}
