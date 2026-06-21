using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Reacts to <see cref="DeathEvent"/>: death/pause, clip playback, respawn, and stat restoration.
/// Animation via <see cref="DynamicAnimator"/> (file <c>Assets/Scripts/Animation/DynamicAnimator.cs</c>), like <see cref="Platformer.Enemy"/> and <c>NpcAnimationTestController</c>.
/// Spawn point — <see cref="spawnAnchor"/> (at respawn time) or a snapshot in <see cref="Start"/> when <see cref="rememberStartPoseWhenNoAnchor"/>.
/// </summary>
public class PlayerDeathHandler : MonoBehaviour {
    [Header("Spawn")]
    [Tooltip("If set — on respawn, take world position/rotation from this Transform (checkpoints, an empty in the scene).")]
    [SerializeField] Transform spawnAnchor;
    [Tooltip("If spawnAnchor is not set, snapshot the player's position/rotation at scene start (typical \"respawn as on level load\").")]
    [SerializeField] bool rememberStartPoseWhenNoAnchor = true;

    [Header("Death & respawn")]
    [SerializeField] float respawnDelaySeconds = 3f;
    [SerializeField] bool restoreFullVitalityOnRespawn = true;
    [SerializeField] float fallToGroundTimeoutSeconds = 6f;

    [Header("Animation (DynamicAnimator)")]
    [Tooltip("Optional: found automatically on this object or its children, like on Enemy.")]
    [FormerlySerializedAs("clipAnimator")]
    [SerializeField] DynamicAnimator dynamicAnimator;
    [SerializeField] bool playDeathAnimationOnDeath = true;
    [SerializeField] string deathAnimationStateId = "Die";
    [SerializeField] string respawnAnimationStateId = "Idle";

    [Header("Movement")]
    [SerializeField] bool disableMovementWhileDead = true;

    [Header("Optional")]
    [SerializeField] bool publishLegacyGameOverEvent;
    public UnityEvent onDeathSequenceStarted;
    public UnityEvent onRespawnCompleted;

    Vector3 spawnPositionSnapshot;
    Quaternion spawnRotationSnapshot;

    StatComponent stats;
    MovementBrain movementBrain;
    CharacterController characterController;

    Coroutine deathRoutine;

    void Awake() {
        stats = GetComponent<StatComponent>();
        if (stats == null)
            Debug.LogError("PlayerDeathHandler requires StatComponent on the same GameObject.", this);

        movementBrain = GetComponent<MovementBrain>();
        characterController = GetComponent<CharacterController>();
        if (dynamicAnimator == null)
            dynamicAnimator = GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();
    }

    void Start() {
        if (spawnAnchor == null && rememberStartPoseWhenNoAnchor)
            CaptureSpawnFromTransform(transform);
    }

    void OnEnable() {
        EventBus.Subscribe<DeathEvent>(OnDeath);
    }

    void OnDisable() {
        EventBus.Unsubscribe<DeathEvent>(OnDeath);
        if (deathRoutine != null) {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
    }

    void OnDeath(DeathEvent e) {
        if (stats == null || e.target != stats)
            return;
        if (deathRoutine != null)
            return;

        deathRoutine = StartCoroutine(DeathAndRespawnRoutine());
    }

    IEnumerator DeathAndRespawnRoutine() {
        onDeathSequenceStarted?.Invoke();
        EventBus.Publish(new RespawnStartedEvent(respawnDelaySeconds));
        stats.HPRegenPaused = true;

        if (publishLegacyGameOverEvent)
            EventBus.Publish(new GameOverEvent(true));

        SetPlayerControlLocked(true);

        if (IsAirborne())
            yield return WaitUntilGrounded();

        PlayDeathAnimation();

        float deathClipLength = 0f;
        if (dynamicAnimator != null && dynamicAnimator.TryGetClipLength(deathAnimationStateId, out deathClipLength))
            yield return new WaitForSeconds(Mathf.Max(respawnDelaySeconds, deathClipLength));
        else
            yield return new WaitForSeconds(Mathf.Max(0f, respawnDelaySeconds));

        TeleportToSpawn();

        if (restoreFullVitalityOnRespawn)
            RestoreFullVitality(stats);

        stats.ClearDeadState();
        stats.HPRegenPaused = false;

        if (dynamicAnimator != null && !string.IsNullOrEmpty(respawnAnimationStateId))
            dynamicAnimator.ResetToState(respawnAnimationStateId);

        SetPlayerControlLocked(false);

        onRespawnCompleted?.Invoke();
        deathRoutine = null;
    }

    void PlayDeathAnimation() {
        if (!playDeathAnimationOnDeath || dynamicAnimator == null || string.IsNullOrEmpty(deathAnimationStateId))
            return;

        dynamicAnimator.ForcePlay(deathAnimationStateId);
    }

    bool IsAirborne() {
        if (movementBrain != null)
            return movementBrain.IsAirborne;

        return characterController != null && !characterController.isGrounded;
    }

    IEnumerator WaitUntilGrounded() {
        float elapsed = 0f;

        while (elapsed < fallToGroundTimeoutSeconds) {
            if (characterController != null && characterController.isGrounded)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void CaptureSpawnFromTransform(Transform t) {
        spawnPositionSnapshot = t.position;
        spawnRotationSnapshot = t.rotation;
    }

    public void SetRespawnPoint(Vector3 position, Quaternion rotation) {
        spawnAnchor = null;
        spawnPositionSnapshot = position;
        spawnRotationSnapshot = rotation;
    }

    public void SetRespawnAnchor(Transform anchor) {
        spawnAnchor = anchor;

        if (spawnAnchor != null)
            CaptureSpawnFromTransform(spawnAnchor);
    }

    Vector3 ResolveSpawnPosition() =>
        spawnAnchor != null ? spawnAnchor.position : spawnPositionSnapshot;

    Quaternion ResolveSpawnRotation() =>
        spawnAnchor != null ? spawnAnchor.rotation : spawnRotationSnapshot;

    void TeleportToSpawn() {
        if (spawnAnchor == null && !rememberStartPoseWhenNoAnchor) {
            Debug.LogWarning(
                "PlayerDeathHandler: set spawnAnchor or enable rememberStartPoseWhenNoAnchor — teleport skipped.",
                this);
            return;
        }

        Vector3 p = ResolveSpawnPosition();
        Quaternion r = ResolveSpawnRotation();

        if (characterController != null) {
            characterController.enabled = false;
            transform.SetPositionAndRotation(p, r);
            characterController.enabled = true;
        }
        else
            transform.SetPositionAndRotation(p, r);
    }

    void SetPlayerControlLocked(bool locked) {
        if (!disableMovementWhileDead)
            return;

        if (movementBrain != null) {
            movementBrain.SetInputLocked(locked);
            if (locked)
                movementBrain.StopHorizontalMovement();
        }
    }

    static void RestoreFullVitality(StatComponent s) {
        if (s == null)
            return;

        float dHp = s.getMaxHP() - s.getHP();
        if (Mathf.Abs(dHp) > 0.001f)
            EventBus.Publish(new StatChangeEvent(s, StatType.HP, dHp));

        float dMp = s.getMaxMP() - s.getMP();
        if (Mathf.Abs(dMp) > 0.001f)
            EventBus.Publish(new StatChangeEvent(s, StatType.MP, dMp));

        float dSt = s.getMaxStamina() - s.getStamina();
        if (Mathf.Abs(dSt) > 0.001f)
            EventBus.Publish(new StatChangeEvent(s, StatType.Stamina, dSt));
    }
}
