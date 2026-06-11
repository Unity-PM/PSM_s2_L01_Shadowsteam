using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    private StatComponent stats;
    private void Awake()
    {
        stats = GetComponent<StatComponent>();
    }
    private void OnEnable()
    {
        EventBus.Subscribe<DeathEvent>(OnDeath);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DeathEvent>(OnDeath);
    }

    private void OnDeath(DeathEvent e)
    {
        if (e.target != stats)
            return;

        Debug.Log("PLAYER DIED");

        gameObject.SetActive(false);
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Application.Quit();
        }
        EventBus.Publish(new GameOverEvent(true));
    }
}
