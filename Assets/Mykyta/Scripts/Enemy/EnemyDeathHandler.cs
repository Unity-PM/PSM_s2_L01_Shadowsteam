using UnityEngine;

public class EnemyDeathHandler : MonoBehaviour
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

        Destroy(gameObject, 0.5f);
    }
}
