using System;
using System.Collections;
using System.Collections.Generic;
using Platformer;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[AddComponentMenu("Audio/Background Audio Manager")]
public class BackgroundAudioManager : MonoBehaviour
{
    public enum BackgroundAudioState
    {
        Idle,
        EnemyNearby,
        Combat,
        BossFight,
        MusicZone
    }

    public enum ZoneShape
    {
        Box,
        Sphere
    }

    [Serializable]
    public class MusicZone
    {
        [Tooltip("Optional name shown only to keep the list readable in the inspector.")]
        public string label;

        [Tooltip("Music that plays while the player is inside this territory.")]
        public AudioClip clip;

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Shape of the territory you edit with the handles in the Scene view.")]
        public ZoneShape shape = ZoneShape.Box;

        [Tooltip("World-space center of the territory. Drag it in the Scene view.")]
        public Vector3 center;

        [Tooltip("World-space size of the box territory. Drag the box faces in the Scene view.")]
        public Vector3 size = new Vector3(10f, 6f, 10f);

        [Min(0f)] [Tooltip("Radius of the sphere territory.")]
        public float radius = 6f;

        [Tooltip("When enabled this zone's music also overrides Combat and Boss Fight music. " +
                 "When disabled combat/boss music takes priority.")]
        public bool overrideCombat;

        [Tooltip("Color used to draw this territory in the Scene view.")]
        public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 1f);

        public bool Contains(Vector3 point)
        {
            if (shape == ZoneShape.Sphere)
                return (point - center).sqrMagnitude <= radius * radius;

            Vector3 half = size * 0.5f;
            Vector3 delta = point - center;
            return Mathf.Abs(delta.x) <= half.x
                && Mathf.Abs(delta.y) <= half.y
                && Mathf.Abs(delta.z) <= half.z;
        }
    }

    [Header("References")]
    [SerializeField] private Transform listenerTarget;
    [SerializeField] private string listenerTag = "Player";
    [SerializeField] private AudioSource primarySource;
    [SerializeField] private AudioSource secondarySource;
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    [Header("Clips")]
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip enemyNearbyClip;
    [SerializeField] private AudioClip combatClip;
    [SerializeField] private AudioClip bossFightClip;

    [Header("Volumes")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float idleVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float enemyNearbyVolume = 0.75f;
    [Range(0f, 1f)] [SerializeField] private float combatVolume = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float bossFightVolume = 1f;
    [SerializeField, Min(0f)] private float fadeSeconds = 1.25f;

    [Header("Detection")]
    [SerializeField] private bool automaticState = true;
    [SerializeField, Min(0.05f)] private float scanInterval = 0.25f;
    [SerializeField] private bool useFlatDistance = true;
    [SerializeField] private LayerMask enemyLayers = ~0;
    [SerializeField, Min(0f)] private float enemyNearbyRadius = 18f;
    [SerializeField, Min(0f)] private float attackDetectionWindow = 0.75f;
    [SerializeField, Min(0f)] private float combatHoldSeconds = 4f;
    [SerializeField, Min(0f)] private float bossFightRadius = 28f;
    [SerializeField] private bool ignoreDeadEnemies = true;

    [Header("Music Zones")]
    [Tooltip("Territories you draw in the Scene view. The chosen track plays while the player is inside one.")]
    [SerializeField] private List<MusicZone> musicZones = new List<MusicZone>();

    [Header("Lifetime")]
    [SerializeField] private bool dontDestroyOnLoad;
    [SerializeField] private bool startIdleOnAwake = true;

    private static BackgroundAudioManager instance;

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private BackgroundAudioState currentState = BackgroundAudioState.Idle;
    private AudioClip currentClip;
    private Coroutine fadeRoutine;
    private float nextScanTime;
    private float lastCombatEngagementTime = float.NegativeInfinity;
    private MusicZone activeZone;

    public BackgroundAudioState CurrentState => currentState;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // DontDestroyOnLoad only works on root objects; detach if nested.
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }

        ResolveSources();
        ConfigureSource(primarySource);
        ConfigureSource(secondarySource);

        activeSource = primarySource;
        inactiveSource = secondarySource;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<DeathEvent>(OnDeath);
    }

    private void Start()
    {
        ResolveListenerTarget();

        BackgroundAudioState initialState = automaticState
            ? EvaluateAutomaticState()
            : currentState;

        if (!startIdleOnAwake && initialState == BackgroundAudioState.Idle)
            return;

        SwitchToState(initialState, true);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DeathEvent>(OnDeath);
    }

    private void Update()
    {
        if (!automaticState || Time.unscaledTime < nextScanTime)
            return;

        nextScanTime = Time.unscaledTime + scanInterval;
        SwitchToState(EvaluateAutomaticState(), false);
    }

    public void ForceState(BackgroundAudioState state)
    {
        automaticState = false;
        SwitchToState(state, false);
    }

    public void EnableAutomaticState()
    {
        automaticState = true;
        nextScanTime = 0f;
        SwitchToState(EvaluateAutomaticState(), false);
    }

    private void OnDeath(DeathEvent deathEvent)
    {
        if (!automaticState)
            return;

        nextScanTime = 0f;
    }

    private BackgroundAudioState EvaluateAutomaticState()
    {
        ResolveListenerTarget();
        if (listenerTarget == null)
        {
            activeZone = null;
            return BackgroundAudioState.Idle;
        }

        BackgroundAudioState combatState = EvaluateCombatState();

        MusicZone zone = GetActiveMusicZone(listenerTarget.position);
        if (zone != null)
        {
            bool combatActive = combatState == BackgroundAudioState.Combat
                || combatState == BackgroundAudioState.BossFight;

            if (!combatActive || zone.overrideCombat)
            {
                activeZone = zone;
                return BackgroundAudioState.MusicZone;
            }
        }

        activeZone = null;
        return combatState;
    }

    private BackgroundAudioState EvaluateCombatState()
    {
        if (HasBossInRange())
            return BackgroundAudioState.BossFight;

        bool hasNearbyEnemy = false;

        Enemy[] enemies = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        float nearbyRadiusSqr = enemyNearbyRadius * enemyNearbyRadius;

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            if (!IsValidEnemy(enemy))
                continue;

            if (!IsBossEnemy(enemy)
                && (enemy.HasActiveOrRecentChase(attackDetectionWindow)
                    || enemy.HasActiveOrRecentAttack(attackDetectionWindow)))
            {
                lastCombatEngagementTime = Time.unscaledTime;
                break;
            }

            float distanceSqr = GetDistanceSqr(listenerTarget.position, enemy.transform.position);
            if (distanceSqr <= nearbyRadiusSqr)
                hasNearbyEnemy = true;
        }

        if (Time.unscaledTime - lastCombatEngagementTime <= combatHoldSeconds)
            return BackgroundAudioState.Combat;

        return hasNearbyEnemy
            ? BackgroundAudioState.EnemyNearby
            : BackgroundAudioState.Idle;
    }

    private MusicZone GetActiveMusicZone(Vector3 listenerPosition)
    {
        if (musicZones == null)
            return null;

        for (int i = 0; i < musicZones.Count; i++)
        {
            MusicZone zone = musicZones[i];
            if (zone == null || zone.clip == null)
                continue;

            if (zone.Contains(listenerPosition))
                return zone;
        }

        return null;
    }

    [ContextMenu("Log Music Zone Diagnostics")]
    private void LogMusicZoneDiagnostics()
    {
        ResolveListenerTarget();
        if (listenerTarget == null)
        {
            Debug.LogWarning(
                $"[BackgroundAudioManager] No listener target — tag '{listenerTag}' was not found in the scene.",
                this);
            return;
        }

        if (musicZones == null || musicZones.Count == 0)
        {
            Debug.LogWarning("[BackgroundAudioManager] Music Zones list is empty — nothing to detect.", this);
            return;
        }

        Vector3 position = listenerTarget.position;
        Debug.Log($"[BackgroundAudioManager] Listener at {position}. Checking {musicZones.Count} zone(s):", this);

        for (int i = 0; i < musicZones.Count; i++)
        {
            MusicZone zone = musicZones[i];
            if (zone == null)
            {
                Debug.Log($"  [{i}] <empty entry>", this);
                continue;
            }

            string name = string.IsNullOrEmpty(zone.label) ? $"Zone {i}" : zone.label;
            string clip = zone.clip != null ? zone.clip.name : "NONE";

            Debug.Log(
                $"  [{i}] '{name}': shape={zone.shape}, center={zone.center}, clip={clip}, insideNow={zone.Contains(position)}",
                this);
        }
    }

