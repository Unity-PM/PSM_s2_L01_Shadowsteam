using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[AddComponentMenu("Stats/Stat Contact Drain")]
public class StatContactDrain : MonoBehaviour
{
    public enum DrainStat
    {
        HP,
        MP,
        Stamina
    }

    public enum DrainRateMode
    {
        PerSecond,
        PerTick
    }

    [Serializable]
    public class StatDrainRule
    {
        public bool enabled = true;
        public DrainStat stat = DrainStat.HP;
        [Min(0f)] public float amount = 5f;
        public DrainRateMode rateMode = DrainRateMode.PerSecond;
        [Min(0.02f)] public float tickInterval = 1f;
        [Min(0f)] public float drainAfterExitDuration;
        public GameObject afterExitVfxPrefab;
        public bool parentAfterExitVfxToTarget = true;
        public Vector3 afterExitVfxOffset;

        public StatDrainRule()
        {
        }

        public StatDrainRule(DrainStat stat, float amount, DrainRateMode rateMode, float tickInterval)
        {
            this.stat = stat;
            this.amount = amount;
            this.rateMode = rateMode;
            this.tickInterval = tickInterval;
        }
    }

    [SerializeField, HideInInspector] protected StatType statType = StatType.HP;
    [SerializeField, HideInInspector] private bool legacySettingsMigrated;
    [SerializeField, HideInInspector] private bool perRuleAfterExitSettingsMigrated;
    [SerializeField, HideInInspector] private float drainAfterExitDuration;
    [SerializeField, HideInInspector] private GameObject afterExitVfxPrefab;
    [SerializeField, HideInInspector] private bool parentAfterExitVfxToTarget = true;
    [SerializeField, HideInInspector] private Vector3 afterExitVfxOffset;

    [Header("Target")]
    [SerializeField] private bool searchInParents = true;
    [SerializeField] private bool requireTag;
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private bool affectDeadTargets;

    [Header("Contact")]
    [SerializeField] private bool useTriggerContacts = true;
    [SerializeField] private bool useCollisionContacts = true;
    [SerializeField, Min(0.05f)] private float staleContactTimeout = 0.25f;

    [Header("Drain Rules")]
    [SerializeField] private List<StatDrainRule> drainRules = new List<StatDrainRule>
    {
        new StatDrainRule()
    };

    private const float ContactValidationInterval = 0.1f;

    private readonly Dictionary<StatComponent, Dictionary<Collider, float>> contactColliders = new Dictionary<StatComponent, Dictionary<Collider, float>>();
    private readonly Dictionary<StatComponent, float[]> drainAfterExitUntilTimes = new Dictionary<StatComponent, float[]>();
    private readonly Dictionary<StatComponent, GameObject[]> afterExitVfxInstances = new Dictionary<StatComponent, GameObject[]>();
    private readonly Dictionary<StatComponent, float[]> nextTickTimes = new Dictionary<StatComponent, float[]>();
    private readonly List<StatComponent> cleanupBuffer = new List<StatComponent>();
    private readonly List<Collider> staleColliderBuffer = new List<Collider>();
    private float nextContactValidationTime;

