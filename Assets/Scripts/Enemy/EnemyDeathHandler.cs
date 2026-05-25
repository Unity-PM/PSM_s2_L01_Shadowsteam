using Platformer;
using UnityEngine;
using UnityEngine.AI;

public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private string deathAnimationStateId = "Die";
    [SerializeField] private float destroyDelaySeconds = 0.5f;

    StatComponent stats;
    DynamicAnimator clipAnimator;
    Enemy enemy;
    NavMeshAgent agent;

    void Awake()
    {
        stats = GetComponent<StatComponent>();
        clipAnimator = GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();
        enemy = GetComponent<Enemy>();
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        EventBus.Subscribe<DeathEvent>(OnDeath);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<DeathEvent>(OnDeath);
    }

    void OnDeath(DeathEvent e)
    {
        if (stats == null || e.target != stats)
            return;

        if (enemy != null)
            enemy.enabled = false;

        if (agent != null)
            agent.enabled = false;

        string dieStateId = enemy != null ? enemy.AnimDieId : deathAnimationStateId;
        float delay = destroyDelaySeconds;
        if (clipAnimator != null && !string.IsNullOrEmpty(dieStateId))
        {
            clipAnimator.ForcePlay(dieStateId);
            if (clipAnimator.TryGetClipLength(dieStateId, out float clipLength))
                delay = Mathf.Max(delay, clipLength);
        }

        EventBus.Publish(new ExperienceGainedEvent(25));

        Destroy(gameObject, delay);
    }
}
