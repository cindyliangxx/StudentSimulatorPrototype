using System.Collections.Generic;
using UnityEngine;

public class CardDeckManager : MonoBehaviour
{
    [SerializeField] private List<EventCardAsset> normalCardAssets = new List<EventCardAsset>();
    [SerializeField] private List<EventCardAsset> specialCardAssets = new List<EventCardAsset>();
    [SerializeField] private int maxEventsBeforeEnding = 15;

    private readonly List<EventCardData> normalCards = new List<EventCardData>();
    private readonly List<EventCardData> specialCards = new List<EventCardData>();
    private readonly Dictionary<string, int> cooldownUntilByCardId = new Dictionary<string, int>();
    private PlayerStats playerStats;
    private StoryFlagManager storyFlags;
    private int normalCardIndex;
    private int specialCardIndex;
    private string pendingNextCardId;

    public int TotalNormalCardsCompleted { get; private set; }
    public int TotalResolvedEvents { get; private set; }
    public int MaxEventsBeforeEnding => Mathf.Max(1, maxEventsBeforeEnding);

    public void InitializeTestData(PlayerStats stats, StoryFlagManager flagManager)
    {
        playerStats = stats;
        storyFlags = flagManager;
        normalCards.Clear();
        specialCards.Clear();
        cooldownUntilByCardId.Clear();
        normalCardIndex = 0;
        specialCardIndex = 0;
        pendingNextCardId = string.Empty;
        TotalNormalCardsCompleted = 0;
        TotalResolvedEvents = 0;

        BuildNormalCards();
        BuildSpecialCards();
    }

    public void QueueNextCard(string cardId)
    {
        pendingNextCardId = string.IsNullOrWhiteSpace(cardId) ? string.Empty : cardId.Trim();
    }

    public EventCardData DrawNextCard()
    {
        EventCardData forcedCard = TryDrawPendingCard();
        if (forcedCard != null)
        {
            return forcedCard;
        }

        EventCardAsset selectedAsset = SelectWeightedAssetCard();
        if (selectedAsset != null)
        {
            EventCardData selectedCard = selectedAsset.ToEventCardData();
            Debug.Log(
                $"[CardDeck] Drew card: {selectedCard.Title}. " +
                $"Special: {selectedCard.IsSpecial}. Hidden: {selectedCard.IsHiddenCard}. Weight: {selectedCard.Weight}");
            return selectedCard;
        }

        Debug.LogWarning("[CardDeck] No eligible EventCardAsset found. Using hardcoded fallback card.");
        return DrawAndLogFallback();
    }

    public void MarkCardCompleted(EventCardData card)
    {
        if (card == null)
        {
            return;
        }

        TotalResolvedEvents++;

        if (!card.IsSpecial)
        {
            TotalNormalCardsCompleted++;
        }

        if (card.CooldownTurns > 0 && !string.IsNullOrWhiteSpace(card.Id))
        {
            cooldownUntilByCardId[card.Id.Trim()] = TotalResolvedEvents + card.CooldownTurns;
        }
    }

    private EventCardData TryDrawPendingCard()
    {
        if (string.IsNullOrWhiteSpace(pendingNextCardId))
        {
            return null;
        }

        string requestedCardId = pendingNextCardId.Trim();
        pendingNextCardId = string.Empty;
        EventCardAsset forcedAsset = FindAssetById(requestedCardId);

        if (forcedAsset == null)
        {
            Debug.LogWarning($"[CardDeck] Pending next card '{requestedCardId}' was not found.");
            return null;
        }

        if (!CanUseAsset(forcedAsset, ignoreWeightAndCooldown: true, out string failedReason))
        {
            Debug.LogWarning($"[CardDeck] Pending next card '{requestedCardId}' was blocked: {failedReason}.");
            return null;
        }

        EventCardData forcedCard = forcedAsset.ToEventCardData();
        Debug.Log($"[CardDeck] Forced next card: {forcedCard.Title} ({forcedCard.Id}).");
        return forcedCard;
    }

