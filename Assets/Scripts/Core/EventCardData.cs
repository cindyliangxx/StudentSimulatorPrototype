using System;
using UnityEngine;

[Serializable]
public class EventCardData
{
    public string Id;
    public string Title;
    public Sprite CardImage;
    public string Description;
    public string LeftChoiceText;
    public string RightChoiceText;
    public ChoiceEffect LeftEffect;
    public ChoiceEffect RightEffect;
    public bool IsSpecial;
    public int Weight;
    public bool UseMaxPriority;
    public int CooldownTurns;
    public bool IsHiddenCard;
    public int MinResolvedEvents;
    public int MaxResolvedEvents;

    public EventCardData(
        string id,
        string title,
        Sprite cardImage,
        string description,
        string leftChoiceText,
        ChoiceEffect leftEffect,
        string rightChoiceText,
        ChoiceEffect rightEffect,
        bool isSpecial,
        int weight = 1,
        bool useMaxPriority = false,
        int cooldownTurns = 0,
        bool isHiddenCard = false,
        int minResolvedEvents = 0,
        int maxResolvedEvents = -1)
    {
        Id = id;
        Title = title;
        CardImage = cardImage;
        Description = description;
        LeftChoiceText = leftChoiceText;
        LeftEffect = leftEffect;
        RightChoiceText = rightChoiceText;
        RightEffect = rightEffect;
        IsSpecial = isSpecial;
        Weight = weight;
        UseMaxPriority = useMaxPriority;
        CooldownTurns = cooldownTurns;
        IsHiddenCard = isHiddenCard;
        MinResolvedEvents = minResolvedEvents;
        MaxResolvedEvents = maxResolvedEvents;
    }
}
