public struct StatChangeEvent
{
    public StatComponent target;
    public StatType statType;
    public float amount;

    public StatChangeEvent(StatComponent target, StatType statType, float amount)
    {
        this.target = target;
        this.statType = statType;
        this.amount = amount;
    }
}