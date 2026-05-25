using System;
using Platformer;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy SFX: locomotion loops, attack, hit, death. Uses 3D spatial audio.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EnemyAudioController : MonoBehaviour {
    [Header("References")]
    [SerializeField] DynamicAnimator dynamicAnimator;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] StatComponent stats;
    [SerializeField] Enemy enemy;
    [SerializeField] AudioSource sfxSource;

    [Header("Locomotion")]
    [SerializeField] AudioClip walkLoopClip;
    [SerializeField] AudioClip runLoopClip;
    [SerializeField] float runPitchMultiplier = 1.35f;
    [SerializeField] AudioSource locomotionLoopSource;
    [SerializeField] string walkAnimationStateId = "WalkFWD";
    [SerializeField] string runAnimationStateId = "Run";
    [SerializeField] float minMoveSpeed = 0.12f;

    [Header("Combat")]
    [SerializeField] AudioClip attackSwingClip;
    [SerializeField] AudioClip hitClip;
    [SerializeField] string attackAnimationStateId = "Attack";

    [Header("Vitality")]
    [SerializeField] AudioClip deathClip;

    [Header("Mix")]
    [Range(0f, 1f)]
    [SerializeField] float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] float footstepVolume = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] float actionVolume = 0.85f;
    [SerializeField] float minDistance = 1f;
    [SerializeField] float maxDistance = 28f;

    string lastAnimationStateId;
    float lastAttackSwingTime;
    float lastHitSoundTime;
    bool suppressLocomotion;
    bool isLocomotionLoopPlaying;

    void Awake() {
        if (dynamicAnimator == null)
            dynamicAnimator = GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (stats == null)
            stats = GetComponent<StatComponent>();

        if (enemy == null)
            enemy = GetComponent<Enemy>();

        SyncAnimationIdsFromEnemy();

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        ConfigureSpatialSource(sfxSource, false);

        if (locomotionLoopSource == null) {
            locomotionLoopSource = gameObject.AddComponent<AudioSource>();
            ConfigureSpatialSource(locomotionLoopSource, true);
        }
        else {
            ConfigureSpatialSource(locomotionLoopSource, true);
        }
    }

    void SyncAnimationIdsFromEnemy() {
        if (enemy == null)
            return;

        walkAnimationStateId = enemy.AnimWalkId;
        runAnimationStateId = enemy.AnimRunId;
        attackAnimationStateId = TrimAttackPrefix(enemy.AnimAttackId);
    }

    static string TrimAttackPrefix(string attackId) {
        if (string.IsNullOrEmpty(attackId))
            return "Attack";

        const string prefix = "Attack";
        return attackId.StartsWith(prefix, StringComparison.Ordinal) ? prefix : attackId;
    }

    void ConfigureSpatialSource(AudioSource source, bool loop) {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 1f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
    }

    void OnEnable() {
        EventBus.Subscribe<DeathEvent>(OnDeath);
        EventBus.Subscribe<StatChangeEvent>(OnStatChange);
    }

    void OnDisable() {
        EventBus.Unsubscribe<DeathEvent>(OnDeath);
        EventBus.Unsubscribe<StatChangeEvent>(OnStatChange);
    }

    void Update() {
        TrackAnimationSounds();
        UpdateLocomotionAudio();
    }

    void TrackAnimationSounds() {
        if (dynamicAnimator == null)
            return;

        string stateId = dynamicAnimator.CurrentStateId;
        if (string.IsNullOrEmpty(stateId) || stateId == lastAnimationStateId)
            return;

        if (IsAttackState(stateId) && !IsAttackState(lastAnimationStateId))
            TryPlayAttackSwing();

        lastAnimationStateId = stateId;
    }

    void UpdateLocomotionAudio() {
        UpdateLocomotionLoop();
    }

    void UpdateLocomotionLoop() {
        if (locomotionLoopSource == null)
            return;

        if (suppressLocomotion || dynamicAnimator == null || agent == null || !agent.enabled) {
            StopLocomotionLoop();
            return;
        }

        if (dynamicAnimator.IsMovementLocked || !TryGetLocomotion(out bool isRunning, out _)) {
            StopLocomotionLoop();
            return;
        }

        AudioClip loopClip = isRunning && runLoopClip != null ? runLoopClip : walkLoopClip;
        float targetPitch = isRunning && runLoopClip == null ? runPitchMultiplier : 1f;

        if (loopClip == null) {
            StopLocomotionLoop();
            return;
        }

        if (isLocomotionLoopPlaying
            && locomotionLoopSource.isPlaying
            && locomotionLoopSource.clip == loopClip) {
            locomotionLoopSource.pitch = targetPitch;
            locomotionLoopSource.volume = footstepVolume * masterVolume;
            return;
        }

        locomotionLoopSource.clip = loopClip;
        locomotionLoopSource.pitch = targetPitch;
        locomotionLoopSource.volume = footstepVolume * masterVolume;
        locomotionLoopSource.loop = true;
        locomotionLoopSource.Play();
        isLocomotionLoopPlaying = true;
    }

    void StopLocomotionLoop() {
        if (locomotionLoopSource == null)
            return;

        if (locomotionLoopSource.isPlaying)
            locomotionLoopSource.Stop();

        locomotionLoopSource.pitch = 1f;
        isLocomotionLoopPlaying = false;
    }

    bool TryGetLocomotion(out bool isRunning, out float speed) {
        isRunning = false;
        speed = 0f;

        if (dynamicAnimator == null)
            return false;

        string animId = dynamicAnimator.CurrentStateId;
        if (string.IsNullOrEmpty(animId))
            return false;

        if (string.Equals(animId, runAnimationStateId, StringComparison.Ordinal)) {
            isRunning = true;
        }
        else if (!string.Equals(animId, walkAnimationStateId, StringComparison.Ordinal)) {
            return false;
        }

        if (agent != null) {
            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            speed = velocity.magnitude;
        }

        return speed >= minMoveSpeed;
    }

    bool IsAttackState(string stateId) =>
        !string.IsNullOrEmpty(stateId)
        && stateId.StartsWith(attackAnimationStateId, StringComparison.Ordinal);

    void TryPlayAttackSwing() {
        if (attackSwingClip == null || Time.time - lastAttackSwingTime < 0.15f)
            return;

        lastAttackSwingTime = Time.time;
        PlayAction(attackSwingClip);
    }

    void TryPlayHit() {
        if (hitClip == null || Time.time - lastHitSoundTime < 0.1f)
            return;

        lastHitSoundTime = Time.time;
        PlayAction(hitClip);
    }

    void PlayAction(AudioClip clip) {
        if (clip == null)
            return;

        PlayOneShot(clip, actionVolume * masterVolume);
    }

    void PlayOneShot(AudioClip clip, float volume, float pitch = 1f) {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = 1f;
    }

    void OnStatChange(StatChangeEvent e) {
        if (stats == null || e.target != stats || e.statType != StatType.HP || e.amount >= 0f)
            return;

        if (stats.IsDead)
            return;

        TryPlayHit();
    }

    void OnDeath(DeathEvent e) {
        if (stats == null || e.target != stats)
            return;

        suppressLocomotion = true;
        StopLocomotionLoop();
        PlayAction(deathClip);
    }

    public void PlayAttackSwing() => TryPlayAttackSwing();

    public void PlayHit() => TryPlayHit();
}