    private EventCardAsset SelectWeightedAssetCard()
    {
        List<EventCardAsset> maxPriorityCards = new List<EventCardAsset>();
        List<EventCardAsset> weightedCards = new List<EventCardAsset>();
        int totalWeight = 0;

        foreach (EventCardAsset cardAsset in GetAllCardAssets())
        {
            if (!CanUseAsset(cardAsset, ignoreWeightAndCooldown: false, out string failedReason))
            {
                Debug.Log($"[CardDeck] Filtered out '{cardAsset.title}': {failedReason}.");
                continue;
            }

            if (cardAsset.useMaxPriority)
            {
                maxPriorityCards.Add(cardAsset);
                continue;
            }

            int cardWeight = Mathf.Max(0, cardAsset.weight);
            totalWeight += cardWeight;
            weightedCards.Add(cardAsset);
        }

        if (maxPriorityCards.Count > 0)
        {
            return maxPriorityCards[Random.Range(0, maxPriorityCards.Count)];
        }

        if (weightedCards.Count == 0 || totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        foreach (EventCardAsset cardAsset in weightedCards)
        {
            cumulativeWeight += Mathf.Max(0, cardAsset.weight);
            if (roll < cumulativeWeight)
            {
                return cardAsset;
            }
        }

        return weightedCards[weightedCards.Count - 1];
    }

    private bool CanUseAsset(EventCardAsset cardAsset, bool ignoreWeightAndCooldown, out string failedReason)
    {
        if (cardAsset == null)
        {
            failedReason = "card asset is null";
            return false;
        }

        if (!cardAsset.AreConditionsMet(playerStats, storyFlags, out failedReason))
        {
            return false;
        }

        if (!cardAsset.IsInResolvedEventWindow(TotalResolvedEvents, out failedReason))
        {
            return false;
        }

        if (ignoreWeightAndCooldown)
        {
            failedReason = string.Empty;
            return true;
        }

        if (!cardAsset.useMaxPriority && cardAsset.weight <= 0)
        {
            failedReason = "weight is 0";
            return false;
        }

        string cardId = cardAsset.ResolveCardId();
        if (cooldownUntilByCardId.TryGetValue(cardId, out int cooldownUntil) && TotalResolvedEvents < cooldownUntil)
        {
            failedReason = $"cooling down until resolved event {cooldownUntil}";
            return false;
        }

        failedReason = string.Empty;
        return true;
    }

    private EventCardAsset FindAssetById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return null;
        }

        foreach (EventCardAsset cardAsset in GetAllCardAssets())
        {
            if (cardAsset != null && cardAsset.ResolveCardId() == cardId.Trim())
            {
                return cardAsset;
            }
        }

