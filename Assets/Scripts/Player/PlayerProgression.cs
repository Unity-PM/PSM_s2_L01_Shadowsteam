using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    [Header("Progression")]
    [SerializeField] private int level = 1;

    [SerializeField] private float currentXP;

    [SerializeField] private float xpToNextLevel = 100f;

    [SerializeField] private float xpGrowthMultiplier = 1.35f;

    public int Level => level;
    public float CurrentXP => currentXP;
    public float XPToNextLevel => xpToNextLevel;

    private void OnEnable()
    {
        EventBus.Subscribe<ExperienceGainedEvent>(OnExperienceGained);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ExperienceGainedEvent>(OnExperienceGained);
    }

    private void OnExperienceGained(ExperienceGainedEvent e)
    {
        AddXP(e.Amount);
    }

    public void AddXP(float amount)
    {
        currentXP += amount;

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;

            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;

        xpToNextLevel *= xpGrowthMultiplier;

        Debug.Log($"LEVEL UP -> {level}");

        EventBus.Publish(new LevelUpEvent(level));
    }
}