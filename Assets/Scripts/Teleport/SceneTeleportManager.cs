using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class SceneTeleportManager : MonoBehaviour
{
    private class PendingTeleport
    {
        public GameObject target;
        public string spawnId;
        public GameObject destinationVfxPrefab;
        public float vfxLifetime;
        public AudioClip destinationAudioClip;
        public float audioVolume;
        public float audioSpatialBlend;
        public bool alignRotation;
        public bool resetPhysics;
    }

    private static SceneTeleportManager instance;
    private static PendingTeleport pendingTeleport;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        TryApplyPendingTeleport();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryApplyPendingTeleport();
    }

    public static void BeginSceneTeleport(
        GameObject target,
        string sceneName,
        string spawnId,
        bool makeTargetPersistent,
        GameObject[] extraPersistentObjects,
        GameObject destinationVfxPrefab,
        float vfxLifetime,
        AudioClip destinationAudioClip,
        float audioVolume,
        float audioSpatialBlend,
        bool alignRotation,
        bool resetPhysics)
    {
        EnsureInstance().StartCoroutine(SceneTeleportRoutine(
            target,
            sceneName,
            spawnId,
            makeTargetPersistent,
            extraPersistentObjects,
            destinationVfxPrefab,
            vfxLifetime,
            destinationAudioClip,
            audioVolume,
            audioSpatialBlend,
            alignRotation,
            resetPhysics
        ));
    }

    private static SceneTeleportManager EnsureInstance()
    {
        if (instance != null)
            return instance;

        SceneTeleportManager existing = FindFirstObjectByType<SceneTeleportManager>();
        if (existing != null)
            return existing;

        GameObject runner = new GameObject("SceneTeleportManager");
        return runner.AddComponent<SceneTeleportManager>();
    }

    private static IEnumerator SceneTeleportRoutine(
        GameObject target,
        string sceneName,
        string spawnId,
        bool makeTargetPersistent,
        GameObject[] extraPersistentObjects,
        GameObject destinationVfxPrefab,
        float vfxLifetime,
        AudioClip destinationAudioClip,
        float audioVolume,
        float audioSpatialBlend,
        bool alignRotation,
        bool resetPhysics)
    {
        if (target == null || string.IsNullOrWhiteSpace(sceneName))
            yield break;

        if (makeTargetPersistent)
            DontDestroyOnLoad(target);

        if (extraPersistentObjects != null)
        {
            for (int i = 0; i < extraPersistentObjects.Length; i++)
            {
                if (extraPersistentObjects[i] != null)
                    DontDestroyOnLoad(extraPersistentObjects[i]);
            }
        }

        pendingTeleport = new PendingTeleport
        {
            target = target,
            spawnId = spawnId,
            destinationVfxPrefab = destinationVfxPrefab,
            vfxLifetime = vfxLifetime,
            destinationAudioClip = destinationAudioClip,
            audioVolume = audioVolume,
            audioSpatialBlend = audioSpatialBlend,
            alignRotation = alignRotation,
            resetPhysics = resetPhysics
        };

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (load == null)
        {
            Debug.LogWarning($"Teleport scene '{sceneName}' could not be loaded. Check Build Settings scene name.");
            pendingTeleport = null;
            yield break;
        }

        while (!load.isDone)
            yield return null;

        yield return null;
        TryApplyPendingTeleport();
    }

    public static bool TryApplyPendingTeleport()
    {
        if (pendingTeleport == null || pendingTeleport.target == null)
            return false;

        TeleportSpawnPoint spawnPoint = FindSpawnPoint(pendingTeleport.spawnId);
        if (spawnPoint == null)
        {
            Debug.LogWarning($"Teleport spawn point '{pendingTeleport.spawnId}' was not found in scene '{SceneManager.GetActiveScene().name}'.");
            return false;
        }

        TeleportObjectTo(
            pendingTeleport.target,
            spawnPoint.transform,
            pendingTeleport.alignRotation,
            pendingTeleport.resetPhysics
        );

        PlayVfx(
            pendingTeleport.destinationVfxPrefab,
            pendingTeleport.target.transform.position,
            pendingTeleport.target.transform.rotation,
            pendingTeleport.vfxLifetime
        );

        PlayAudio(
            pendingTeleport.destinationAudioClip,
            pendingTeleport.target.transform.position,
            pendingTeleport.audioVolume,
            pendingTeleport.audioSpatialBlend
        );

        pendingTeleport = null;
        return true;
    }

    public static void TeleportObjectTo(GameObject target, Transform destination, bool alignRotation, bool resetPhysics)
    {
        if (target == null || destination == null)
            return;

        CharacterController[] controllers = target.GetComponentsInChildren<CharacterController>();
        for (int i = 0; i < controllers.Length; i++)
            controllers[i].enabled = false;

        NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(destination.position);

        Quaternion rotation = alignRotation ? destination.rotation : target.transform.rotation;
        target.transform.SetPositionAndRotation(destination.position, rotation);

        for (int i = 0; i < controllers.Length; i++)
            controllers[i].enabled = true;

        if (resetPhysics)
            ResetPhysics(target);

        MovementBrain movementBrain = target.GetComponent<MovementBrain>();
        if (movementBrain != null)
        {
            movementBrain.SetVerticalVelocity(0f);
            movementBrain.StopHorizontalMovement();
        }

        PlayerDeathHandler deathHandler = target.GetComponent<PlayerDeathHandler>();
        if (deathHandler != null)
            deathHandler.SetRespawnPoint(destination.position, rotation);
    }

    public static void PlayVfx(GameObject vfxPrefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (vfxPrefab == null)
            return;

        GameObject instanceObject = Instantiate(vfxPrefab, position, rotation);
        instanceObject.SetActive(true);

        ParticleSystem[] particleSystems = instanceObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
            particleSystems[i].Play(true);

        if (lifetime > 0f)
            Destroy(instanceObject, lifetime);
    }

    public static void PlayAudio(
        AudioClip clip,
        Vector3 position,
        float volume,
        float spatialBlend,
        AudioSource preferredSource = null)
    {
        if (clip == null)
            return;

        float finalVolume = Mathf.Clamp01(volume);
        if (preferredSource != null)
        {
            preferredSource.PlayOneShot(clip, finalVolume);
            return;
        }

        GameObject audioObject = new GameObject("Teleport Audio");
        audioObject.transform.position = position;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.minDistance = 1f;
        source.maxDistance = 28f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.PlayOneShot(clip, finalVolume);

        Destroy(audioObject, clip.length + 0.25f);
    }

    private static TeleportSpawnPoint FindSpawnPoint(string spawnId)
    {
        TeleportSpawnPoint[] spawnPoints = FindObjectsByType<TeleportSpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        TeleportSpawnPoint fallback = null;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (fallback == null)
                fallback = spawnPoints[i];

            if (spawnPoints[i].Matches(spawnId))
                return spawnPoints[i];
        }

        return fallback;
    }

    private static void ResetPhysics(GameObject target)
    {
        Rigidbody[] rigidbodies = target.GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].linearVelocity = Vector3.zero;
            rigidbodies[i].angularVelocity = Vector3.zero;
            rigidbodies[i].Sleep();
        }
    }
}
