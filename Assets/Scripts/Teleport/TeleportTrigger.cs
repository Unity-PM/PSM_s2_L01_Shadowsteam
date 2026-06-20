using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class TeleportTrigger : MonoBehaviour
{
    public enum TeleportMode
    {
        Local,
        Scene
    }

    [Header("Target Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Mode")]
    [SerializeField] private TeleportMode mode = TeleportMode.Local;
    [SerializeField] private Transform localDestination;

    [Header("Scene Teleport")]
    [SerializeField] private string destinationSceneName;
    [SerializeField] private string destinationSpawnId = "Default";
    [SerializeField] private bool makeTargetPersistent = true;
    [SerializeField] private GameObject[] extraObjectsToPersist;

    [Header("Transform")]
    [SerializeField] private bool alignRotation = true;
    [SerializeField] private bool resetPhysics = true;
    [Min(0f)]
    [SerializeField] private float reenterCooldown = 0.75f;
    [Tooltip("Seconds to wait before moving the target. Set to 0 for instant teleport.")]
    [Min(0f)]
    [SerializeField] private float teleportDelay;

    [Header("VFX")]
    [SerializeField] private GameObject teleportVfxPrefab;
    [SerializeField] private Transform sourceVfxPoint;
    [SerializeField] private bool playVfxAtSource = true;
    [SerializeField] private bool playVfxAtDestination = true;
    [SerializeField] private float vfxLifetime = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip teleportAudioClip;
    [Tooltip("Optional clip for arrival. If empty, Teleport Audio Clip is reused.")]
    [SerializeField] private AudioClip destinationAudioClip;
    [SerializeField] private AudioSource sourceAudioOverride;
    [SerializeField] private Transform sourceAudioPoint;
    [SerializeField] private bool playAudioAtSource = true;
    [SerializeField] private bool playAudioAtDestination = true;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float audioSpatialBlend = 1f;

    private static readonly Dictionary<int, float> CooldownUntilByTarget = new Dictionary<int, float>();
    private Collider triggerCollider;

    public event Action<GameObject> TeleportStarted;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
        sourceAudioOverride = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject target = ResolveTeleportTarget(other);
        StartTeleportIfPossible(target);
    }

    public void Teleport(GameObject target)
    {
        if (target == null || IsOnCooldown(target))
            return;

        StartTeleportIfPossible(target);
    }

    private void StartTeleportIfPossible(GameObject target)
    {
        if (target == null || IsOnCooldown(target))
            return;

        SetCooldown(target);
        TeleportStarted?.Invoke(target);
        StartCoroutine(TeleportRoutine(target));
    }

    private IEnumerator TeleportRoutine(GameObject target)
    {
        if (playVfxAtSource)
            PlayVfxAt(GetSourceVfxTransform());

        if (playAudioAtSource)
            PlaySourceAudio();

        if (teleportDelay > 0f)
            yield return new WaitForSeconds(teleportDelay);

        if (target == null)
            yield break;

        if (mode == TeleportMode.Local)
        {
            if (localDestination == null)
            {
                Debug.LogWarning("Local teleport skipped: local destination is not assigned.", this);
                yield break;
            }

            SceneTeleportManager.TeleportObjectTo(target, localDestination, alignRotation, resetPhysics);

            if (playVfxAtDestination)
                PlayVfxAt(localDestination);

            if (playAudioAtDestination)
                PlayDestinationAudio(localDestination);

            yield break;
        }

        if (string.IsNullOrWhiteSpace(destinationSceneName))
        {
            Debug.LogWarning("Scene teleport skipped: destination scene name is empty.", this);
            yield break;
        }

        if (SceneManager.GetActiveScene().name == destinationSceneName)
        {
            TeleportSpawnPoint spawnPoint = FindLocalSpawn(destinationSpawnId);
            if (spawnPoint == null)
            {
                Debug.LogWarning($"Scene teleport skipped: spawn point '{destinationSpawnId}' was not found in current scene.", this);
                yield break;
            }

            SceneTeleportManager.TeleportObjectTo(target, spawnPoint.transform, alignRotation, resetPhysics);

            if (playVfxAtDestination)
                PlayVfxAt(spawnPoint.transform);

            if (playAudioAtDestination)
                PlayDestinationAudio(spawnPoint.transform);

            yield break;
        }

        SceneTeleportManager.BeginSceneTeleport(
            target,
            destinationSceneName,
            destinationSpawnId,
            makeTargetPersistent,
            extraObjectsToPersist,
            playVfxAtDestination ? teleportVfxPrefab : null,
            vfxLifetime,
            playAudioAtDestination ? GetDestinationAudioClip() : null,
            audioVolume,
            audioSpatialBlend,
            alignRotation,
            resetPhysics
        );
    }

    private GameObject ResolveTeleportTarget(Collider other)
    {
        if (other == null)
            return null;

        if (requireTag)
        {
            Transform tagged = FindTaggedAncestor(other.transform);
            return tagged != null ? tagged.gameObject : null;
        }

        if (other.attachedRigidbody != null)
            return other.attachedRigidbody.gameObject;

        return other.gameObject;
    }

    private Transform FindTaggedAncestor(Transform current)
    {
        while (current != null)
        {
            if (current.CompareTag(requiredTag))
                return current;

            current = current.parent;
        }

        return null;
    }

    private bool IsOnCooldown(GameObject target)
    {
        int id = target.GetInstanceID();
        return CooldownUntilByTarget.TryGetValue(id, out float until) && Time.time < until;
    }

    private void SetCooldown(GameObject target)
    {
        CooldownUntilByTarget[target.GetInstanceID()] = Time.time + Mathf.Max(0.05f, reenterCooldown);
    }

    private Transform GetSourceVfxTransform()
    {
        return sourceVfxPoint != null ? sourceVfxPoint : transform;
    }

    private Transform GetSourceAudioTransform()
    {
        return sourceAudioPoint != null ? sourceAudioPoint : transform;
    }

    private AudioClip GetDestinationAudioClip()
    {
        return destinationAudioClip != null ? destinationAudioClip : teleportAudioClip;
    }

    private void PlaySourceAudio()
    {
        AudioClip clip = teleportAudioClip;
        if (clip == null)
            return;

        Transform point = GetSourceAudioTransform();
        if (point == null)
            return;

        SceneTeleportManager.PlayAudio(clip, point.position, audioVolume, audioSpatialBlend, sourceAudioOverride);
    }

    private void PlayDestinationAudio(Transform destination)
    {
        AudioClip clip = GetDestinationAudioClip();
        if (clip == null || destination == null)
            return;

        SceneTeleportManager.PlayAudio(clip, destination.position, audioVolume, audioSpatialBlend);
    }

    private void PlayVfxAt(Transform point)
    {
        if (point == null)
            return;

        SceneTeleportManager.PlayVfx(teleportVfxPrefab, point.position, point.rotation, vfxLifetime);
    }

    private static TeleportSpawnPoint FindLocalSpawn(string spawnId)
    {
        TeleportSpawnPoint[] spawnPoints = FindObjectsByType<TeleportSpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i].Matches(spawnId))
                return spawnPoints[i];
        }

        return spawnPoints.Length > 0 ? spawnPoints[0] : null;
    }
}
