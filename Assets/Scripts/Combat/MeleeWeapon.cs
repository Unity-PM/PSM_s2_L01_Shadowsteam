using System.Collections.Generic;
using Platformer;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MeleeWeapon : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private bool useKeyInput = true;
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionReference attackAction;
#endif

    [Header("Damage")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private bool useOwnerAttackStat = true;
    [SerializeField] private float attackStatMultiplier = 1f;
    [SerializeField] private bool useCriticalStats = true;
    [SerializeField] private bool applyTargetDefense = true;
    [SerializeField] private float defenseStatMultiplier = 1f;
    [SerializeField] private float minimumDamage = 1f;
    [SerializeField] private float cooldown = 0.35f;
    [SerializeField] private float hitActiveTime = 0.22f;
    [SerializeField] private bool requireEnemyComponent = true;
    [SerializeField] private bool guaranteeNearbyEnemyHit = true;
    [SerializeField] private float guaranteedHitRadius = 2.35f;
    [SerializeField] private float closeRangeOmnidirectionalRadius = 1.15f;
    [SerializeField, Range(0f, 180f)] private float guaranteedHitAngle = 140f;
    [SerializeField] private bool requireFacingForGuaranteedHit;
    [SerializeField] private bool searchEnemyComponentsWhenOverlapMisses = true;
    [SerializeField] private float minimumGuaranteedHitRadius = 2.75f;
    [SerializeField] private float guaranteedHitVerticalTolerance = 2.25f;

    [Header("Hitbox")]
    [SerializeField] private Transform hitOrigin;
    [SerializeField] private Collider hitCollider;
    [SerializeField] private Renderer hitRenderer;
    [SerializeField] private bool useOwnerForwardCapsule = true;
    [SerializeField] private float attackReach = 2.1f;
    [SerializeField] private float attackRadius = 0.75f;
    [SerializeField] private float attackHeightOffset = 1.0f;
    [SerializeField] private float attackStartOffset = 0.25f;
    [SerializeField] private bool useWeaponBounds = true;
    [SerializeField] private bool useWeaponSweep = true;
    [SerializeField] private float weaponSweepRadius = 0.35f;
    [SerializeField] private Vector3 fallbackBoxSize = new Vector3(0.5f, 0.5f, 1.2f);
    [SerializeField] private float boundsPadding = 0.1f;
    [SerializeField] private float forwardOffset = 0f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Owner")]
    [SerializeField] private Transform ownerRoot;
    [SerializeField] private StatComponent ownerStats;

    [Header("Animation")]
    [SerializeField] private DynamicAnimator dynamicAnimator;
    [SerializeField] private string attackAnimationStateId = "Attack";
    [SerializeField] private bool syncCooldownToAttackAnimation = true;
    [SerializeField] private float attackAnimationEndPadding = 0.05f;

    private readonly HashSet<StatComponent> damagedTargets = new HashSet<StatComponent>();
    private float cooldownTimer;
    private float attackInterval;
    private float hitTimer;
    private Vector3 previousWeaponProbePosition;
    private bool hasPreviousWeaponProbePosition;
#if ENABLE_INPUT_SYSTEM
    private bool attackQueued;
#endif

    private void Reset()
    {
        hitOrigin = transform;
        hitCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        hitRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        ownerStats = GetComponentInParent<StatComponent>();
        ownerRoot = ownerStats != null ? ownerStats.transform : transform.root;
    }

    private void Awake()
    {
        if (hitOrigin == null)
            hitOrigin = transform;

        if (hitCollider == null)
            hitCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();

        if (hitRenderer == null)
            hitRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();

        if (ownerStats == null)
            ownerStats = GetComponentInParent<StatComponent>();

        if (ownerRoot == null)
            ownerRoot = ownerStats != null ? ownerStats.transform : transform.root;

        if (dynamicAnimator == null && ownerRoot != null)
            dynamicAnimator = ownerRoot.GetComponent<DynamicAnimator>() ?? ownerRoot.GetComponentInChildren<DynamicAnimator>();

        previousWeaponProbePosition = GetWeaponProbePosition();
        hasPreviousWeaponProbePosition = true;
    }

    private void Start()
    {
        ResolveAttackInterval();
    }

    private void ResolveAttackInterval()
    {
        attackInterval = cooldown;

        if (!syncCooldownToAttackAnimation || dynamicAnimator == null || string.IsNullOrEmpty(attackAnimationStateId))
            return;

        if (dynamicAnimator.TryGetClipLength(attackAnimationStateId, out float clipLength))
            attackInterval = Mathf.Max(cooldown, clipLength - attackAnimationEndPadding);
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (attackAction != null && attackAction.action != null)
        {
            attackAction.action.performed += OnAttackActionPerformed;
            attackAction.action.Enable();
        }
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (attackAction != null && attackAction.action != null)
            attackAction.action.performed -= OnAttackActionPerformed;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void OnAttackActionPerformed(InputAction.CallbackContext _)
    {
        attackQueued = true;
    }
#endif

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (hitTimer > 0f)
        {
            ScanForTargets();
            hitTimer -= Time.deltaTime;
        }
        else
        {
            previousWeaponProbePosition = GetWeaponProbePosition();
            hasPreviousWeaponProbePosition = true;
        }

#if ENABLE_INPUT_SYSTEM
        if (attackQueued)
        {
            attackQueued = false;
            TryAttack();
        }
        else if (useKeyInput && attackAction == null && WasAttackKeyPressedThisFrame())
            TryAttack();
#else
        if (useKeyInput && WasAttackKeyPressedThisFrame())
            TryAttack();
#endif
    }

    private void TryAttack()
    {
        if (!CanAttack())
            return;

        Attack();
    }

    private bool CanAttack()
    {
        if (cooldownTimer > 0f)
            return false;

        if (dynamicAnimator == null || string.IsNullOrEmpty(attackAnimationStateId))
            return true;

        return dynamicAnimator.CanRestartState(attackAnimationStateId);
    }

    public void Attack()
    {
        if (!CanAttack())
            return;

        cooldownTimer = GetEffectiveAttackInterval();
        damagedTargets.Clear();
        hitTimer = Mathf.Max(0.02f, hitActiveTime);

        if (dynamicAnimator != null && !string.IsNullOrEmpty(attackAnimationStateId))
            dynamicAnimator.ForcePlay(attackAnimationStateId);

        previousWeaponProbePosition = GetWeaponProbePosition();
        hasPreviousWeaponProbePosition = true;
        TryDamageBestNearbyEnemy();
        ScanForTargets();
    }

    private void ScanForTargets()
    {
        TryDamageBestNearbyEnemy();

        if (useOwnerForwardCapsule)
            ScanOwnerForwardCapsule();

        if (useWeaponBounds)
            ScanWeaponBounds();

        if (useWeaponSweep)
            ScanWeaponSweep();
    }

    private void ScanOwnerForwardCapsule()
    {
        Transform root = ownerRoot != null ? ownerRoot : transform;
        Vector3 forward = root.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        forward.Normalize();

        Vector3 basePosition = root.position + Vector3.up * attackHeightOffset;
        Vector3 start = basePosition + forward * attackStartOffset;
        Vector3 end = basePosition + forward * Mathf.Max(attackStartOffset, attackReach);

        Collider[] hits = Physics.OverlapCapsule(start, end, attackRadius, targetMask, triggerInteraction);
        for (int i = 0; i < hits.Length; i++)
            TryDamage(hits[i]);
    }

    private void ScanWeaponBounds()
    {
        GetHitBox(out Vector3 center, out Vector3 halfExtents, out Quaternion rotation);
        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, targetMask, triggerInteraction);

        for (int i = 0; i < hits.Length; i++)
            TryDamage(hits[i]);
    }

    private void ScanWeaponSweep()
    {
        Vector3 currentPosition = GetWeaponProbePosition();

        if (!hasPreviousWeaponProbePosition)
        {
            previousWeaponProbePosition = currentPosition;
            hasPreviousWeaponProbePosition = true;
        }

        Collider[] currentHits = Physics.OverlapSphere(currentPosition, weaponSweepRadius, targetMask, triggerInteraction);
        for (int i = 0; i < currentHits.Length; i++)
            TryDamage(currentHits[i]);

        Vector3 delta = currentPosition - previousWeaponProbePosition;
        float distance = delta.magnitude;
        if (distance > 0.001f)
        {
            RaycastHit[] sweepHits = Physics.SphereCastAll(
                previousWeaponProbePosition,
                weaponSweepRadius,
                delta / distance,
                distance,
                targetMask,
                triggerInteraction
            );

            for (int i = 0; i < sweepHits.Length; i++)
                TryDamage(sweepHits[i].collider);
        }

        previousWeaponProbePosition = currentPosition;
    }

    private void TryDamage(Collider hit)
    {
        if (!TryResolveTarget(hit, out StatComponent targetStats))
            return;

        DamageTarget(targetStats);
    }

    private void TryDamageBestNearbyEnemy()
    {
        if (!guaranteeNearbyEnemyHit)
            return;

        Transform root = ownerRoot != null ? ownerRoot : transform;
        Vector3 origin = root.position + Vector3.up * attackHeightOffset;
        float effectiveRadius = GetEffectiveGuaranteedHitRadius();
        Collider[] hits = Physics.OverlapSphere(origin, effectiveRadius, targetMask, triggerInteraction);

        StatComponent bestTarget = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (!TryResolveTarget(hit, out StatComponent targetStats))
                continue;

            TrySelectBetterTarget(targetStats, root, origin, effectiveRadius, ref bestTarget, ref bestScore);
        }

        if (bestTarget == null && searchEnemyComponentsWhenOverlapMisses)
            TryFindBestEnemyByComponent(root, origin, effectiveRadius, ref bestTarget, ref bestScore);

        if (bestTarget != null)
            DamageTarget(bestTarget);
    }

    private void TryFindBestEnemyByComponent(Transform root, Vector3 origin, float effectiveRadius,
        ref StatComponent bestTarget, ref float bestScore)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null)
                continue;
            if (ownerRoot != null && enemy.transform.IsChildOf(ownerRoot))
                continue;

            StatComponent targetStats = enemy.GetComponent<StatComponent>();
            if (targetStats == null || targetStats == ownerStats || targetStats.IsDead)
                continue;

            TrySelectBetterTarget(targetStats, root, origin, effectiveRadius, ref bestTarget, ref bestScore);
        }
    }

    private void TrySelectBetterTarget(StatComponent targetStats, Transform root, Vector3 origin, float effectiveRadius,
        ref StatComponent bestTarget, ref float bestScore)
    {
        if (targetStats == null)
            return;

        Vector3 targetPoint = GetTargetPoint(targetStats.transform, origin);
        Vector3 worldOffset = targetPoint - root.position;
        if (Mathf.Abs(worldOffset.y) > Mathf.Max(0.25f, guaranteedHitVerticalTolerance))
            return;

        Vector3 flatOffset = worldOffset;
        flatOffset.y = 0f;
        float distance = flatOffset.magnitude;
        if (distance > effectiveRadius)
            return;

        bool closeEnough = distance <= GetEffectiveCloseRange(effectiveRadius);
        if (requireFacingForGuaranteedHit && !closeEnough && !IsInsideAttackAngle(root, flatOffset))
            return;

        float score = distance;
        if (requireFacingForGuaranteedHit && !closeEnough && flatOffset.sqrMagnitude > 0.001f)
            score += Vector3.Angle(GetFlatForward(root), flatOffset.normalized) * 0.01f;

        if (score >= bestScore)
            return;

        bestScore = score;
        bestTarget = targetStats;
    }

    private bool TryResolveTarget(Collider hit, out StatComponent targetStats)
    {
        targetStats = null;
        if (hit == null)
            return false;

        if (ownerRoot != null && hit.transform.IsChildOf(ownerRoot))
            return false;

        Enemy enemy = hit.GetComponentInParent<Enemy>();
        if (requireEnemyComponent && enemy == null)
            return false;

        targetStats = hit.GetComponentInParent<StatComponent>();
        if (targetStats == null && enemy != null)
            targetStats = enemy.GetComponent<StatComponent>();
        if (targetStats == null || targetStats == ownerStats || targetStats.IsDead)
            return false;

        return true;
    }

    private void DamageTarget(StatComponent targetStats)
    {
        if (targetStats == null)
            return;

        if (!damagedTargets.Add(targetStats))
            return;

        EventBus.Publish(new StatChangeEvent(targetStats, StatType.HP, -CalculateDamage(targetStats)));

        if (!targetStats.IsDead)
            targetStats.GetComponent<EnemyAudioController>()?.PlayHit();
    }

    private float CalculateDamage(StatComponent targetStats)
    {
        float finalDamage = Mathf.Max(0f, damage);
        StatComponent attackerStats = ResolveOwnerStats();

        if (useOwnerAttackStat && attackerStats != null)
            finalDamage += Mathf.Max(0f, attackerStats.GetEffectiveStat(StatType.ATK)) * Mathf.Max(0f, attackStatMultiplier);

        if (useCriticalStats && attackerStats != null && RollCritical(attackerStats, out float criticalMultiplier))
            finalDamage *= criticalMultiplier;

        if (applyTargetDefense && targetStats != null)
            finalDamage -= Mathf.Max(0f, targetStats.GetEffectiveStat(StatType.DEF)) * Mathf.Max(0f, defenseStatMultiplier);

        return Mathf.Max(minimumDamage, finalDamage);
    }

    private StatComponent ResolveOwnerStats()
    {
        if (ownerStats != null)
            return ownerStats;

        if (ownerRoot != null)
            ownerStats = ownerRoot.GetComponent<StatComponent>() ?? ownerRoot.GetComponentInChildren<StatComponent>();

        if (ownerStats == null)
            ownerStats = GetComponentInParent<StatComponent>();

        return ownerStats;
    }

    private float GetEffectiveAttackInterval()
    {
        StatComponent stats = ResolveOwnerStats();
        float attackSpeedMultiplier = stats != null
            ? stats.GetPercentStatMultiplier(StatType.AS)
            : 1f;

        return attackInterval / Mathf.Max(0.01f, attackSpeedMultiplier);
    }

    private static bool RollCritical(StatComponent attackerStats, out float multiplier)
    {
        multiplier = 1f;
        if (attackerStats == null)
            return false;

        float chance = Mathf.Clamp(attackerStats.GetEffectiveStat(StatType.CritChance), 0f, 100f);
        if (chance <= 0f || Random.value * 100f > chance)
            return false;

        multiplier = Mathf.Max(1f, attackerStats.GetEffectiveStat(StatType.CritDamage) / 100f);
        return multiplier > 1f;
    }

    private bool IsInsideAttackAngle(Transform root, Vector3 flatOffset)
    {
        if (flatOffset.sqrMagnitude < 0.001f)
            return true;

        Vector3 forward = GetFlatForward(root);
        if (forward.sqrMagnitude < 0.001f)
            return true;

        return Vector3.Angle(forward, flatOffset.normalized) <= guaranteedHitAngle * 0.5f;
    }

    private float GetEffectiveGuaranteedHitRadius()
    {
        return Mathf.Max(
            guaranteedHitRadius,
            minimumGuaranteedHitRadius,
            attackReach + Mathf.Max(0.25f, attackRadius)
        );
    }

    private float GetEffectiveCloseRange(float effectiveRadius)
    {
        if (!requireFacingForGuaranteedHit)
            return effectiveRadius;

        return Mathf.Min(effectiveRadius, Mathf.Max(closeRangeOmnidirectionalRadius, 1.6f));
    }

    private static Vector3 GetTargetPoint(Transform targetRoot, Vector3 fallback)
    {
        if (targetRoot == null)
            return fallback;

        Collider[] colliders = targetRoot.GetComponentsInChildren<Collider>();
        Vector3 bestPoint = targetRoot.position;
        float bestDistance = float.PositiveInfinity;
        bool foundSolidCollider = false;

        for (int pass = 0; pass < 2; pass++)
        {
            bool allowTriggers = pass == 1;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                if (targetCollider == null || !targetCollider.enabled)
                    continue;
                if (!allowTriggers && targetCollider.isTrigger)
                    continue;
                if (foundSolidCollider && targetCollider.isTrigger)
                    continue;

                Vector3 point = targetCollider.ClosestPoint(fallback);
                float distance = (point - fallback).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestPoint = point;
                foundSolidCollider = !targetCollider.isTrigger;
            }

            if (foundSolidCollider || bestDistance < float.PositiveInfinity)
                return bestPoint;
        }

        return targetRoot.position;
    }

    private Vector3 GetFlatForward(Transform root)
    {
        Vector3 forward = root != null ? root.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.001f)
            return Vector3.forward;

        return forward.normalized;
    }

    private void GetHitBox(out Vector3 center, out Vector3 halfExtents, out Quaternion rotation)
    {
        Vector3 offset = hitOrigin != null ? hitOrigin.forward * forwardOffset : transform.forward * forwardOffset;

        if (hitCollider is BoxCollider box)
        {
            center = box.transform.TransformPoint(box.center) + offset;
            halfExtents = Vector3.Scale(box.size, Abs(box.transform.lossyScale)) * 0.5f;
            halfExtents += Vector3.one * boundsPadding;
            rotation = box.transform.rotation;
            return;
        }

        if (hitCollider != null)
        {
            Bounds b = hitCollider.bounds;
            center = b.center + offset;
            halfExtents = b.extents + Vector3.one * boundsPadding;
            rotation = Quaternion.identity;
            return;
        }

        if (hitRenderer != null)
        {
            Bounds b = hitRenderer.bounds;
            center = b.center + offset;
            halfExtents = b.extents + Vector3.one * boundsPadding;
            rotation = Quaternion.identity;
            return;
        }

        Transform origin = hitOrigin != null ? hitOrigin : transform;
        center = origin.position + origin.forward * ((fallbackBoxSize.z * 0.5f) + forwardOffset);
        halfExtents = Vector3.Max(fallbackBoxSize * 0.5f, Vector3.one * 0.01f);
        rotation = origin.rotation;
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private Vector3 GetWeaponProbePosition()
    {
        if (hitCollider != null)
            return hitCollider.bounds.center;

        if (hitRenderer != null)
            return hitRenderer.bounds.center;

        Transform origin = hitOrigin != null ? hitOrigin : transform;
        return origin.position;
    }

    private bool WasAttackKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (WasNewInputKeyPressedThisFrame(attackKey))
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(attackKey);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool WasNewInputKeyPressedThisFrame(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Mouse0:
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            case KeyCode.Mouse1:
                return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            case KeyCode.Mouse2:
                return Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
        }

        if (Keyboard.current == null || !TryConvertKeyCode(keyCode, out Key key))
            return false;

        return Keyboard.current[key].wasPressedThisFrame;
    }

    private static bool TryConvertKeyCode(KeyCode keyCode, out Key key)
    {
        key = keyCode switch
        {
            KeyCode.Space => Key.Space,
            KeyCode.Return => Key.Enter,
            KeyCode.KeypadEnter => Key.NumpadEnter,
            KeyCode.Escape => Key.Escape,
            KeyCode.Tab => Key.Tab,
            KeyCode.Backspace => Key.Backspace,
            KeyCode.LeftShift => Key.LeftShift,
            KeyCode.RightShift => Key.RightShift,
            KeyCode.LeftControl => Key.LeftCtrl,
            KeyCode.RightControl => Key.RightCtrl,
            KeyCode.LeftAlt => Key.LeftAlt,
            KeyCode.RightAlt => Key.RightAlt,
            KeyCode.A => Key.A,
            KeyCode.B => Key.B,
            KeyCode.C => Key.C,
            KeyCode.D => Key.D,
            KeyCode.E => Key.E,
            KeyCode.F => Key.F,
            KeyCode.G => Key.G,
            KeyCode.H => Key.H,
            KeyCode.I => Key.I,
            KeyCode.J => Key.J,
            KeyCode.K => Key.K,
            KeyCode.L => Key.L,
            KeyCode.M => Key.M,
            KeyCode.N => Key.N,
            KeyCode.O => Key.O,
            KeyCode.P => Key.P,
            KeyCode.Q => Key.Q,
            KeyCode.R => Key.R,
            KeyCode.S => Key.S,
            KeyCode.T => Key.T,
            KeyCode.U => Key.U,
            KeyCode.V => Key.V,
            KeyCode.W => Key.W,
            KeyCode.X => Key.X,
            KeyCode.Y => Key.Y,
            KeyCode.Z => Key.Z,
            KeyCode.Alpha0 => Key.Digit0,
            KeyCode.Alpha1 => Key.Digit1,
            KeyCode.Alpha2 => Key.Digit2,
            KeyCode.Alpha3 => Key.Digit3,
            KeyCode.Alpha4 => Key.Digit4,
            KeyCode.Alpha5 => Key.Digit5,
            KeyCode.Alpha6 => Key.Digit6,
            KeyCode.Alpha7 => Key.Digit7,
            KeyCode.Alpha8 => Key.Digit8,
            KeyCode.Alpha9 => Key.Digit9,
            KeyCode.Keypad0 => Key.Numpad0,
            KeyCode.Keypad1 => Key.Numpad1,
            KeyCode.Keypad2 => Key.Numpad2,
            KeyCode.Keypad3 => Key.Numpad3,
            KeyCode.Keypad4 => Key.Numpad4,
            KeyCode.Keypad5 => Key.Numpad5,
            KeyCode.Keypad6 => Key.Numpad6,
            KeyCode.Keypad7 => Key.Numpad7,
            KeyCode.Keypad8 => Key.Numpad8,
            KeyCode.Keypad9 => Key.Numpad9,
            _ => Key.None
        };

        return key != Key.None;
    }
