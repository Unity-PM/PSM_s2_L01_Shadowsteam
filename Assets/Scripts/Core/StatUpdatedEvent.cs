using UnityEngine;

public struct StatUpdatedEvent
{
    public StatComponent target;

    public StatUpdatedEvent(StatComponent target)
    {
        this.target = target;
    }
}