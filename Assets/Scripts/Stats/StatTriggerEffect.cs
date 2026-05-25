using UnityEngine;

public class StatTriggerEffect : MonoBehaviour
{
    public StatType statType;
    private float amountPerSecond = -5f;

    private void OnTriggerStay(Collider other)
    {
        StatComponent stats = other.GetComponent<StatComponent>();
        if (stats == null) return;

        EventBus.Publish(
            new StatChangeEvent(
                stats,
                statType,
                amountPerSecond * Time.deltaTime
            )
        );
    }
}