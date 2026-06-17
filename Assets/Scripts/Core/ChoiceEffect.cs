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
    public List<string> clearFlags = new List<string>();
    public string nextCardId;

    public ChoiceEffect()
    {
    }

    public ChoiceEffect(params StatChange[] statChanges)
    {
        Changes.AddRange(statChanges);
    }

    public Dictionary<StatType, int> GetMergedStatChanges()
    {
        Dictionary<StatType, int> mergedChanges = new Dictionary<StatType, int>();

        if (Changes == null)
        {
            return mergedChanges;
        }

        foreach (StatChange change in Changes)
        {
            StatType statType = StatTypeUtility.Normalize(change.StatType);

            if (!mergedChanges.ContainsKey(statType))
            {
                mergedChanges[statType] = 0;
            }

            mergedChanges[statType] += change.Amount;
        }

        return mergedChanges;
    }

    public string ToLogString()
    {
        Dictionary<StatType, int> mergedChanges = GetMergedStatChanges();

        if (mergedChanges.Count == 0)
        {
            return "No stat changes";
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;

        foreach (StatType statType in StatTypeUtility.CoreStats)
        {
            if (!mergedChanges.TryGetValue(statType, out int amount))
            {
                continue;
            }

            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(StatTypeUtility.GetDisplayName(statType));
            builder.Append(' ');
            builder.Append(amount >= 0 ? "+" : string.Empty);
            builder.Append(amount);
            index++;
        }

        return builder.ToString();
    }
}
