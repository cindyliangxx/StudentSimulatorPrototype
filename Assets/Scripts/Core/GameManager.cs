using System;
using UnityEngine;

public class GameManager
{
    private readonly PlayerStats playerStats = new PlayerStats();
    private readonly StoryFlagManager storyFlagManager = new StoryFlagManager();
    private readonly CardDeckManager deckManager;
    private readonly EndingManager endingManager = new EndingManager();

    public EventCardData CurrentCard { get; private set; }
    public bool IsGameOver { get; private set; }
    public string EndingText { get; private set; }
    public string EndingTitle { get; private set; }
    public string EndingDescription { get; private set; }
    public EndingType CurrentEndingType { get; private set; }

    public PlayerStats PlayerStats => playerStats;
    public StoryFlagManager StoryFlags => storyFlagManager;
    public int TotalNormalCardsCompleted => deckManager.TotalNormalCardsCompleted;
    public int TotalResolvedEvents => deckManager.TotalResolvedEvents;
    public int MaxEventsBeforeEnding => deckManager.MaxEventsBeforeEnding;
    public bool IsCurrentCardSpecial => CurrentCard != null && CurrentCard.IsSpecial;

    public event Action StateChanged;

    public GameManager(CardDeckManager cardDeckManager)
    {
        deckManager = cardDeckManager;
    }

    public void StartNewGame()
    {
        playerStats.Reset();
        storyFlagManager.Reset();
        deckManager.InitializeTestData(playerStats, storyFlagManager);
        IsGameOver = false;
        EndingText = string.Empty;
        EndingTitle = string.Empty;
        EndingDescription = string.Empty;
        CurrentEndingType = EndingType.None;
        CurrentCard = deckManager.DrawNextCard();
        StateChanged?.Invoke();
    }

    public void ChooseLeft()
    {
        ApplyChoice(true);
    }

    public void ChooseRight()
    {
        ApplyChoice(false);
    }

    private void ApplyChoice(bool chooseLeft)
    {
        if (IsGameOver || CurrentCard == null)
        {
            return;
        }

        EventCardData selectedCard = CurrentCard;
        ChoiceEffect effect = chooseLeft ? CurrentCard.LeftEffect : CurrentCard.RightEffect;
        string choiceText = chooseLeft ? CurrentCard.LeftChoiceText : CurrentCard.RightChoiceText;
        string direction = chooseLeft ? "Left" : "Right";
        string beforeStats = GetStatsLogString();

        // Apply first, then log both the change and the resulting prototype state.
        playerStats.ApplyEffect(effect);
        ApplyStoryFlags(effect);
        string afterStats = GetStatsLogString();

        Debug.Log(
            $"Card: {selectedCard.Title}. Direction: {direction}. Choice: {choiceText}. " +
            $"Changes: {effect.ToLogString()}. Before: {beforeStats}. After: {afterStats}");

        deckManager.MarkCardCompleted(CurrentCard);

        if (playerStats.TryGetFailedStat(out StatType failedStat))
        {
            EnterEnding(endingManager.GetFailureEnding(failedStat));
            return;
        }

        if (deckManager.TotalResolvedEvents >= deckManager.MaxEventsBeforeEnding)
        {
            EnterEnding(endingManager.GetFinalEnding(playerStats, storyFlagManager));
            return;
        }

        CurrentCard = deckManager.DrawNextCard();
        StateChanged?.Invoke();
    }

    private void ApplyStoryFlags(ChoiceEffect effect)
    {
        if (effect == null || effect.setFlags == null)
        {
            return;
        }

        foreach (string flag in effect.setFlags)
        {
            if (storyFlagManager.AddFlag(flag))
            {
                Debug.Log($"[StoryFlag] Added: {flag.Trim()}");
            }
        }
    }

    private void EnterEnding(EndingResult endingResult)
    {
        IsGameOver = true;
        CurrentCard = null;
        CurrentEndingType = endingResult.Type;
        EndingTitle = endingResult.Title;
        EndingDescription = endingResult.Description;
        EndingText = endingResult.ToDisplayText();
        Debug.Log($"[Ending] Entered {endingResult.Type}: {endingResult.Title}");
        StateChanged?.Invoke();
    }

    private string GetStatsLogString()
    {
        return
            $"Body={playerStats.GetValue(StatType.Body)}, " +
            $"Mental={playerStats.GetValue(StatType.Mental)}, " +
            $"Academic={playerStats.GetValue(StatType.Academic)}, " +
            $"Social={playerStats.GetValue(StatType.Social)}, " +
            $"Money={playerStats.GetValue(StatType.Money)}";
    }
}
