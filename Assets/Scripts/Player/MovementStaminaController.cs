using UnityEngine;
using Unity.VisualScripting;

public class MovementStaminaController : MonoBehaviour
{
    [Header("References")]
    //private StarterAssetsInputs input; инпут метод
    private StatComponent stats;

    //[Header("Stamina Settings")]
    //private float staminaDrainPerSecond = 2;
    //private float minStaminaToAction = 2;
    private void Awake()
    {
        //input = gameObject.GetComponent<StarterAssetsInputs>(); компонент инпута
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
        //если спринт включен, то отнимаем стамину, если стамины меньше чем нужно для действия, то выключаем спринт а если игрок не спринтит тогда просто ретерн*/
    }
}
