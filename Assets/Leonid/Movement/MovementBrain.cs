using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementBrain : MonoBehaviour
{
    [Header("Modules")]
    [SerializeField] protected MovementModule idleModule;
    [SerializeField] protected MovementModule runModule;
    [SerializeField] protected MovementModule sprintModule;
    [SerializeField] protected MovementModule jumpModule;
    [SerializeField] protected MovementModule glideModule;

    [Header("References")]
    [SerializeField] public MovementSettingsSO settings;
    [SerializeField] public Transform cameraTransform;

    private CharacterController controller;
    private IMovementInput movementInput;
    private Vector3 verticalVelocity;
    private MovementModule activeModule;

    private float lockTimer = 0f;
    private Vector3 externalForce;
    private Vector3 airMomentum;

    public CharacterController Controller => controller;
    public IMovementInput Input => movementInput;
    public MovementState CurrentState => activeModule != null ? activeModule.State : MovementState.Idle;

    public void ApplyImpulse(Vector3 force) => externalForce += force;
    public void LockMovement(float duration) => lockTimer = duration;
    public void SetVerticalVelocity(float val) => verticalVelocity.y = val;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movementInput = GetComponent<IMovementInput>();
        if (gameObject.CompareTag("Player") && cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (lockTimer > 0) lockTimer -= Time.deltaTime;

        UpdateActiveModule();

        // Логика перемещения с учетом инерции прыжка и блокировки
        if (controller.isGrounded)
        {
            Vector3 posBefore = transform.position;
            if (activeModule != null && lockTimer <= 0) activeModule.Process(this);

            // Запоминаем вектор горизонтальной скорости для полета
            Vector3 frameMove = (transform.position - posBefore);
            frameMove.y = 0;
            airMomentum = frameMove / Time.deltaTime;
        }
        else
        {
            // В воздухе: либо глайд, либо сохранение скорости прыжка (airMomentum)
            if (activeModule == glideModule) activeModule.Process(this);
            else controller.Move(airMomentum * Time.deltaTime);
        }

        if (CurrentState != MovementState.Gliding) ApplyGravity();

        ApplyExternalForces();

        if (TryGetComponent(out StatComponent stats))
            stats.StaminaRegenPaused = (CurrentState == MovementState.Sprinting || CurrentState == MovementState.Gliding);
    }

    private void UpdateActiveModule()
    {
        if (jumpModule != null && jumpModule.CanEnter(this))
        { jumpModule.Process(this); }

        if (!controller.isGrounded)
        {
            if (glideModule != null && glideModule.CanEnter(this)) { activeModule = glideModule; }
            else { activeModule = idleModule; }
            return;
        }

        if (sprintModule != null && sprintModule.CanEnter(this)) { activeModule = sprintModule; }
        else if (runModule != null && runModule.CanEnter(this)) { activeModule = runModule; }
        else { activeModule = idleModule; }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0) verticalVelocity.y = -2f;
        verticalVelocity.y += (settings != null ? settings.gravity : -9.81f) * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void ApplyExternalForces()
    {
        if (externalForce.magnitude > 0.1f)
        {
            controller.Move(externalForce * Time.deltaTime);
            externalForce = Vector3.Lerp(externalForce, Vector3.zero, 10f * Time.deltaTime);
        }
    }

    public void RotateTowards(Vector3 direction, float rotSpeed)
    {
        if (direction.magnitude < 0.1f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotSpeed * Time.deltaTime);
    }

    public void Teleport(Vector3 offset)
    {
        controller.enabled = false;
        transform.position += offset;
        controller.enabled = true;
    }

    // Метод для подброса (Launch)
    public void Launch(float force) => verticalVelocity.y = force;

}

