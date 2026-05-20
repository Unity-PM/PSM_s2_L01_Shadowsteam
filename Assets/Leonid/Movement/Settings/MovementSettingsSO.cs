using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettings", menuName = "DGD/Movement Settings")]
public class MovementSettingsSO : ScriptableObject
{
    [Header("Speeds")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 5.5f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 10f;

    [Header("Physics")]
    public float gravity = -19.62f;
    public float jumpHeight = 1.0f;

    [Header("Stamina")]
    public float runStaminaDrainPerSecond = 1.5f;
    public float staminaDrainPerSecond = 8f;
    public float jumpStaminaCost = 5f;
}