using System;
using UnityEngine;

public class GameManager
{
    private readonly PlayerStats playerStats = new PlayerStats();
    private readonly CardDeckManager deckManager;
    private readonly EndingManager endingManager = new EndingManager();

    public EventCardData CurrentCard { get; private set; }
    public bool IsGameOver { get; private set; }
    public string EndingText { get; private set; }

    public PlayerStats PlayerStats => playerStats;
    public int TotalNormalCardsCompleted => deckManager.TotalNormalCardsCompleted;
    public bool IsCurrentCardSpecial => CurrentCard != null && CurrentCard.IsSpecial;

    public event Action StateChanged;

    public GameManager(CardDeckManager cardDeckManager)
    {
        deckManager = cardDeckManager;
    }

    public void StartNewGame()
    {
        playerStats.Reset();
        deckManager.InitializeTestData();
        IsGameOver = false;
        EndingText = string.Empty;
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
        string afterStats = GetStatsLogString();

        Debug.Log(
            $"Card: {selectedCard.Title}. Direction: {direction}. Choice: {choiceText}. " +
            $"Changes: {effect.ToLogString()}. Before: {beforeStats}. After: {afterStats}");

        deckManager.MarkCardCompleted(CurrentCard);

        if (playerStats.TryGetFailedStat(out StatType failedStat))
        {
            IsGameOver = true;
            EndingText = endingManager.GetFailureEnding(failedStat);
            Debug.Log(EndingText);
            StateChanged?.Invoke();
            return;
        }

        CurrentCard = deckManager.DrawNextCard();
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
