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
}
