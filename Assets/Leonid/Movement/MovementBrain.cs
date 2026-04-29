using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementBrain : MonoBehaviour
{
    [Header("Modules")]
    [SerializeField]
    protected MovementModule idleModule;
    [SerializeField]
    protected MovementModule runModule;
    [SerializeField]
    protected MovementModule sprintModule;
    [SerializeField]
    protected MovementModule jumpModule;
    [SerializeField]
    protected MovementModule glideModule;

    [Header("References")]
    [SerializeField]
    public MovementSettingsSO settings;
    [SerializeField]
    public Transform cameraTransform;

    private CharacterController controller;
    private IMovementInput movementInput;
    private Vector3 verticalVelocity;
    private MovementModule activeModule;

    public CharacterController Controller => controller;
    public IMovementInput Input => movementInput;
    public MovementState CurrentState => activeModule != null ? activeModule.State : MovementState.Idle;

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
        UpdateActiveModule();
        if (activeModule != null) activeModule.Process(this);
        if (CurrentState != MovementState.Gliding) ApplyGravity();

        // Управление паузой регена стамины
        if (TryGetComponent(out StatComponent stats))
        {
            stats.StaminaRegenPaused = (CurrentState == MovementState.Sprinting || CurrentState == MovementState.Gliding);
        }
    }

    private void UpdateActiveModule()
    {
        if (jumpModule != null && jumpModule.CanEnter(this))
        { jumpModule.Process(this); }

        // 2. ФИКСАЦИЯ СОСТОЯНИЯ В ВОЗДУХЕ
        if (!controller.isGrounded)
        {
            // Если можем лететь — летим, иначе сбрасываем в idleModule, чтобы работала гравитация
            if (glideModule != null && glideModule.CanEnter(this))
            {
                activeModule = glideModule;
            }
            else
            {
                activeModule = idleModule;
            }
            return;
        }

        // 3. ОБЫЧНАЯ ЛОГИКА (только когда на земле)
        if (sprintModule != null && sprintModule.CanEnter(this))
        { activeModule = sprintModule; }
        else if (runModule != null && runModule.CanEnter(this))
        { activeModule = runModule; }
        else
        { activeModule = idleModule; }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0) verticalVelocity.y = -2f;
        verticalVelocity.y += (settings != null ? settings.gravity : -9.81f) * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    public void RotateTowards(Vector3 direction, float rotSpeed)
    {
        if (direction.magnitude < 0.1f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotSpeed * Time.deltaTime);
    }
}