        return null;
    }

    private List<EventCardAsset> GetAllCardAssets()
    {
        List<EventCardAsset> allCards = new List<EventCardAsset>();
        HashSet<EventCardAsset> seenCards = new HashSet<EventCardAsset>();
        AddUniqueAssets(normalCardAssets, allCards, seenCards);
        AddUniqueAssets(specialCardAssets, allCards, seenCards);
        return allCards;
    }

    private static void AddUniqueAssets(
        List<EventCardAsset> source,
        List<EventCardAsset> target,
        HashSet<EventCardAsset> seenCards)
    {
        if (source == null)
        {
            return;
        }

        foreach (EventCardAsset cardAsset in source)
        {
            if (cardAsset == null || seenCards.Contains(cardAsset))
            {
                continue;
            }

            seenCards.Add(cardAsset);
            target.Add(cardAsset);
        }
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

    private EventCardData DrawAndLogFallback()
    {
        EventCardData card = DrawFromDeck(normalCards, ref normalCardIndex);

        if (card == null)
        {
            card = DrawFromDeck(specialCards, ref specialCardIndex);
        }

        if (card == null)
        {
            Debug.LogWarning("[CardDeck] No fallback card is available.");
            return null;
        }

        Debug.Log($"[CardDeck] Drew fallback card: {card.Title}. Special: {card.IsSpecial}");
        return card;
    }

    private void BuildNormalCards()
    {
        normalCards.Add(new EventCardData(
            "normal_01",
            "Morning Lecture",
            null,
            "The professor announces a surprise quiz in a crowded morning class.",
            "Review notes",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 10),
                new StatChange(StatType.Health, -9)),
            "Wing it",
            new ChoiceEffect(
                new StatChange(StatType.Health, 4),
                new StatChange(StatType.Academic, -8)),
            false));

        normalCards.Add(new EventCardData(
            "normal_02",
            "Cafeteria Line",
            null,
            "Lunch hour is chaos, but your friend waves from a table.",
            "Eat properly",
            new ChoiceEffect(
                new StatChange(StatType.Health, 8),
                new StatChange(StatType.Money, -5)),
            "Skip lunch and chat",
            new ChoiceEffect(
                new StatChange(StatType.Social, 8),
                new StatChange(StatType.Health, -8)),
            false));

        normalCards.Add(new EventCardData(
            "normal_03",
            "Part-Time Shift",
            null,
            "A cafe near campus asks if you can cover an evening shift.",
            "Take the shift",
            new ChoiceEffect(
                new StatChange(StatType.Money, 14),
                new StatChange(StatType.Health, -8),
                new StatChange(StatType.Academic, -4)),
            "Decline politely",
            new ChoiceEffect(
                new StatChange(StatType.Health, 5),
                new StatChange(StatType.Money, -4)),
            false));

        normalCards.Add(new EventCardData(
            "normal_04",
            "Club Poster",
            null,
            "A campus club is recruiting volunteers for a weekend event.",
            "Join the event",
            new ChoiceEffect(
                new StatChange(StatType.Social, 12),
                new StatChange(StatType.Health, -8)),
            "Focus on yourself",
            new ChoiceEffect(
                new StatChange(StatType.Health, 7),
                new StatChange(StatType.Social, -5)),
            false));

        normalCards.Add(new EventCardData(
            "normal_05",
            "Library Night",
            null,
            "The exam is close, and the library still has a few empty seats.",
            "Study late",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 14),
                new StatChange(StatType.Health, -11)),
            "Sleep early",
            new ChoiceEffect(
                new StatChange(StatType.Health, 10),
                new StatChange(StatType.Academic, -6)),
            false));

        normalCards.Add(new EventCardData(
            "normal_06",
            "Roommate Trouble",
            null,
            "Your roommate keeps making noise while you are trying to rest.",
            "Talk it out",
            new ChoiceEffect(
                new StatChange(StatType.Social, 7),
                new StatChange(StatType.Health, 3)),
            "Endure silently",
            new ChoiceEffect(
                new StatChange(StatType.Health, -10),
                new StatChange(StatType.Social, -3)),
            false));

        normalCards.Add(new EventCardData(
            "normal_07",
            "Flash Sale",
            null,
            "Your favorite store has a one-day discount on something you want.",
            "Buy it",
            new ChoiceEffect(
                new StatChange(StatType.Health, 8),
                new StatChange(StatType.Money, -15)),
            "Save money",
            new ChoiceEffect(
                new StatChange(StatType.Money, 5),
                new StatChange(StatType.Health, -4)),
            false));

        normalCards.Add(new EventCardData(
            "normal_08",
            "Group Project",
            null,
            "Your team needs someone to organize the final presentation.",
            "Lead the group",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 8),
                new StatChange(StatType.Social, 6),
                new StatChange(StatType.Health, -8)),
            "Do your part only",
            new ChoiceEffect(
                new StatChange(StatType.Health, 4),
                new StatChange(StatType.Social, -6)),
            false));
    }

    private void BuildSpecialCards()
    {
        specialCards.Add(new EventCardData(
            "special_01",
            "Midterm Crossroads",
            null,
            "A major midterm week arrives, and every choice suddenly feels heavier.",
            "Commit to study",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 18),
                new StatChange(StatType.Health, -18)),
            "Protect your balance",
            new ChoiceEffect(
                new StatChange(StatType.Health, 15),
                new StatChange(StatType.Academic, -10)),
            true));

        specialCards.Add(new EventCardData(
            "special_02",
            "Scholarship Notice",
            null,
            "The student office posts a short-list for scholarship interviews.",
            "Prepare seriously",
            new ChoiceEffect(
                new StatChange(StatType.Academic, 12),
                new StatChange(StatType.Money, 10),
                new StatChange(StatType.Health, -10)),
            "Let it pass",
            new ChoiceEffect(
                new StatChange(StatType.Health, 8),
                new StatChange(StatType.Money, -8),
                new StatChange(StatType.Academic, -4)),
            true));
    }
}
