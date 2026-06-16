using System;

public enum StatCompareType
{
    GreaterOrEqual,
    LessOrEqual
}

[Serializable]
public class StatCondition
{
    public StatType statType;
    public StatCompareType compareType;
    public int threshold;

    public bool IsMet(PlayerStats playerStats)
    {
        if (playerStats == null)
        {
            return false;
        }

        int value = playerStats.GetValue(statType);

        switch (compareType)
        {
            case StatCompareType.GreaterOrEqual:
                return value >= threshold;
            case StatCompareType.LessOrEqual:
                return value <= threshold;
            default:
                return false;
        }
    }

    public string ToDebugString()
    {
        string symbol = compareType == StatCompareType.GreaterOrEqual ? ">=" : "<=";
        return $"{statType} {symbol} {threshold}";
    }
}
