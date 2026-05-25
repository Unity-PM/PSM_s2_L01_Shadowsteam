using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    private StatComponent caster;
    private float damage;
    private float speed;

    public void Init(StatComponent caster, float damage, float speed)
    {
        this.caster = caster;
        this.damage = damage;
        this.speed = speed;
        Destroy(gameObject, 3f);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        StatComponent target = other.GetComponentInParent<StatComponent>();
        if (target == null || target == caster) return;

        EventBus.Publish(new StatChangeEvent(
            target,
            StatType.HP,
            -damage
        ));

        Destroy(gameObject);
    }
}
