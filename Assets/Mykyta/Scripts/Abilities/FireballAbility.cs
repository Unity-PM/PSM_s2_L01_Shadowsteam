using UnityEngine;

[CreateAssetMenu(fileName = "FireballAbility", menuName = "Scriptable Objects/Abilities/Fireball")]
public class FireballAbility : RangedAbilitySO
{
    public override void Execute(StatComponent caster, Transform castPoint, MovementBrain brain)
    {
        // Спавн проджектайла
        GameObject fb = Instantiate(projectilePrefab, castPoint.position, castPoint.rotation);
        if (fb.TryGetComponent(out FireballProjectile proj)) proj.Init(caster, damage, speed);

        // Эффект на игрока: стоп на 0.2с и откат назад
        brain.LockMovement(0.2f);
        brain.ApplyImpulse(-brain.transform.forward * 5f);
    }
}