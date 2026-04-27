using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettings", menuName = "DGD/Movement Settings")]
public class MovementSettingsSO : ScriptableObject
{
    [Header("Speeds")]
    public float runSpeed = 5.5f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 10f;

    [Header("Physics")]
    public float gravity = -19.62f;
    public float jumpHeight = 2.0f;

    [Header("Stamina")]
    public float staminaDrainPerSecond = 10f;

    [Header("Gliding")]
    public float glideFallSpeed = -1.5f;
    public float glideForwardSpeed = 8f;
    public float glideStaminaDrain = 5f;
}