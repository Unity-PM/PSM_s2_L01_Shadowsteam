using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Teleporter : MonoBehaviour
{
    [Header("Same Scene")]
    public Transform targetPoint;

    [Header("Cross Scene")]
    public bool loadScene;
    public string sceneName;
    public string targetSpawnPointName;

    [Header("Effects")]
    public GameObject enterEffect;
    public GameObject exitEffect;

    public float delayBeforeTeleport = 0.5f;
    public float effectLifetime = 3f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        StartCoroutine(Teleport(other.transform));
    }

    private IEnumerator Teleport(Transform player)
    {
        if (enterEffect)
        {
            GameObject fx = Instantiate(
                enterEffect,
                player.position,
                Quaternion.identity);

            Destroy(fx, effectLifetime);
        }

        yield return new WaitForSeconds(delayBeforeTeleport);

        if (loadScene)
        {
            SpawnManager.PendingSpawnPoint = targetSpawnPointName;

            DontDestroyOnLoad(player.gameObject);

            SceneManager.LoadScene(sceneName);
        }
        else
        {
            player.SetPositionAndRotation(
                targetPoint.position,
                targetPoint.rotation);

            if (exitEffect)
            {
                GameObject fx = Instantiate(
                    exitEffect,
                    player.position,
                    Quaternion.identity);

                Destroy(fx, effectLifetime);
            }
        }
    }
}