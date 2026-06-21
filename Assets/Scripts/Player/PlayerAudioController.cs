using System;
using UnityEngine;

/// <summary>
/// Player SFX: footsteps, jump/land, attack, death/respawn.
/// Attach to the player GameObject and assign clips in the Inspector.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerAudioController : MonoBehaviour {
    [Header("References")]
    [SerializeField] MovementBrain movementBrain;
    [SerializeField] DynamicAnimator dynamicAnimator;
    [SerializeField] PlayerDeathHandler deathHandler;
    [SerializeField] AudioSource sfxSource;

    [Header("Locomotion")]
    [SerializeField] AudioClip jumpClip;
    [SerializeField] AudioClip landClip;
    [Tooltip("Loop while walking (also used for run/sprint at Run Pitch Multiplier speed).")]
    [SerializeField] AudioClip walkLoopClip;
    [Tooltip("Optional separate run loop. Leave empty to reuse Walk Loop Clip faster.")]
    [SerializeField] AudioClip runLoopClip;
    [Tooltip("Playback speed multiplier for run/sprint animation (1.5 = 50% faster walk sound).")]
    [SerializeField] float runPitchMultiplier = 1.5f;
    [Tooltip("Short one-shot clips played on an interval. Leave empty if you use loop clips above.")]
    [SerializeField] AudioClip[] walkFootstepClips;
    [SerializeField] AudioClip[] runFootstepClips;
    [SerializeField] AudioSource locomotionLoopSource;
    [SerializeField] float walkStepInterval = 0.48f;
    [SerializeField] string walkAnimationStateId = "Walk";
    [SerializeField] string walkBackAnimationStateId = "WalkBack";
    [SerializeField] string runAnimationStateId = "Run";
    [SerializeField] Vector2 footstepPitchRange = new Vector2(0.92f, 1.08f);
    [Tooltip("Play land SFX when feet are this close to the ground while falling.")]
    [SerializeField] float landAnticipationDistance = 0.45f;
    [Tooltip("Play land SFX this many seconds before estimated ground impact.")]
    [SerializeField] float landLeadTime = 0.12f;
    [SerializeField] float landMinFallSpeed = 0.75f;
    [SerializeField] float groundProbeDistance = 20f;

    [Header("Combat")]
    [SerializeField] AudioClip attackSwingClip;
    [SerializeField] AudioClip attackHitClip;
    [SerializeField] string attackAnimationStateId = "Attack";

    [Header("Vitality")]
    [SerializeField] AudioClip deathClip;
    [SerializeField] AudioClip respawnClip;

    [Header("Mix")]
    [Range(0f, 1f)]
    [SerializeField] float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] float footstepVolume = 0.55f;
    [Range(0f, 1f)]
    [SerializeField] float actionVolume = 0.85f;

    CharacterController characterController;
    bool wasAirborne;
    string lastAnimationStateId;
    float footstepTimer;
    bool suppressFootsteps;
    float lastJumpSoundTime;
    float lastAttackSwingTime;
    float lastLandSoundTime;
    bool landSoundPlayedForFall;
    bool isLocomotionLoopPlaying;

    void Awake() {
        if (movementBrain == null)
            movementBrain = GetComponent<MovementBrain>();

        if (dynamicAnimator == null)
            dynamicAnimator = GetComponent<DynamicAnimator>() ?? GetComponentInChildren<DynamicAnimator>();

        if (deathHandler == null)
            deathHandler = GetComponent<PlayerDeathHandler>();

        characterController = GetComponent<CharacterController>();

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        if (locomotionLoopSource == null) {
            locomotionLoopSource = gameObject.AddComponent<AudioSource>();
            locomotionLoopSource.playOnAwake = false;
            locomotionLoopSource.loop = true;
            locomotionLoopSource.spatialBlend = 0f;
        }
    }

    void OnEnable() {
        if (movementBrain == null)
            movementBrain = GetComponent<MovementBrain>();

        if (movementBrain != null)
            movementBrain.JumpStarted += TryPlayJump;

        if (deathHandler != null) {
            deathHandler.onDeathSequenceStarted.AddListener(HandleDeathStarted);
            deathHandler.onRespawnCompleted.AddListener(HandleRespawnCompleted);
        }
    }

    void OnDisable() {
        if (movementBrain != null)
            movementBrain.JumpStarted -= TryPlayJump;

        if (deathHandler != null) {
            deathHandler.onDeathSequenceStarted.RemoveListener(HandleDeathStarted);
            deathHandler.onRespawnCompleted.RemoveListener(HandleRespawnCompleted);
        }
    }

    void Update() {
        TrackAnimationSounds();
        UpdateLocomotionAudio();
    }

    void LateUpdate() {
        TrackAirborneSounds();
    }

    void TrackAirborneSounds() {
        if (movementBrain == null)
            return;

        bool airborne = movementBrain.IsAirborne;
        float verticalVelocity = movementBrain.VerticalVelocityY;

        if (airborne && verticalVelocity < -landMinFallSpeed && !landSoundPlayedForFall) {
            if (TryGetDistanceToGround(out float groundDistance)) {
                float fallSpeed = -verticalVelocity;
                float timeToImpact = groundDistance / fallSpeed;

                if (groundDistance <= landAnticipationDistance || timeToImpact <= landLeadTime)
                    TryPlayLand();
            }
        }
        else if (wasAirborne && !airborne && !landSoundPlayedForFall)
            TryPlayLand();

        if (!airborne)
            landSoundPlayedForFall = false;

        wasAirborne = airborne;
    }

    bool TryGetDistanceToGround(out float distance) {
        distance = float.MaxValue;

        if (characterController == null)
            return false;

        Vector3 origin = GetFeetProbeOrigin();
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            groundProbeDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        float closest = float.MaxValue;
        bool found = false;

        foreach (RaycastHit hit in hits) {
            if (hit.collider == null || IsSelfCollider(hit.collider))
                continue;

            if (hit.distance < closest) {
                closest = hit.distance;
                found = true;
            }
        }

        if (!found)
            return false;

        distance = closest;
        return true;
    }

    Vector3 GetFeetProbeOrigin() {
        Vector3 worldCenter = transform.TransformPoint(characterController.center);
        float halfHeight = characterController.height * 0.5f;
        float feetOffset = halfHeight - characterController.radius;
        return worldCenter + Vector3.down * feetOffset + Vector3.up * 0.05f;
    }

    bool IsSelfCollider(Collider collider) =>
        collider.transform == transform || collider.transform.IsChildOf(transform);

    void TryPlayLand() {
        if (landClip == null || landSoundPlayedForFall || Time.time - lastLandSoundTime < 0.15f)
            return;

        landSoundPlayedForFall = true;
        lastLandSoundTime = Time.time;
        PlayAction(landClip);
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

    void TryPlayJump() {
        if (jumpClip == null || Time.time - lastJumpSoundTime < 0.15f)
            return;

        lastJumpSoundTime = Time.time;
        PlayAction(jumpClip);
    }

    void TryPlayAttackSwing() {
        if (attackSwingClip == null || Time.time - lastAttackSwingTime < 0.15f)
            return;

        lastAttackSwingTime = Time.time;
        PlayAction(attackSwingClip);
    }

    bool IsAttackState(string stateId) =>
        !string.IsNullOrEmpty(stateId)
        && stateId.StartsWith(attackAnimationStateId, StringComparison.Ordinal);

    void UpdateLocomotionAudio() {
        UpdateLocomotionLoop();
        UpdateFootsteps();
    }

    void UpdateLocomotionLoop() {
        if (locomotionLoopSource == null)
            return;

        if (suppressFootsteps || movementBrain == null || movementBrain.IsAirborne || dynamicAnimator == null) {
            StopLocomotionLoop();
            return;
        }

        if (!TryGetLocomotion(out bool isRunning, out _)) {
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

    void UpdateFootsteps() {
        if (suppressFootsteps || movementBrain == null || movementBrain.IsAirborne || dynamicAnimator == null) {
            footstepTimer = 0f;
            return;
        }

        if (!TryGetLocomotion(out bool isRunning, out float speed)) {
            footstepTimer = 0f;
            return;
        }

        AudioClip[] clips = walkFootstepClips;
        if (isRunning && runFootstepClips != null && runFootstepClips.Length > 0)
            clips = runFootstepClips;

        if (clips == null || clips.Length == 0)
            return;

        float interval = walkStepInterval / (isRunning ? runPitchMultiplier : 1f);
        float speedFactor = Mathf.Clamp(speed / (isRunning ? 5f : 3f), 0.75f, 1.35f);
        footstepTimer += Time.deltaTime;

        if (footstepTimer < interval / speedFactor)
            return;

        footstepTimer = 0f;
        PlayFootstep(isRunning);
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
        else if (!string.Equals(animId, walkAnimationStateId, StringComparison.Ordinal)
            && !string.Equals(animId, walkBackAnimationStateId, StringComparison.Ordinal)) {
            return false;
        }

        if (characterController != null) {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            speed = velocity.magnitude;
        }

        return true;
    }

    void PlayFootstep(bool running) {
        AudioClip[] clips = walkFootstepClips;
        if (running && runFootstepClips != null && runFootstepClips.Length > 0)
            clips = runFootstepClips;

        AudioClip clip = PickRandomClip(clips);
        if (clip == null)
            return;

        float pitch = UnityEngine.Random.Range(footstepPitchRange.x, footstepPitchRange.y);
        if (running && (runFootstepClips == null || runFootstepClips.Length == 0))
            pitch *= runPitchMultiplier;

        PlayOneShot(clip, footstepVolume * masterVolume, pitch);
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

    static AudioClip PickRandomClip(AudioClip[] clips) {
        if (clips == null || clips.Length == 0)
            return null;

        if (clips.Length == 1)
            return clips[0];

        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }

    void HandleDeathStarted() {
        suppressFootsteps = true;
        footstepTimer = 0f;
        StopLocomotionLoop();
        PlayAction(deathClip);
    }

    void HandleRespawnCompleted() {
        suppressFootsteps = false;
        wasAirborne = movementBrain != null && movementBrain.IsAirborne;
        landSoundPlayedForFall = false;
        PlayAction(respawnClip);
    }

    /// <summary>Call from animation events at the attack hit frame.</summary>
    public void PlayAttackHit() => PlayAction(attackHitClip);

    /// <summary>Call from animation events or other gameplay hooks.</summary>
    public void PlayAttackSwing() => TryPlayAttackSwing();

    public void PlayJump() => TryPlayJump();

    public void PlayLand() => TryPlayLand();
}
