using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEventCard", menuName = "Student Simulator/Event Card")]
public class EventCardAsset : ScriptableObject
{
    public string cardId;
    public string title;
    [TextArea(3, 8)] public string description;
    public string leftChoiceText;
    public string rightChoiceText;
    public ChoiceEffect leftChoiceEffect = new ChoiceEffect();
    public ChoiceEffect rightChoiceEffect = new ChoiceEffect();
    public bool isSpecialEvent;
    [TextArea(2, 5)] public string debugNote;
    public List<string> requiredFlags = new List<string>();
    public List<string> blockedFlags = new List<string>();
    public List<StatCondition> statConditions = new List<StatCondition>();
    [TextArea(2, 5)] public string debugConditionNote;

    public EventCardData ToEventCardData()
    {
        return new EventCardData(
            cardId,
            title,
            description,
            leftChoiceText,
            leftChoiceEffect,
            rightChoiceText,
            rightChoiceEffect,
            isSpecialEvent);
    }

    public bool AreConditionsMet(PlayerStats playerStats, StoryFlagManager storyFlags, out string failedReason)
    {
        if (requiredFlags != null)
        {
            foreach (string flag in requiredFlags)
            {
                if (string.IsNullOrWhiteSpace(flag))
                {
                    continue;
                }

                if (storyFlags == null || !storyFlags.HasFlag(flag))
                {
                    failedReason = $"missing required flag '{flag.Trim()}'";
                    return false;
                }
            }
        }

        if (blockedFlags != null)
        {
            foreach (string flag in blockedFlags)
            {
                if (string.IsNullOrWhiteSpace(flag))
                {
                    continue;
                }

                if (storyFlags != null && storyFlags.HasFlag(flag))
                {
                    failedReason = $"blocked by flag '{flag.Trim()}'";
                    return false;
                }
            }
        }

        if (statConditions != null)
        {
            foreach (StatCondition condition in statConditions)
            {
                if (condition == null)
                {
                    continue;
                }

                if (!condition.IsMet(playerStats))
                {
                    failedReason = $"stat condition not met ({condition.ToDebugString()})";
                    return false;
                }
            }
        }

        failedReason = string.Empty;
        return true;
    }
}
