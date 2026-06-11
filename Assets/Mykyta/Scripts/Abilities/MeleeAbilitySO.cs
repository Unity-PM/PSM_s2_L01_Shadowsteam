using UnityEngine;

public abstract class MeleeAbilitySO : AbilitySO
{
    public float range;
    public float damage;
    public float arcAngle;
    public float slashTiltAngle = 0f;
    [Tooltip("Lock movement while performing the ability")]
    public bool lockMovement = false;
}