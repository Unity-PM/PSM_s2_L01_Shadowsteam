using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Реагирует на <see cref="DeathEvent"/>: смерть/пауза, проигрывание клипов, респавн и восстановление статов.
/// Анимация через <see cref="DynamicAnimator"/> (файл <c>Assets/Anton/Scripts/Animation Handler/DynamicAnimator.cs</c>), как у <see cref="Platformer.Enemy"/> и <c>NPCTestController</c>.
/// Точка спавна — <see cref="spawnAnchor"/> (на момент респавна) или снимок в <see cref="Start"/> при <see cref="rememberStartPoseWhenNoAnchor"/>.
/// </summary>
public class PlayerDeathHandler : MonoBehaviour {
    [Header("Spawn")]
    [Tooltip("Если задан — при респавне берём world position/rotation у этого Transform (чекпоинты, пустышка в сцене).")]
    [SerializeField] Transform spawnAnchor;
    [Tooltip("Если spawnAnchor не задан, при старте сцены запоминаем позицию/поворот игрока (типичный респавн «как при загрузке уровня»).")]
    [SerializeField] bool rememberStartPoseWhenNoAnchor = true;

    [Header("Death & respawn")]
    [SerializeField] float respawnDelaySeconds = 3f;
    [SerializeField] bool restoreFullVitalityOnRespawn = true;
    [SerializeField] float fallToGroundTimeoutSeconds = 6f;

    [Header("Animation (DynamicAnimator)")]
    [Tooltip("Необязательно: находится автоматически на этом объекте или в дочерних, как у Enemy.")]
    [FormerlySerializedAs("clipAnimator")]
    [SerializeField] DynamicAnimator dynamicAnimator;
    [SerializeField] bool playDeathAnimationOnDeath = true;
    [SerializeField] string deathAnimationStateId = "Die";
    [SerializeField] string respawnAnimationStateId = "Idle";

    [Header("Movement")]
    [SerializeField] bool disableMovementWhileDead = true;

    [Header("Optional")]
    [SerializeField] bool publishLegacyGameOverEvent;
    [SerializeField] UnityEvent onDeathSequenceStarted;
    [SerializeField] UnityEvent onRespawnCompleted;

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

        if (dynamicAnimator != null) {
            dynamicAnimator.SetInputEnabled(true);
            if (!string.IsNullOrEmpty(respawnAnimationStateId))
                dynamicAnimator.ResetToState(respawnAnimationStateId);
        }

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

    Vector3 ResolveSpawnPosition() =>
        spawnAnchor != null ? spawnAnchor.position : spawnPositionSnapshot;

    Quaternion ResolveSpawnRotation() =>
        spawnAnchor != null ? spawnAnchor.rotation : spawnRotationSnapshot;

    void TeleportToSpawn() {
        if (spawnAnchor == null && !rememberStartPoseWhenNoAnchor) {
            Debug.LogWarning(
                "PlayerDeathHandler: задайте spawnAnchor или включите rememberStartPoseWhenNoAnchor — телепорт пропущен.",
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

        if (dynamicAnimator != null)
            dynamicAnimator.SetInputEnabled(!locked);
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
