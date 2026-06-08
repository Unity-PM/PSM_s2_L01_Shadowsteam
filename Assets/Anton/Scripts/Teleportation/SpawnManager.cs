using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public static string PendingSpawnPoint;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(PendingSpawnPoint))
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Player with tag 'Player' not found.");
            return;
        }

        TeleporterSpawnPoint[] spawnPoints = FindObjectsByType<TeleporterSpawnPoint>(
            FindObjectsSortMode.None);

        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint.name == PendingSpawnPoint)
            {
                player.transform.SetPositionAndRotation(
                    spawnPoint.transform.position,
                    spawnPoint.transform.rotation);

                Debug.Log($"Spawned at {spawnPoint.name}");

                PendingSpawnPoint = "";
                return;
            }
        }

        Debug.LogError($"Spawn point '{PendingSpawnPoint}' not found.");
    }
}