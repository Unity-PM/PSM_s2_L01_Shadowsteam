using UnityEngine;

[CreateAssetMenu(fileName = "FireballAbility", menuName = "Scriptable Objects/Abilities/Fireball")]
public class FireballAbility : RangedAbilitySO
{
    public override void Execute(StatComponent caster, Transform castPoint)
    {
        if (projectilePrefab == null || castPoint == null)
            return;

        GameObject fb = Instantiate(projectilePrefab, castPoint.position, castPoint.rotation);
        if (fb.TryGetComponent(out FireballProjectile proj)) proj.Init(caster, damage, speed);
    }
}