    private void Reset()
    {
        EnsureLegacySettingsMigrated();
        MigratePerRuleAfterExitSettings();

        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider != null)
            ownCollider.isTrigger = true;
    }

    private void Awake()
    {
        EnsureLegacySettingsMigrated();
        MigratePerRuleAfterExitSettings();
        ValidateRules();
    }

    private void OnValidate()
    {
        EnsureLegacySettingsMigrated();
        MigratePerRuleAfterExitSettings();
        ValidateRules();
    }

    private void Update()
    {
        if (drainRules == null || drainRules.Count == 0)
            return;

        if (contactColliders.Count == 0 && drainAfterExitUntilTimes.Count == 0)
            return;

        ValidateActiveContactsIfNeeded();
        ApplyContactDrains();
        ApplyAfterExitDrains();
    }

    private void OnDisable()
    {
        contactColliders.Clear();
        drainAfterExitUntilTimes.Clear();
        nextTickTimes.Clear();

        foreach (KeyValuePair<StatComponent, GameObject[]> pair in afterExitVfxInstances)
        {
            GameObject[] instances = pair.Value;
            if (instances == null)
                continue;

            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i] != null)
                    DestroyVfx(instances[i]);
            }
        }

        afterExitVfxInstances.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggerContacts)
            return;

        RegisterContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!useTriggerContacts)
            return;

        UnregisterContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!useTriggerContacts)
            return;

        EnsureContactRegistered(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!useCollisionContacts || collision == null)
            return;

        RegisterContact(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!useCollisionContacts || collision == null)
            return;

        UnregisterContact(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!useCollisionContacts || collision == null)
            return;

        EnsureContactRegistered(collision.collider);
    }

    private void ApplyContactDrains()
    {
        if (contactColliders.Count == 0)
            return;

        cleanupBuffer.Clear();

        foreach (KeyValuePair<StatComponent, Dictionary<Collider, float>> contact in contactColliders)
        {
            StatComponent targetStats = contact.Key;
            if (targetStats == null || contact.Value == null || contact.Value.Count == 0 || (!affectDeadTargets && targetStats.IsDead))
            {
                cleanupBuffer.Add(targetStats);
                continue;
            }

            ApplyDrain(targetStats);
        }

        for (int i = 0; i < cleanupBuffer.Count; i++)
        {
            StatComponent targetStats = cleanupBuffer[i];
            contactColliders.Remove(targetStats);

            if (targetStats == null || !affectDeadTargets && targetStats.IsDead)
                StopAfterExitDrain(targetStats);
        }
    }

    private void ApplyAfterExitDrains()
    {
        if (drainAfterExitUntilTimes.Count == 0)
            return;

        cleanupBuffer.Clear();

        foreach (KeyValuePair<StatComponent, float[]> activeDrain in drainAfterExitUntilTimes)
        {
            StatComponent targetStats = activeDrain.Key;
            float[] ruleUntilTimes = activeDrain.Value;

            if (targetStats == null
                || contactColliders.ContainsKey(targetStats)
                || ruleUntilTimes == null
                || ruleUntilTimes.Length == 0
                || (!affectDeadTargets && targetStats.IsDead))
            {
                cleanupBuffer.Add(targetStats);
                continue;
            }

            bool hasActiveRule = false;

            for (int i = 0; i < ruleUntilTimes.Length; i++)
            {
                if (i >= drainRules.Count || ruleUntilTimes[i] <= 0f)
                    continue;

                if (Time.time >= ruleUntilTimes[i])
                {
                    StopAfterExitDrainRule(targetStats, i);
                    continue;
                }

                StatDrainRule rule = drainRules[i];
                if (rule == null || !rule.enabled || rule.amount <= 0f)
                    continue;

                ApplyDrainRule(targetStats, rule, i);
                hasActiveRule = true;
            }

            if (!hasActiveRule && !HasAnyActiveAfterExitRule(ruleUntilTimes))
                cleanupBuffer.Add(targetStats);
        }

        for (int i = 0; i < cleanupBuffer.Count; i++)
            StopAfterExitDrain(cleanupBuffer[i]);
    }

    private void RegisterContact(Collider other)
    {
        if (!TryResolveTarget(other, false, out StatComponent targetStats))
            return;

        StopAfterExitDrain(targetStats);

        if (!contactColliders.TryGetValue(targetStats, out Dictionary<Collider, float> colliders))
        {
            colliders = new Dictionary<Collider, float>();
            contactColliders.Add(targetStats, colliders);
        }

        colliders[other] = Time.time;
    }

    private void UnregisterContact(Collider other)
    {
        if (!TryResolveTarget(other, true, out StatComponent targetStats))
            return;

        if (!contactColliders.TryGetValue(targetStats, out Dictionary<Collider, float> colliders))
            return;

        colliders.Remove(other);

        if (colliders.Count > 0)
            return;

        contactColliders.Remove(targetStats);
        StartAfterExitDrain(targetStats);
    }

    private void EnsureContactRegistered(Collider other)
    {
        if (!TryResolveTarget(other, false, out StatComponent targetStats))
            return;

        StopAfterExitDrain(targetStats);

        if (contactColliders.TryGetValue(targetStats, out Dictionary<Collider, float> colliders))
        {
            colliders[other] = Time.time;
            return;
        }

        RegisterContact(other);
    }

    private void ValidateActiveContactsIfNeeded()
    {
        if (Time.time < nextContactValidationTime)
            return;

        nextContactValidationTime = Time.time + ContactValidationInterval;
        cleanupBuffer.Clear();

        foreach (KeyValuePair<StatComponent, Dictionary<Collider, float>> contact in contactColliders)
        {
            StatComponent targetStats = contact.Key;
            Dictionary<Collider, float> colliders = contact.Value;

            if (targetStats == null || colliders == null)
            {
                cleanupBuffer.Add(targetStats);
                continue;
            }

            RemoveStaleContacts(colliders);

            if (colliders.Count == 0)
                cleanupBuffer.Add(targetStats);
        }

        for (int i = 0; i < cleanupBuffer.Count; i++)
        {
            StatComponent targetStats = cleanupBuffer[i];
            contactColliders.Remove(targetStats);
            StartAfterExitDrain(targetStats);
        }
    }

    private static bool IsColliderValid(Collider collider)
    {
        return collider != null && collider.enabled && collider.gameObject.activeInHierarchy;
    }

    private void RemoveStaleContacts(Dictionary<Collider, float> colliders)
    {
        float staleBeforeTime = Time.time - staleContactTimeout;
        staleColliderBuffer.Clear();

        foreach (KeyValuePair<Collider, float> contact in colliders)
        {
            if (!IsColliderValid(contact.Key) || contact.Value < staleBeforeTime)
                staleColliderBuffer.Add(contact.Key);
        }

        for (int i = 0; i < staleColliderBuffer.Count; i++)
            colliders.Remove(staleColliderBuffer[i]);
    }

    private void StartAfterExitDrain(StatComponent targetStats)
    {
        if (targetStats == null)
            return;

        float[] ruleUntilTimes = null;
        GameObject[] vfxInstances = null;

        for (int i = 0; i < drainRules.Count; i++)
        {
            StatDrainRule rule = drainRules[i];
            if (rule == null || !rule.enabled || rule.amount <= 0f || rule.drainAfterExitDuration <= 0f)
                continue;

            if (ruleUntilTimes == null)
                ruleUntilTimes = new float[drainRules.Count];

            ruleUntilTimes[i] = Time.time + rule.drainAfterExitDuration;

            if (rule.afterExitVfxPrefab == null)
                continue;

            if (vfxInstances == null)
                vfxInstances = new GameObject[drainRules.Count];

            Transform targetTransform = targetStats.transform;
            GameObject vfxInstance = Instantiate(
                rule.afterExitVfxPrefab,
                targetTransform.position + rule.afterExitVfxOffset,
                targetTransform.rotation
            );

            if (rule.parentAfterExitVfxToTarget)
                vfxInstance.transform.SetParent(targetTransform, true);

            PlayVfx(vfxInstance);
            vfxInstances[i] = vfxInstance;
        }

        if (ruleUntilTimes == null)
        {
            nextTickTimes.Remove(targetStats);
            return;
        }

        drainAfterExitUntilTimes[targetStats] = ruleUntilTimes;

        if (vfxInstances != null)
            afterExitVfxInstances[targetStats] = vfxInstances;
    }

    private void StopAfterExitDrain(StatComponent targetStats)
    {
        if (targetStats == null)
            return;

        drainAfterExitUntilTimes.Remove(targetStats);
        nextTickTimes.Remove(targetStats);

        if (!afterExitVfxInstances.TryGetValue(targetStats, out GameObject[] vfxInstances))
            return;

        afterExitVfxInstances.Remove(targetStats);

        if (vfxInstances == null)
            return;

        for (int i = 0; i < vfxInstances.Length; i++)
        {
            if (vfxInstances[i] != null)
                DestroyVfx(vfxInstances[i]);
        }
    }

    private void StopAfterExitDrainRule(StatComponent targetStats, int ruleIndex)
    {
        if (targetStats == null)
            return;

        if (drainAfterExitUntilTimes.TryGetValue(targetStats, out float[] ruleUntilTimes)
            && ruleUntilTimes != null
            && ruleIndex >= 0
            && ruleIndex < ruleUntilTimes.Length)
        {
            ruleUntilTimes[ruleIndex] = 0f;
        }

        if (!afterExitVfxInstances.TryGetValue(targetStats, out GameObject[] vfxInstances)
            || vfxInstances == null
            || ruleIndex < 0
            || ruleIndex >= vfxInstances.Length
            || vfxInstances[ruleIndex] == null)
        {
            return;
        }

        DestroyVfx(vfxInstances[ruleIndex]);
        vfxInstances[ruleIndex] = null;
    }

    private static void DestroyVfx(GameObject vfxInstance)
    {
        if (vfxInstance == null)
            return;

        if (Application.isPlaying)
            Destroy(vfxInstance);
        else
            DestroyImmediate(vfxInstance);
    }

    private static void PlayVfx(GameObject vfxInstance)
    {
        if (vfxInstance == null)
            return;

        vfxInstance.SetActive(true);

        ParticleSystem[] particles = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
            particles[i].Play(true);

        Component[] components = vfxInstance.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null || component.GetType().FullName != "UnityEngine.VFX.VisualEffect")
                continue;

            component.GetType().GetMethod("Play", Type.EmptyTypes)?.Invoke(component, null);
        }
    }

    private static bool HasAnyActiveAfterExitRule(float[] ruleUntilTimes)
    {
        if (ruleUntilTimes == null)
            return false;

        for (int i = 0; i < ruleUntilTimes.Length; i++)
        {
            if (ruleUntilTimes[i] > 0f)
                return true;
        }

        return false;
    }

    private bool TryResolveTarget(Collider other, bool ignoreDeadState, out StatComponent targetStats)
    {
        targetStats = null;
        if (other == null)
            return false;

        if (requireTag && !MatchesRequiredTag(other.transform))
            return false;

        targetStats = searchInParents
            ? other.GetComponentInParent<StatComponent>()
            : other.GetComponent<StatComponent>();

        if (targetStats == null)
            return false;

        if (!ignoreDeadState && !affectDeadTargets && targetStats.IsDead)
            return false;

        return true;
    }

    private bool MatchesRequiredTag(Transform target)
    {
        if (string.IsNullOrEmpty(requiredTag))
            return true;

        if (!searchInParents)
            return target.gameObject.tag == requiredTag;

        Transform current = target;
        while (current != null)
        {
            if (current.gameObject.tag == requiredTag)
                return true;

            current = current.parent;
        }

        return false;
    }

    private void ApplyDrain(StatComponent targetStats)
    {
        for (int i = 0; i < drainRules.Count; i++)
        {
            StatDrainRule rule = drainRules[i];
            if (rule == null || !rule.enabled || rule.amount <= 0f)
                continue;

            ApplyDrainRule(targetStats, rule, i);
        }
    }

    private void ApplyDrainRule(StatComponent targetStats, StatDrainRule rule, int ruleIndex)
    {
        if (rule.rateMode == DrainRateMode.PerSecond)
        {
            PublishDrain(targetStats, rule.stat, rule.amount * Time.deltaTime);
            return;
        }

        float[] timers = GetTickTimers(targetStats);
        if (ruleIndex < 0 || ruleIndex >= timers.Length || Time.time < timers[ruleIndex])
            return;

        PublishDrain(targetStats, rule.stat, rule.amount);
        timers[ruleIndex] = Time.time + Mathf.Max(0.02f, rule.tickInterval);
    }

    private float[] GetTickTimers(StatComponent targetStats)
    {
        if (nextTickTimes.TryGetValue(targetStats, out float[] timers) && timers.Length == drainRules.Count)
            return timers;

        timers = new float[drainRules.Count];
        nextTickTimes[targetStats] = timers;
        return timers;
    }

    private static void PublishDrain(StatComponent targetStats, DrainStat stat, float positiveAmount)
    {
        if (targetStats == null || positiveAmount <= 0f)
            return;

        EventBus.Publish(new StatChangeEvent(targetStats, ToStatType(stat), -positiveAmount));
    }

    private static StatType ToStatType(DrainStat stat)
    {
        return stat switch
        {
            DrainStat.MP => StatType.MP,
            DrainStat.Stamina => StatType.Stamina,
            _ => StatType.HP
        };
    }

    private static DrainStat ToDrainStat(StatType stat)
    {
        return stat switch
        {
            StatType.MP => DrainStat.MP,
            StatType.Stamina => DrainStat.Stamina,
            _ => DrainStat.HP
        };
    }

    private void EnsureLegacySettingsMigrated()
    {
        if (legacySettingsMigrated)
            return;

        drainRules = new List<StatDrainRule>
        {
            new StatDrainRule(ToDrainStat(statType), 5f, DrainRateMode.PerSecond, 1f)
        };

        legacySettingsMigrated = true;
    }

    private void MigratePerRuleAfterExitSettings()
    {
        if (perRuleAfterExitSettingsMigrated)
            return;

        if (drainRules == null)
            drainRules = new List<StatDrainRule>();

        if (drainAfterExitDuration > 0f || afterExitVfxPrefab != null)
        {
            for (int i = 0; i < drainRules.Count; i++)
            {
                StatDrainRule rule = drainRules[i];
                if (rule == null)
                    continue;

                rule.drainAfterExitDuration = drainAfterExitDuration;
                rule.afterExitVfxPrefab = afterExitVfxPrefab;
                rule.parentAfterExitVfxToTarget = parentAfterExitVfxToTarget;
                rule.afterExitVfxOffset = afterExitVfxOffset;
            }
        }

        perRuleAfterExitSettingsMigrated = true;
    }

    private void ValidateRules()
    {
        if (drainRules == null)
            drainRules = new List<StatDrainRule>();

        for (int i = 0; i < drainRules.Count; i++)
        {
            StatDrainRule rule = drainRules[i];
            if (rule == null)
                continue;

            rule.amount = Mathf.Max(0f, rule.amount);
            rule.tickInterval = Mathf.Max(0.02f, rule.tickInterval);
            rule.drainAfterExitDuration = Mathf.Max(0f, rule.drainAfterExitDuration);
        }
    }
}