#if UNITY_EDITOR
    // Exposes the live zone list so the custom editor can draw editable handles for each territory.
    public List<MusicZone> EditorMusicZones => musicZones;
#endif

    private bool HasBossInRange()
    {
        FinalBossVictoryTrigger[] bosses = FindObjectsByType<FinalBossVictoryTrigger>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        float bossRadiusSqr = bossFightRadius * bossFightRadius;

        for (int i = 0; i < bosses.Length; i++)
        {
            FinalBossVictoryTrigger boss = bosses[i];
            if (boss == null || !IsLayerAllowed(boss.gameObject))
                continue;

            StatComponent bossStats = boss.GetComponent<StatComponent>();
            if (ignoreDeadEnemies && bossStats != null && bossStats.IsDead)
                continue;

            float distanceSqr = GetDistanceSqr(listenerTarget.position, boss.transform.position);
            if (distanceSqr <= bossRadiusSqr)
                return true;
        }

        return false;
    }

    private static bool IsBossEnemy(Enemy enemy)
    {
        return enemy != null
            && (enemy.GetComponent<FinalBossVictoryTrigger>() != null
                || enemy.GetComponentInParent<FinalBossVictoryTrigger>() != null);
    }

    private bool IsValidEnemy(Enemy enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || !IsLayerAllowed(enemy.gameObject))
            return false;

        StatComponent stats = enemy.GetComponent<StatComponent>();
        return !ignoreDeadEnemies || stats == null || !stats.IsDead;
    }

    private bool IsLayerAllowed(GameObject target)
    {
        return target != null && (enemyLayers.value & (1 << target.layer)) != 0;
    }

    private float GetDistanceSqr(Vector3 a, Vector3 b)
    {
        if (useFlatDistance)
        {
            a.y = 0f;
            b.y = 0f;
        }

        return (a - b).sqrMagnitude;
    }

    private void SwitchToState(BackgroundAudioState state, bool immediate)
    {
        currentState = state;

        AudioClip targetClip = GetClipForState(state, out float targetVolume);
        targetVolume *= masterVolume;

        if (targetClip == currentClip)
        {
            if (activeSource != null)
                activeSource.volume = targetVolume;
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeToClip(targetClip, targetVolume, immediate ? 0f : fadeSeconds));
    }

    private IEnumerator FadeToClip(AudioClip targetClip, float targetVolume, float duration)
    {
        AudioSource from = activeSource;
        AudioSource to = inactiveSource;
        float fromStartVolume = from != null ? from.volume : 0f;

        if (targetClip != null)
        {
            ConfigureSource(to);
            to.clip = targetClip;
            to.volume = duration <= 0f ? targetVolume : 0f;

            if (!to.isPlaying)
                to.Play();
        }

        if (duration <= 0f)
        {
            if (from != null && from != to)
                from.Stop();

            if (targetClip == null)
                activeSource = from;
            else
                SwapSources();

            currentClip = targetClip;
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (from != null)
                from.volume = Mathf.Lerp(fromStartVolume, 0f, t);

            if (to != null && targetClip != null)
                to.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        if (from != null)
        {
            from.Stop();
            from.volume = 0f;
        }

        if (targetClip != null)
            SwapSources();

        currentClip = targetClip;
        fadeRoutine = null;
    }

    private AudioClip GetClipForState(BackgroundAudioState state, out float volume)
    {
        switch (state)
        {
            case BackgroundAudioState.MusicZone:
                if (activeZone != null && activeZone.clip != null)
                {
                    volume = activeZone.volume;
                    return activeZone.clip;
                }

                break;

            case BackgroundAudioState.BossFight:
                if (bossFightClip != null)
                {
                    volume = bossFightVolume;
                    return bossFightClip;
                }

                if (combatClip != null)
                {
                    volume = combatVolume;
                    return combatClip;
                }

                break;

            case BackgroundAudioState.Combat:
                if (combatClip != null)
                {
                    volume = combatVolume;
                    return combatClip;
                }

                break;

            case BackgroundAudioState.EnemyNearby:
                if (enemyNearbyClip != null)
                {
                    volume = enemyNearbyVolume;
                    return enemyNearbyClip;
                }

                break;
        }

        if (enemyNearbyClip != null && state != BackgroundAudioState.Idle)
        {
            volume = enemyNearbyVolume;
            return enemyNearbyClip;
        }

        volume = idleVolume;
        return idleClip;
    }

    private void SwapSources()
    {
        AudioSource previousActive = activeSource;
        activeSource = inactiveSource;
        inactiveSource = previousActive;
    }

    private void ResolveSources()
    {
        if (primarySource == null)
            primarySource = gameObject.AddComponent<AudioSource>();

        if (secondarySource == null)
            secondarySource = gameObject.AddComponent<AudioSource>();
    }

    private void ConfigureSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = outputMixerGroup;
    }

    private void ResolveListenerTarget()
    {
        if (listenerTarget != null || string.IsNullOrEmpty(listenerTag))
            return;

        GameObject targetObject = GameObject.FindGameObjectWithTag(listenerTag);
        if (targetObject != null)
            listenerTarget = targetObject.transform;
    }

    private void OnValidate()
    {
        enemyNearbyRadius = Mathf.Max(0f, enemyNearbyRadius);
        bossFightRadius = Mathf.Max(0f, bossFightRadius);
        attackDetectionWindow = Mathf.Max(0f, attackDetectionWindow);
        combatHoldSeconds = Mathf.Max(0f, combatHoldSeconds);
        scanInterval = Mathf.Max(0.05f, scanInterval);
        fadeSeconds = Mathf.Max(0f, fadeSeconds);
    }

    private void OnDrawGizmos()
    {
        DrawZoneGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        Transform target = listenerTarget != null ? listenerTarget : transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, enemyNearbyRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(target.position, bossFightRadius);
    }

    private void DrawZoneGizmos()
    {
        if (musicZones == null)
            return;

        for (int i = 0; i < musicZones.Count; i++)
        {
            MusicZone zone = musicZones[i];
            if (zone == null)
                continue;

            Color wire = zone.gizmoColor;
            Color fill = new Color(wire.r, wire.g, wire.b, 0.12f);

            if (zone.shape == ZoneShape.Sphere)
            {
                Gizmos.color = fill;
                Gizmos.DrawSphere(zone.center, zone.radius);
                Gizmos.color = wire;
                Gizmos.DrawWireSphere(zone.center, zone.radius);
            }
            else
            {
                Gizmos.color = fill;
                Gizmos.DrawCube(zone.center, zone.size);
                Gizmos.color = wire;
                Gizmos.DrawWireCube(zone.center, zone.size);
            }
        }
    }
}
