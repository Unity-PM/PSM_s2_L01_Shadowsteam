using UnityEngine;
using Unity.VisualScripting;

public class MovementStaminaController : MonoBehaviour
{
    [Header("References")]
    //private StarterAssetsInputs input; input method
    private StatComponent stats;

    //[Header("Stamina Settings")]
    //private float staminaDrainPerSecond = 2;
    //private float minStaminaToAction = 2;
    private void Awake()
    {
        //input = gameObject.GetComponent<StarterAssetsInputs>(); input component
        stats = gameObject.GetComponent<StatComponent>();
    }
    private void Update()
    {
        /*if (!input.sprint)
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
        ));*//*
        //if sprint is on, drain stamina; if stamina is below the action threshold, turn sprint off; if the player isn't sprinting, just return*/
    }
}
