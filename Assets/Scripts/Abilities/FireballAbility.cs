using UnityEngine;
using Platformer;

[CreateAssetMenu(fileName = "FireballAbility", menuName = "Scriptable Objects/Abilities/Fireball")]
public class FireballAbility : RangedAbilitySO
{
    [Header("Targeting")]
    [SerializeField] float targetSearchRadius = 24f;
    [SerializeField] float targetAimHeight = 1f;

    [Header("Lifetime")]
    [SerializeField] float projectileLifetime = 4f;
    [SerializeField] float noTargetLifetime = 1.25f;
    [SerializeField] float homingTurnSpeed = 720f;

    [Header("VFX")]
    [SerializeField] GameObject projectileVfxPrefab;
    [SerializeField] GameObject impactVfxPrefab;

    [Header("Audio")]
    [SerializeField] AudioClip launchSound;
    [Range(0f, 1f)] [SerializeField] float launchVolume = 1f;
    [SerializeField] AudioClip impactSound;
    [Range(0f, 1f)] [SerializeField] float impactVolume = 1f;

    public override void Execute(StatComponent caster, Transform castPoint)
    {
        if (projectilePrefab == null || castPoint == null)
            return;

        Transform target = FindNearestEnemy(castPoint.position, caster);
        Vector3 direction = target != null
            ? GetTargetPoint(target) - castPoint.position
            : castPoint.forward;

        if (direction.sqrMagnitude < 0.001f)
            direction = castPoint.forward;

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        PlayOneShot(launchSound, castPoint.position, launchVolume);

        GameObject fb = Instantiate(projectilePrefab, castPoint.position, rotation);
        if (fb.TryGetComponent(out FireballProjectile proj))
        {
            proj.Init(
                caster,
                damage,
                speed,
                target,
                direction.normalized,
                targetAimHeight,
                projectileLifetime,
                noTargetLifetime,
                homingTurnSpeed,
                projectileVfxPrefab,
                impactVfxPrefab,
                impactSound,
                impactVolume);
        }
    }

    void PlayOneShot(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null)
            return;

        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
    }

    Transform FindNearestEnemy(Vector3 origin, StatComponent caster)
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        Transform nearest = null;
        float radiusSqr = targetSearchRadius <= 0f
            ? float.PositiveInfinity
            : targetSearchRadius * targetSearchRadius;
        float bestSqrDistance = radiusSqr;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled)
                continue;

            StatComponent enemyStats = enemy.GetComponent<StatComponent>();
            if (enemyStats == caster || (enemyStats != null && enemyStats.IsDead))
                continue;

            float sqrDistance = (GetTargetPoint(enemy.transform) - origin).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            nearest = enemy.transform;
        }

        return nearest;
    }

    Vector3 GetTargetPoint(Transform target)
    {
        if (target == null)
            return Vector3.zero;

        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null && col.enabled)
            return col.bounds.center;

        return target.position + Vector3.up * targetAimHeight;
    }
}
