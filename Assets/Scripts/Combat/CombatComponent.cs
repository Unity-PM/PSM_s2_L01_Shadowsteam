using System.Collections.Generic;
using Platformer;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Ближняя атака: ищет коллайдеры с <see cref="Enemy"/> в сфере перед персонажем и наносит урон через <see cref="StatChangeEvent"/>.
/// </summary>
public class CombatComponent : MonoBehaviour {
    [Header("Damage")]
    [SerializeField] float attackDamage = 10f;
    [SerializeField] float attackCooldown = 0.45f;

    [Header("Hit volume (in front of attack origin)")]
    [SerializeField] float attackReach = 1.2f;
    [SerializeField] float attackRadius = 0.65f;
    [SerializeField] Transform attackOrigin;
    [SerializeField] StatComponent attackerStats;
    [SerializeField] PlayerAudioController playerAudio;

#if ENABLE_INPUT_SYSTEM
    [Header("Optional: тот же Input Action, что удара в DynamicAnimator")]
    [SerializeField] InputActionReference meleeAttackAction;
#endif

    float cooldownTimer;

    void Awake() {
        if (attackerStats == null)
            attackerStats = GetComponent<StatComponent>();

        if (attackerStats == null)
            Debug.LogError("CombatComponent requires StatComponent on the same GameObject.", this);

        if (attackOrigin == null)
            attackOrigin = transform;

        if (playerAudio == null)
            playerAudio = GetComponent<PlayerAudioController>();
    }

    void OnEnable() {
#if ENABLE_INPUT_SYSTEM
        if (meleeAttackAction != null && meleeAttackAction.action != null) {
            meleeAttackAction.action.performed += OnMeleePerformed;
            meleeAttackAction.action.Enable();
        }
#endif
    }

    void OnDisable() {
#if ENABLE_INPUT_SYSTEM
        if (meleeAttackAction != null && meleeAttackAction.action != null)
            meleeAttackAction.action.performed -= OnMeleePerformed;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    void OnMeleePerformed(InputAction.CallbackContext _) {
        Attack();
    }
#endif

    void Update() {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    /// <summary>Можно повесить как Animation Event «Attack» или оставить только привязку ввода выше.</summary>
    public void Attack() {
        if (cooldownTimer > 0f || attackerStats == null)
            return;

        cooldownTimer = attackCooldown;
        playerAudio?.PlayAttackSwing();

        Vector3 forward = attackOrigin.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) {
            forward = transform.forward;
            forward.y = 0f;
        }

        forward.Normalize();
        Vector3 sphereCenter = attackOrigin.position + forward * attackReach;

        Collider[] hits = Physics.OverlapSphere(sphereCenter, attackRadius, ~0, QueryTriggerInteraction.Ignore);
        var dealt = new HashSet<StatComponent>();
        bool hitEnemy = false;

        foreach (Collider col in hits) {
            if (col == null)
                continue;

            if (col.GetComponentInParent<Enemy>() == null)
                continue;

            StatComponent targetStats = col.GetComponentInParent<StatComponent>();
            if (targetStats == null || targetStats == attackerStats || !dealt.Add(targetStats))
                continue;

            EventBus.Publish(new StatChangeEvent(targetStats, StatType.HP, -attackDamage));
            hitEnemy = true;
        }

        if (hitEnemy)
            playerAudio?.PlayAttackHit();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected() {
        Transform o = attackOrigin != null ? attackOrigin : transform;
        Vector3 forward = o.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = o.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawWireSphere(o.position + forward * attackReach, attackRadius);
    }
#endif
}
