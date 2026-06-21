using Platformer;
using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    [Header("Hit")]
    [SerializeField] float hitRadius = 0.35f;
    [SerializeField] LayerMask hitMask = Physics.DefaultRaycastLayers;
    [SerializeField] bool destroyOnWorldHit = true;

    [Header("VFX")]
    [SerializeField] GameObject projectileVfxPrefab;
    [SerializeField] GameObject impactVfxPrefab;

    [Header("Audio")]
    [SerializeField] AudioClip impactSound;
    [Range(0f, 1f)] [SerializeField] float impactVolume = 1f;

    StatComponent caster;
    Transform target;
    Vector3 moveDirection;
    float damage;
    float speed;
    float targetAimHeight = 1f;
    float homingTurnSpeed = 720f;
    bool initialized;
    bool isDestroying;
    bool impactVfxSpawned;

    public void Init(StatComponent caster, float damage, float speed)
    {
        Init(
            caster,
            damage,
            speed,
            null,
            transform.forward,
            targetAimHeight,
            3f,
            1.25f,
            homingTurnSpeed,
            null,
            null,
            null,
            impactVolume);
    }

    public void Init(
        StatComponent caster,
        float damage,
        float speed,
        Transform target,
        Vector3 direction,
        float targetAimHeight,
        float projectileLifetime,
        float noTargetLifetime,
        float homingTurnSpeed,
        GameObject projectileVfxOverride,
        GameObject impactVfxOverride,
        AudioClip impactSoundOverride,
        float impactVolume)
    {
        this.caster = caster;
        this.damage = damage;
        this.speed = Mathf.Max(0f, speed);
        this.target = target;
        this.targetAimHeight = Mathf.Max(0f, targetAimHeight);
        this.homingTurnSpeed = Mathf.Max(0f, homingTurnSpeed);
        this.impactVolume = Mathf.Clamp01(impactVolume);
        moveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;

        if (projectileVfxOverride != null)
            projectileVfxPrefab = projectileVfxOverride;
        if (impactVfxOverride != null)
            impactVfxPrefab = impactVfxOverride;
        if (impactSoundOverride != null)
            impactSound = impactSoundOverride;

        ConfigurePhysics();
        AttachProjectileVfx();

        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        initialized = true;

        float lifetime = target != null
            ? Mathf.Max(0.05f, projectileLifetime)
            : Mathf.Max(0.05f, noTargetLifetime);
        Destroy(gameObject, lifetime);
    }

    void Awake()
    {
        ConfigurePhysics();
    }

    void Update()
    {
        if (!initialized)
            return;

        UpdateDirection();

        Vector3 startPosition = transform.position;
        Vector3 displacement = moveDirection * speed * Time.deltaTime;
        if (TryHitAtPosition(startPosition, out Collider overlap)
            || TryHitAlongPath(startPosition, displacement, out overlap))
        {
            HandleHit(overlap);
            return;
        }

        transform.position = startPosition + displacement;
        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
    }

    void UpdateDirection()
    {
        Vector3 desiredDirection = moveDirection;
        if (HasValidTarget())
            desiredDirection = GetTargetPoint(target) - transform.position;

        if (desiredDirection.sqrMagnitude < 0.001f)
            return;

        float maxRadiansDelta = homingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
        moveDirection = Vector3.RotateTowards(
            moveDirection,
            desiredDirection.normalized,
            maxRadiansDelta,
            0f).normalized;
    }

    bool HasValidTarget()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        StatComponent targetStats = target.GetComponent<StatComponent>();
        return targetStats == null || !targetStats.IsDead;
    }

    bool TryHitAtPosition(Vector3 position, out Collider hit)
    {
        hit = null;
        Collider[] overlaps = Physics.OverlapSphere(
            position,
            hitRadius,
            hitMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider candidate = overlaps[i];
            if (IsIgnoredCollider(candidate))
                continue;

            hit = candidate;
            return true;
        }

        return false;
    }

    bool TryHitAlongPath(Vector3 startPosition, Vector3 displacement, out Collider hit)
    {
        hit = null;
        float distance = displacement.magnitude;
        if (distance <= 0f)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            startPosition,
            hitRadius,
            displacement.normalized,
            distance,
            hitMask,
            QueryTriggerInteraction.Collide);

        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider candidate = hits[i].collider;
            if (IsIgnoredCollider(candidate) || hits[i].distance >= bestDistance)
                continue;

            hit = candidate;
            bestDistance = hits[i].distance;
        }

        return hit != null;
    }

    bool IsIgnoredCollider(Collider candidate)
    {
        if (candidate == null || candidate.transform.IsChildOf(transform))
            return true;

        if (caster != null)
        {
            StatComponent ownerStats = candidate.GetComponentInParent<StatComponent>();
            if (ownerStats == caster)
                return true;
        }

        return false;
    }

    void HandleHit(Collider other)
    {
        if (isDestroying || IsIgnoredCollider(other))
            return;

        if (TryDamageEnemy(other))
        {
            DestroyProjectile();
            return;
        }

        if (destroyOnWorldHit && !other.isTrigger)
            DestroyProjectile();
    }

    bool TryDamageEnemy(Collider other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null)
            return false;

        StatComponent targetStats = enemy.GetComponent<StatComponent>();
        if (targetStats == null || targetStats == caster || targetStats.IsDead)
            return false;

        float finalDamage = Mathf.Max(0f, damage - Mathf.Max(0f, targetStats.GetEffectiveStat(StatType.MDEF)));
        EventBus.Publish(new StatChangeEvent(
            targetStats,
            StatType.HP,
            -finalDamage
        ));

        return true;
    }

    void DestroyProjectile()
    {
        if (isDestroying)
            return;

        isDestroying = true;
        SpawnImpactVfx();
        PlayImpactSound();
        Destroy(gameObject);
    }

    void AttachProjectileVfx()
    {
        if (projectileVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(projectileVfxPrefab, transform.position, transform.rotation, transform);
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.identity;
    }

    void SpawnImpactVfx()
    {
        if (impactVfxSpawned || impactVfxPrefab == null)
            return;

        impactVfxSpawned = true;
        GameObject vfx = Instantiate(impactVfxPrefab, transform.position, transform.rotation);
        float destroyDelay = PrepareParticleVfxAsOneShot(vfx);
        Destroy(vfx, destroyDelay);
    }

    float PrepareParticleVfxAsOneShot(GameObject vfx)
    {
        if (vfx == null)
            return 2f;

        ParticleSystem[] systems = vfx.GetComponentsInChildren<ParticleSystem>(true);
        if (systems.Length == 0)
            return 2f;

        float maxLifetime = 0f;
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem system = systems[i];
            if (system == null)
                continue;

            ParticleSystem.MainModule main = system.main;
            main.loop = false;

            float lifetime = main.startDelay.constantMax
                + main.duration
                + main.startLifetime.constantMax;
            maxLifetime = Mathf.Max(maxLifetime, lifetime);

            system.Clear(true);
            system.Play(true);
        }

        return Mathf.Max(0.5f, maxLifetime + 0.25f);
    }

    void PlayImpactSound()
    {
        if (impactSound == null)
            return;

        AudioSource.PlayClipAtPoint(impactSound, transform.position, impactVolume);
    }

    Vector3 GetTargetPoint(Transform targetTransform)
    {
        if (targetTransform == null)
            return transform.position + moveDirection;

        Collider col = targetTransform.GetComponentInChildren<Collider>();
        if (col != null && col.enabled)
            return col.bounds.center;

        return targetTransform.position + Vector3.up * targetAimHeight;
    }

    void ConfigurePhysics()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].isTrigger = true;

        if (!TryGetComponent(out Rigidbody rb))
            return;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }
}
