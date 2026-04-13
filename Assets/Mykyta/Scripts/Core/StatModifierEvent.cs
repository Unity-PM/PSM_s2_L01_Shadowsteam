public struct StatModifierEvent
{
    public StatComponent target;
    public StatType statType;
    public float value;

    public StatModifierEvent(StatComponent target, StatType statType, float value)
    {
        this.target = target;
        this.statType = statType;
        this.value = value;
    }
}
