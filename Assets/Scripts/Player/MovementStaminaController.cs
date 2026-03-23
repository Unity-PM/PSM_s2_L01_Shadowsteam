using UnityEngine;
using StarterAssets;
using Unity.VisualScripting;

public class MovementStaminaController : MonoBehaviour
{
    [Header("References")]
    private StarterAssetsInputs input;
    private StatComponent stats;

    [Header("Stamina Settings")]
    private float staminaDrainPerSecond = 2;
    private float minStaminaToAction = 2;
    private void Awake()
    {
        input = gameObject.GetComponent<StarterAssetsInputs>();
        stats = gameObject.GetComponent<StatComponent>();
    }
    private void Update()
    {
        if (!input.sprint)
            return;

        if (stats.getStamina() <= minStaminaToAction)
        {
            input.sprint = false;
            return;
        }

        EventBus.Publish(new StatChangeEvent(
            stats,
            StatType.Stamina,
            -staminaDrainPerSecond * Time.deltaTime
        ));
    }
}