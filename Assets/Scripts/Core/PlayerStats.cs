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
        values[StatType.Body] = InitialValue;
        values[StatType.Mental] = InitialValue;
        values[StatType.Academic] = InitialValue;
        values[StatType.Social] = InitialValue;
        values[StatType.Money] = InitialValue;
        StatsChanged?.Invoke();
    }

    public int GetValue(StatType statType)
    {
        return values[statType];
    }

    public void ApplyEffect(ChoiceEffect effect)
    {
        if (effect == null || effect.Changes == null)
        {
            return;
        }

        foreach (StatChange change in effect.Changes)
        {
            int nextValue = GetValue(change.StatType) + change.Amount;
            values[change.StatType] = Clamp(nextValue);
        }

        StatsChanged?.Invoke();
    }

    public bool TryGetFailedStat(out StatType failedStat)
    {
        foreach (KeyValuePair<StatType, int> pair in values)
        {
            if (pair.Value <= MinValue)
            {
                failedStat = pair.Key;
                return true;
            }
        }

        failedStat = StatType.Body;
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
