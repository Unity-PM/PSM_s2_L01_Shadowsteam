using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public float distance = 5f, mouseSensitivity = 3f;
    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.25f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.12f;
    [SerializeField, Min(0f)] private float groundClearance = 0.25f;
    [SerializeField, Min(0f)] private float groundProbeHeight = 1.5f;
    [SerializeField, Min(0f)] private float groundProbeDistance = 2.5f;
    [SerializeField, Min(0f)] private float maxGroundSnapUp = 1.25f;
    [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.35f;

    private float _yaw, _pitch;
    private bool _cursorUnlockedByAlt;

    void Start() { UIManager.LockCursorForGameplay(); }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            UIManager.UnlockCursorForUi();

        bool altHeld = IsAltHeld();
        if (altHeld)
        {
            _cursorUnlockedByAlt = true;
            UIManager.UnlockCursorForUi();
            return;
        }

        if (!_cursorUnlockedByAlt)
            return;

        _cursorUnlockedByAlt = false;
        if (!ShouldKeepCursorUnlockedForUi())
            UIManager.LockCursorForGameplay();
    }

    void LateUpdate()
    {
        if (!target)
            return;

        Vector3 pivot = target.position + offset;
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            ApplyCameraPosition(pivot);
            return;
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null)
            return;

        _yaw += mouse.delta.x.ReadValue() * mouseSensitivity * 0.1f;
        _pitch = Mathf.Clamp(_pitch - mouse.delta.y.ReadValue() * mouseSensitivity * 0.1f, -20f, 70f);
        transform.eulerAngles = new Vector3(_pitch, _yaw, 0);
        ApplyCameraPosition(pivot);
    }

    private void ApplyCameraPosition(Vector3 pivot)
    {
        Vector3 desiredPosition = pivot - transform.forward * distance;
        Vector3 blockedPosition = ResolveLineOfSight(pivot, desiredPosition);
        transform.position = ResolveGroundClearance(blockedPosition);
    }

    private Vector3 ResolveLineOfSight(Vector3 pivot, Vector3 desiredPosition)
    {
        Vector3 toDesired = desiredPosition - pivot;
        float maxDistance = toDesired.magnitude;
        if (maxDistance <= 0.001f)
            return desiredPosition;

        Vector3 direction = toDesired / maxDistance;
        RaycastHit[] hits = Physics.SphereCastAll(
            pivot,
            collisionRadius,
            direction,
            maxDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        if (!TryGetClosestValidHit(hits, out RaycastHit closestHit))
            return desiredPosition;

        float safeDistance = Mathf.Max(0.05f, closestHit.distance - collisionPadding);
        return pivot + direction * Mathf.Min(safeDistance, maxDistance);
    }

    private Vector3 ResolveGroundClearance(Vector3 cameraPosition)
    {
        if (groundClearance <= 0f || groundProbeDistance <= 0f)
            return cameraPosition;

        Vector3 origin = cameraPosition + Vector3.up * groundProbeHeight;
        float castDistance = groundProbeHeight + groundProbeDistance;
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            collisionRadius,
            Vector3.down,
            castDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        float highestGroundY = float.NegativeInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!IsValidCameraHit(hit) || hit.normal.y < minGroundNormalY)
                continue;

            float requiredY = hit.point.y + groundClearance;
            if (requiredY <= cameraPosition.y || requiredY - cameraPosition.y > maxGroundSnapUp)
                continue;

            highestGroundY = Mathf.Max(highestGroundY, requiredY);
        }

        if (float.IsNegativeInfinity(highestGroundY))
            return cameraPosition;

        cameraPosition.y = highestGroundY;
        return cameraPosition;
    }

    private bool TryGetClosestValidHit(RaycastHit[] hits, out RaycastHit closestHit)
    {
        closestHit = default;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!IsValidCameraHit(hit) || hit.distance >= closestDistance)
                continue;

            closestHit = hit;
            closestDistance = hit.distance;
        }

        return !float.IsPositiveInfinity(closestDistance);
    }

    private bool IsValidCameraHit(RaycastHit hit)
    {
        Transform hitTransform = hit.collider != null ? hit.collider.transform : null;
        if (hitTransform == null || hitTransform == transform || hitTransform.IsChildOf(transform))
            return false;

        if (target == null)
            return true;

        return hitTransform != target
            && !hitTransform.IsChildOf(target)
            && !target.IsChildOf(hitTransform);
    }

    private static bool IsAltHeld()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
    }

    private static bool ShouldKeepCursorUnlockedForUi()
    {
        return Time.timeScale <= 0f || UIManager.HasOpenPanel || PauseMenuController.IsAnyPaused;
    }
}