#endif

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform origin = hitOrigin != null ? hitOrigin : transform;
        if (origin == null)
            return;

        if (useOwnerForwardCapsule)
            DrawOwnerForwardCapsuleGizmo();

        if (guaranteeNearbyEnemyHit)
        {
            Transform root = ownerRoot != null ? ownerRoot : transform;
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.45f);
            Gizmos.DrawWireSphere(root.position + Vector3.up * attackHeightOffset, GetEffectiveGuaranteedHitRadius());
        }

        if (useWeaponBounds)
        {
            GetHitBox(out Vector3 center, out Vector3 halfExtents, out Quaternion rotation);

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            Gizmos.color = new Color(1f, 0.15f, 0.05f, 0.65f);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
            Gizmos.matrix = previousMatrix;
        }

        if (useWeaponSweep)
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.65f);
            Gizmos.DrawWireSphere(GetWeaponProbePosition(), weaponSweepRadius);
        }
    }

    private void DrawOwnerForwardCapsuleGizmo()
    {
        Transform root = ownerRoot != null ? ownerRoot : transform;
        Vector3 forward = root.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        forward.Normalize();

        Vector3 basePosition = root.position + Vector3.up * attackHeightOffset;
        Vector3 start = basePosition + forward * attackStartOffset;
        Vector3 end = basePosition + forward * Mathf.Max(attackStartOffset, attackReach);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized * attackRadius;

        Gizmos.color = new Color(1f, 0.45f, 0f, 0.65f);
        Gizmos.DrawWireSphere(start, attackRadius);
        Gizmos.DrawWireSphere(end, attackRadius);
        Gizmos.DrawLine(start + right, end + right);
        Gizmos.DrawLine(start - right, end - right);
    }
#endif
}
