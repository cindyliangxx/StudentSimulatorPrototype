public enum StatType
{
    Health = 0,
    Academic = 2,
    Social = 3,
    Money = 4
}

public static class StatTypeUtility
{
    private const int LegacyMentalValue = 1;

    public static readonly StatType[] CoreStats =
    {
        StatType.Health,
        StatType.Academic,
        StatType.Social,
        StatType.Money
    };

    public static StatType Normalize(StatType statType)
    {
        if ((int)statType == LegacyMentalValue)
        {
            return StatType.Health;
        }

        switch (statType)
        {
            case StatType.Health:
            case StatType.Academic:
            case StatType.Social:
            case StatType.Money:
                return statType;
            default:
                return StatType.Health;
        }
    }

    public static string GetDisplayName(StatType statType)
    {
        statType = Normalize(statType);

        switch (statType)
        {
            case StatType.Health:
                return "Health";
            case StatType.Academic:
                return "Academic";
            case StatType.Social:
                return "Social";
            case StatType.Money:
                return "Money";
            default:
                return statType.ToString();
        }
    }
}
