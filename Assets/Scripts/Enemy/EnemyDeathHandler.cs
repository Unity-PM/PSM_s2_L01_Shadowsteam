using Platformer;
using UnityEngine;
using UnityEngine.AI;

public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private string deathAnimationStateId = "Die";
    [SerializeField] private float destroyDelaySeconds = 3f;
    [SerializeField] private float minimumDeathAnimationSeconds = 1.8f;
    [SerializeField] private bool freezePhysicsOnDeath = true;
    [SerializeField] private bool disableCollidersOnDeath = true;

    StatComponent stats;
    DynamicAnimator clipAnimator;
    Enemy enemy;
    EnemyAudioController audioController;
    NavMeshAgent agent;
    Rigidbody rb;
    Collider[] colliders;
    bool isDead;

    void Awake()
    {
        stats = GetComponent<StatComponent>();
        clipAnimator = GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();
        enemy = GetComponent<Enemy>();
        audioController = GetComponent<EnemyAudioController>() ?? GetComponentInChildren<EnemyAudioController>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
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
        if (isDead)
            return;

        isDead = true;
        audioController?.PlayDeath();

        if (enemy != null)
            enemy.enabled = false;

        if (agent != null && agent.enabled)
        {
            if (EnemyLocomotion.IsReadyForNavigation(agent))
            {
                agent.ResetPath();
                agent.isStopped = true;
            }

            agent.enabled = false;
        }

        FreezePhysics();
        DisableHitColliders();

        string dieStateId = enemy != null ? enemy.AnimDieId : deathAnimationStateId;
        float delay = Mathf.Max(destroyDelaySeconds, minimumDeathAnimationSeconds);
        if (clipAnimator != null && !string.IsNullOrEmpty(dieStateId))
        {
            clipAnimator.ForcePlay(dieStateId);
            if (clipAnimator.TryGetClipLength(dieStateId, out float clipLength))
                delay = Mathf.Max(delay, clipLength + 0.15f);
        }

        EventBus.Publish(new ExperienceGainedEvent(25));

        Destroy(gameObject, delay);
    }

    void FreezePhysics()
    {
        if (!freezePhysicsOnDeath || rb == null)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    void DisableHitColliders()
    {
        if (!disableCollidersOnDeath || colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col != null)
                col.enabled = false;
        }
    }
}
