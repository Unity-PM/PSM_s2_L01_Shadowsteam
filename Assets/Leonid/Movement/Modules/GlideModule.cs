using UnityEngine;

public class GlideModule : MovementModule
{
    public override MovementState State => MovementState.Gliding;
    private bool _isGliding;

    public override bool CanEnter(MovementBrain brain)
    {
        // 1. Если коснулись земли — глайд выключается
        if (brain.Controller.isGrounded) { _isGliding = false; return false; }

        // 2. Если стамина кончилась — падаем
        if (brain.GetComponent<StatComponent>().getStamina() <= 0) { _isGliding = false; return false; }

        // 3. Переключение режима на пробел (JumpDown)
        if (brain.Input.IsJumpDown) _isGliding = !_isGliding;

        return _isGliding;
    }

    public override void Process(MovementBrain brain)
    {
        // Направление вперед (игнорируем наклон камеры вверх/вниз)
        Vector3 forwardDir = brain.cameraTransform.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();

        brain.RotateTowards(forwardDir, rotationSpeed);

        // KISS: Считаем итоговый вектор (Вперед * Скорость + Вверх * СкоростьСпуска)
        // settings.glideFallSpeed должен быть отрицательным (например -1.5)
        Vector3 moveVelocity = (forwardDir * brain.settings.glideForwardSpeed) + (Vector3.up * brain.settings.glideFallSpeed);

        // Применяем движение
        brain.Controller.Move(moveVelocity * Time.deltaTime);

        // Тратим стамину
        EventBus.Publish(new StatChangeEvent(brain.GetComponent<StatComponent>(), StatType.Stamina, -brain.settings.glideStaminaDrain * Time.deltaTime));
    }
}