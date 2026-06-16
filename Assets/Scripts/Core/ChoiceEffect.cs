using System;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class StatChange
{
    public StatType StatType;
    public int Amount;

    public StatChange()
    {
    }

    public StatChange(StatType statType, int amount)
    {
        StatType = statType;
        Amount = amount;
    }
}

[Serializable]
public class ChoiceEffect
{
    public List<StatChange> Changes = new List<StatChange>();
    public List<string> setFlags = new List<string>();

    public ChoiceEffect()
    {
    }

    public ChoiceEffect(params StatChange[] statChanges)
    {
        Changes.AddRange(statChanges);
    }

    public string ToLogString()
    {
        if (Changes == null || Changes.Count == 0)
        {
            return "No stat changes";
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < Changes.Count; i++)
        {
            StatChange change = Changes[i];
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(change.StatType);
            builder.Append(' ');
            builder.Append(change.Amount >= 0 ? "+" : string.Empty);
            builder.Append(change.Amount);
        }

        return builder.ToString();
    }
}
