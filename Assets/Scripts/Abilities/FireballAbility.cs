using UnityEngine;
[CreateAssetMenu(fileName = "FireballAbility", menuName = "Scriptable Objects/FireballAbility")]
public class FireballAbility : AbilitySO
{
    public GameObject fireball;
    public float damage;
    public float speed;

    public override void Execute(StatComponent caster, Transform castPoint)
    {
        GameObject fb = Instantiate(
            fireball,
            castPoint.position,
            castPoint.rotation
        );

        FireballProjectile proj = fb.GetComponent<FireballProjectile>();
        proj.Init(caster, damage, speed);
    }
}
