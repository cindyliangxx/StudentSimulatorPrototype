using System;
using System.Collections.Generic;

public class PlayerStats
{
    public const int MinValue = 0;
    public const int MaxValue = 100;
    public const int InitialValue = 50;

    private readonly Dictionary<StatType, int> values = new Dictionary<StatType, int>();

    public PlayerStats()
    {
        Reset();
    }

    public event Action StatsChanged;

    public void Reset()
    {
        values.Clear();

        foreach (StatType statType in StatTypeUtility.CoreStats)
        {
            values[statType] = InitialValue;
        }

        StatsChanged?.Invoke();
    }

    public int GetValue(StatType statType)
    {
        statType = StatTypeUtility.Normalize(statType);

        if (!values.ContainsKey(statType))
        {
            values[statType] = InitialValue;
        }

        return values[statType];
    }

    public void ApplyEffect(ChoiceEffect effect)
    {
        if (effect == null || effect.Changes == null)
        {
            return;
        }

        Dictionary<StatType, int> mergedChanges = effect.GetMergedStatChanges();

        foreach (KeyValuePair<StatType, int> change in mergedChanges)
        {
            int nextValue = GetValue(change.Key) + change.Value;
            values[change.Key] = Clamp(nextValue);
        }

        StatsChanged?.Invoke();
    }

    public int GetEffectiveChange(StatType statType, int rawAmount)
    {
        statType = StatTypeUtility.Normalize(statType);
        int currentValue = GetValue(statType);
        int nextValue = Clamp(currentValue + rawAmount);
        return nextValue - currentValue;
    }

    public bool TryGetFailedStat(out StatType failedStat)
    {
        foreach (StatType statType in StatTypeUtility.CoreStats)
        {
            if (GetValue(statType) <= MinValue)
            {
                failedStat = statType;
                return true;
            }
        }

        failedStat = StatType.Health;
        return false;
    }

    private static int Clamp(int value)
    {
        if (value < MinValue)
        {
            return MinValue;
        }

        if (value > MaxValue)
        {
            return MaxValue;
        }

        return value;
    }
}
