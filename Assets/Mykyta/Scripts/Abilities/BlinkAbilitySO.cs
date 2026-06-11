using UnityEngine;

[CreateAssetMenu(fileName = "BlinkAbility", menuName = "Scriptable Objects/Abilities/Blink")]
public class BlinkAbility : RangedAbilitySO
{
    public override void Execute(StatComponent caster, Transform castPoint, MovementBrain brain)
    {
        // 1. Блокируем управление на 0.3с
        brain.LockMovement(0.3f);

        // 2. Телепортируем вперед на 4 метра (куда смотрит камера)
        Vector3 blinkDir = brain.cameraTransform.forward;
        blinkDir.y = 0;
        brain.Teleport(blinkDir.normalized * 4f);

        // 3. Можно добавить импульс вверх для эффекта "прыжка"
        brain.Launch(5f);
    }
}