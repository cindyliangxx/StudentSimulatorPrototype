using System;

[Serializable]
public class EventCardData
{
    public string Id;
    public string Title;
    public string Description;
    public string LeftChoiceText;
    public string RightChoiceText;
    public ChoiceEffect LeftEffect;
    public ChoiceEffect RightEffect;
    public bool IsSpecial;

    public EventCardData(
        string id,
        string title,
        string description,
        string leftChoiceText,
        ChoiceEffect leftEffect,
        string rightChoiceText,
        ChoiceEffect rightEffect,
        bool isSpecial)
    {
        Id = id;
        Title = title;
        Description = description;
        LeftChoiceText = leftChoiceText;
        LeftEffect = leftEffect;
        RightChoiceText = rightChoiceText;
        RightEffect = rightEffect;
        IsSpecial = isSpecial;
    }
}
