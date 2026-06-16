using System.Collections.Generic;
using UnityEngine;

public class CardDeckManager : MonoBehaviour
{
    private const int NormalCardsBeforeSpecial = 5;

    [SerializeField] private List<EventCardAsset> normalCardAssets = new List<EventCardAsset>();
    [SerializeField] private List<EventCardAsset> specialCardAssets = new List<EventCardAsset>();

    private readonly List<EventCardData> normalCards = new List<EventCardData>();
    private readonly List<EventCardData> specialCards = new List<EventCardData>();
    private int normalCardIndex;
    private int specialCardIndex;
    private int normalCardsSinceSpecial;

    public int TotalNormalCardsCompleted { get; private set; }

    public void InitializeTestData()
    {
        normalCards.Clear();
        specialCards.Clear();
        normalCardIndex = 0;
        specialCardIndex = 0;
        normalCardsSinceSpecial = 0;
        TotalNormalCardsCompleted = 0;

        if (!BuildCardsFromAssets(normalCardAssets, normalCards))
        {
            BuildNormalCards();
        }

        if (!BuildCardsFromAssets(specialCardAssets, specialCards))
        {
            BuildSpecialCards();
        }
    }

    public EventCardData DrawNextCard()
    {
        if (normalCardsSinceSpecial >= NormalCardsBeforeSpecial && specialCards.Count > 0)
        {
            normalCardsSinceSpecial = 0;
            return DrawFromDeck(specialCards, ref specialCardIndex);
        }

        return DrawFromDeck(normalCards, ref normalCardIndex);
    }

    public void MarkCardCompleted(EventCardData card)
    {
        if (card == null || card.IsSpecial)
        {
            return;
        }

        TotalNormalCardsCompleted++;
        normalCardsSinceSpecial++;
    }

    private static EventCardData DrawFromDeck(List<EventCardData> deck, ref int index)
    {
        if (deck.Count == 0)
        {
            return null;
        }

        EventCardData card = deck[index];
        index = (index + 1) % deck.Count;
        return card;
    }

    private static bool BuildCardsFromAssets(List<EventCardAsset> cardAssets, List<EventCardData> targetDeck)
    {
        if (cardAssets == null || cardAssets.Count == 0)
        {
            return false;
        }

        foreach (EventCardAsset cardAsset in cardAssets)
        {
            if (cardAsset == null)
            {
                continue;
            }

            targetDeck.Add(cardAsset.ToEventCardData());
        }

        return targetDeck.Count > 0;
    }

    private void BuildNormalCards()
    {
        normalCards.Add(new EventCardData(
            "normal_01",
            "Morning Lecture",
            "The professor announces a surprise quiz in a crowded morning class.",
            "Review notes",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 10),
                new StatChange(StatType.Mental, -6),
                new StatChange(StatType.Body, -3)),
            "Wing it",
            new ChoiceEffect(
                new StatChange(StatType.Mental, 4),
                new StatChange(StatType.Academic, -8)),
            false));

        normalCards.Add(new EventCardData(
            "normal_02",
            "Cafeteria Line",
            "Lunch hour is chaos, but your friend waves from a table.",
            "Eat properly",
            new ChoiceEffect(
                new StatChange(StatType.Body, 8),
                new StatChange(StatType.Money, -5)),
            "Skip lunch and chat",
            new ChoiceEffect(
                new StatChange(StatType.Social, 8),
                new StatChange(StatType.Body, -8)),
            false));

        normalCards.Add(new EventCardData(
            "normal_03",
            "Part-Time Shift",
            "A cafe near campus asks if you can cover an evening shift.",
            "Take the shift",
            new ChoiceEffect(
                new StatChange(StatType.Money, 14),
                new StatChange(StatType.Body, -8),
                new StatChange(StatType.Academic, -4)),
            "Decline politely",
            new ChoiceEffect(
                new StatChange(StatType.Mental, 5),
                new StatChange(StatType.Money, -4)),
            false));

        normalCards.Add(new EventCardData(
            "normal_04",
            "Club Poster",
            "A campus club is recruiting volunteers for a weekend event.",
            "Join the event",
            new ChoiceEffect(
                new StatChange(StatType.Social, 12),
                new StatChange(StatType.Body, -5),
                new StatChange(StatType.Mental, -3)),
            "Focus on yourself",
            new ChoiceEffect(
                new StatChange(StatType.Mental, 7),
                new StatChange(StatType.Social, -5)),
            false));

        normalCards.Add(new EventCardData(
            "normal_05",
            "Library Night",
            "The exam is close, and the library still has a few empty seats.",
            "Study late",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 14),
                new StatChange(StatType.Body, -6),
                new StatChange(StatType.Mental, -5)),
            "Sleep early",
            new ChoiceEffect(
                new StatChange(StatType.Body, 10),
                new StatChange(StatType.Academic, -6)),
            false));

        normalCards.Add(new EventCardData(
            "normal_06",
            "Roommate Trouble",
            "Your roommate keeps making noise while you are trying to rest.",
            "Talk it out",
            new ChoiceEffect(
                new StatChange(StatType.Social, 7),
                new StatChange(StatType.Mental, 3)),
            "Endure silently",
            new ChoiceEffect(
                new StatChange(StatType.Mental, -10),
                new StatChange(StatType.Social, -3)),
            false));

        normalCards.Add(new EventCardData(
            "normal_07",
            "Flash Sale",
            "Your favorite store has a one-day discount on something you want.",
            "Buy it",
            new ChoiceEffect(
                new StatChange(StatType.Mental, 8),
                new StatChange(StatType.Money, -15)),
            "Save money",
            new ChoiceEffect(
                new StatChange(StatType.Money, 5),
                new StatChange(StatType.Mental, -4)),
            false));

        normalCards.Add(new EventCardData(
            "normal_08",
            "Group Project",
            "Your team needs someone to organize the final presentation.",
            "Lead the group",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 8),
                new StatChange(StatType.Social, 6),
                new StatChange(StatType.Mental, -8)),
            "Do your part only",
            new ChoiceEffect(
                new StatChange(StatType.Mental, 4),
                new StatChange(StatType.Social, -6)),
            false));
    }

    private void BuildSpecialCards()
    {
        specialCards.Add(new EventCardData(
            "special_01",
            "Midterm Crossroads",
            "A major midterm week arrives, and every choice suddenly feels heavier.",
            "Commit to study",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 18),
                new StatChange(StatType.Mental, -12),
                new StatChange(StatType.Body, -6)),
            "Protect your balance",
            new ChoiceEffect(
                new StatChange(StatType.Mental, 10),
                new StatChange(StatType.Body, 5),
                new StatChange(StatType.Academic, -10)),
            true));

        specialCards.Add(new EventCardData(
            "special_02",
            "Scholarship Notice",
            "The student office posts a short-list for scholarship interviews.",
            "Prepare seriously",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 12),
                new StatChange(StatType.Money, 10),
                new StatChange(StatType.Mental, -10)),
            "Let it pass",
            new ChoiceEffect(
                new StatChange(StatType.Mental, 8),
                new StatChange(StatType.Money, -8),
                new StatChange(StatType.Academic, -4)),
            true));
    }
}
