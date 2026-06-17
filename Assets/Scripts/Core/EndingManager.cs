using UnityEngine;

public enum EndingType
{
    None,
    FailureEnding,
    NormalEnding,
    HiddenEnding
}

public class EndingResult
{
    public EndingType Type { get; }
    public string Title { get; }
    public string Description { get; }

    public EndingResult(EndingType type, string title, string description)
    {
        Type = type;
        Title = title;
        Description = description;
    }

    public string ToDisplayText()
    {
        return $"{Title}\n{Description}";
    }
}

public class EndingManager
{
    public EndingResult GetFailureEnding(StatType failedStat)
    {
        switch (failedStat)
        {
            case StatType.Health:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending: Health Collapse",
                    "Your physical and mental balance collapsed, forcing the semester to end early.");
            case StatType.Academic:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending: Academic Failure",
                    "Your academic pressure became impossible to recover from.");
            case StatType.Social:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending: Social Isolation",
                    "Your campus relationships fell apart before the semester could stabilize.");
            case StatType.Money:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending: Money Crisis",
                    "Your finances collapsed and forced you into crisis management.");
            default:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending",
                    "University life ended early for an unknown reason.");
        }
    }

    public EndingResult GetFinalEnding(PlayerStats playerStats, StoryFlagManager storyFlags)
    {
        bool hasHelpedFriend = CheckFlag(storyFlags, "HelpedFriend");
        bool hasChoseInternship = CheckFlag(storyFlags, "ChoseInternship");
        bool hasQuestionedLoop = CheckFlag(storyFlags, "QuestionedLoop");
        bool healthOk = CheckStat(playerStats, StatType.Health, 30);
        bool academicOk = CheckStat(playerStats, StatType.Academic, 30);

        bool hiddenEndingUnlocked =
            hasHelpedFriend &&
            hasChoseInternship &&
            hasQuestionedLoop &&
            healthOk &&
            academicOk;

        if (hiddenEndingUnlocked)
        {
            return new EndingResult(
                EndingType.HiddenEnding,
                "Hidden Ending: Wake Up Call",
                "You noticed the pattern behind the campus loop and kept enough balance to move forward.");
        }

        return new EndingResult(
            EndingType.NormalEnding,
            "Normal Ending: Semester Complete",
            "You completed this stage of university life. Not every problem has an answer, but you reached the end.");
    }

    private static bool CheckFlag(StoryFlagManager storyFlags, string flag)
    {
        bool isMet = storyFlags != null && storyFlags.HasFlag(flag);
        Debug.Log($"[EndingCheck] Flag {flag}: {(isMet ? "met" : "missing")}");
        return isMet;
    }

    private static bool CheckStat(PlayerStats playerStats, StatType statType, int threshold)
    {
        int value = playerStats != null ? playerStats.GetValue(statType) : 0;
        bool isMet = value >= threshold;
        Debug.Log($"[EndingCheck] Stat {statType} >= {threshold}: {(isMet ? "met" : "missing")} (current {value})");
        return isMet;
    }
}
