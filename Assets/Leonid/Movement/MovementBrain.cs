using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementBrain : MonoBehaviour
{
    [Header("Modules")]
    public MovementModule idleModule;
    public MovementModule runModule;
    public MovementModule sprintModule;
    public MovementModule jumpModule;
    public MovementModule glideModule;

    [Header("References")]
    public MovementSettingsSO settings;
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
    }

    private void UpdateActiveModule()
    {
        if (jumpModule != null && jumpModule.CanEnter(this))
        { jumpModule.Process(this); }

        // 2. ФИКСАЦИЯ СОСТОЯНИЯ В ВОЗДУХЕ
        if (!controller.isGrounded) {
            if (glideModule != null && glideModule.CanEnter(this)) { activeModule = glideModule; }
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