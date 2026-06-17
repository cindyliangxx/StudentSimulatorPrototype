using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEventCard", menuName = "Student Simulator/Event Card")]
public class EventCardAsset : ScriptableObject
{
    public string cardId;
    public string title;
    public Sprite cardImage;
    [TextArea(3, 8)] public string description;
    public string leftChoiceText;
    public string rightChoiceText;
    public ChoiceEffect leftChoiceEffect = new ChoiceEffect();
    public ChoiceEffect rightChoiceEffect = new ChoiceEffect();
    public bool isSpecialEvent;
    [Min(0)] public int weight = 1;
    public bool useMaxPriority;
    [Min(0)] public int cooldownTurns;
    public bool isHiddenCard;
    [Min(0)] public int minResolvedEvents;
    public int maxResolvedEvents = -1;
    [TextArea(2, 5)] public string debugNote;
    public List<string> requiredFlags = new List<string>();
    public List<string> blockedFlags = new List<string>();
    public List<StatCondition> statConditions = new List<StatCondition>();
    [TextArea(2, 5)] public string debugConditionNote;

    public EventCardData ToEventCardData()
    {
        return new EventCardData(
            ResolveCardId(),
            title,
            cardImage,
            description,
            leftChoiceText,
            leftChoiceEffect,
            rightChoiceText,
            rightChoiceEffect,
            isSpecialEvent,
            weight,
            useMaxPriority,
            cooldownTurns,
            isHiddenCard,
            minResolvedEvents,
            maxResolvedEvents);
    }

    public string ResolveCardId()
    {
        return string.IsNullOrWhiteSpace(cardId) ? name : cardId.Trim();
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

    public bool IsInResolvedEventWindow(int resolvedEvents, out string failedReason)
    {
        if (resolvedEvents < minResolvedEvents)
        {
            failedReason = $"requires at least {minResolvedEvents} resolved events";
            return false;
        }

        if (maxResolvedEvents >= 0 && resolvedEvents > maxResolvedEvents)
        {
            failedReason = $"expired after {maxResolvedEvents} resolved events";
            return false;
        }

        failedReason = string.Empty;
        return true;
    }
}
