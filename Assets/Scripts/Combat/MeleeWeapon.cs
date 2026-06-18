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
    [SerializeField] private float cooldown = 0.35f;
    [SerializeField] private float hitActiveTime = 0.22f;
    [SerializeField] private bool requireEnemyComponent = true;

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

        cooldownTimer = attackInterval;
        damagedTargets.Clear();
        hitTimer = Mathf.Max(0.02f, hitActiveTime);

        if (dynamicAnimator != null && !string.IsNullOrEmpty(attackAnimationStateId))
            dynamicAnimator.ForcePlay(attackAnimationStateId);

        previousWeaponProbePosition = GetWeaponProbePosition();
        hasPreviousWeaponProbePosition = true;
        ScanForTargets();
    }

    private void ScanForTargets()
    {
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
        if (hit == null)
            return;

        if (ownerRoot != null && hit.transform.IsChildOf(ownerRoot))
            return;

        if (requireEnemyComponent && hit.GetComponentInParent<Enemy>() == null)
            return;

        StatComponent targetStats = hit.GetComponentInParent<StatComponent>();
        if (targetStats == null || targetStats == ownerStats || targetStats.IsDead)
            return;

        if (!damagedTargets.Add(targetStats))
            return;

        EventBus.Publish(new StatChangeEvent(targetStats, StatType.HP, -damage));
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
