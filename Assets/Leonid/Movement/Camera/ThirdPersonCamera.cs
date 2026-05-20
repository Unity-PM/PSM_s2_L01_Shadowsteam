using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public float distance = 5f, mouseSensitivity = 3f;
    private float _yaw, _pitch;

    void Start() { Cursor.lockState = CursorLockMode.Locked; }

    void LateUpdate()
    {
        if (!target)
            return;

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null)
            return;

        _yaw += mouse.delta.x.ReadValue() * mouseSensitivity * 0.1f;
        _pitch = Mathf.Clamp(_pitch - mouse.delta.y.ReadValue() * mouseSensitivity * 0.1f, -20f, 70f);
        transform.eulerAngles = new Vector3(_pitch, _yaw, 0);
        transform.position = (target.position + offset) - transform.forward * distance;
    }
}