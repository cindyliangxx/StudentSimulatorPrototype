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
            case StatType.Body:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending: Body Collapse",
                    "身体崩溃，因健康问题被迫中断学业。");
            case StatType.Mental:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending: Mental Breakdown",
                    "心理状态崩溃，被迫休学调整。");
            case StatType.Academic:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending: Academic Failure",
                    "学业严重失败，挂科/延毕。");
            case StatType.Social:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending: Social Isolation",
                    "人际关系破裂，陷入孤立。");
            case StatType.Money:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending: Money Crisis",
                    "经济状况崩溃，陷入债务危机。");
            default:
                return new EndingResult(
                    EndingType.FailureEnding,
                    "Failure Ending",
                    "大学生活因未知原因提前结束。");
        }
    }

    public EndingResult GetFinalEnding(PlayerStats playerStats, StoryFlagManager storyFlags)
    {
        bool hasHelpedFriend = CheckFlag(storyFlags, "HelpedFriend");
        bool hasChoseInternship = CheckFlag(storyFlags, "ChoseInternship");
        bool hasQuestionedLoop = CheckFlag(storyFlags, "QuestionedLoop");
        bool mentalOk = CheckStat(playerStats, StatType.Mental, 30);
        bool academicOk = CheckStat(playerStats, StatType.Academic, 30);

        bool hiddenEndingUnlocked =
            hasHelpedFriend &&
            hasChoseInternship &&
            hasQuestionedLoop &&
            mentalOk &&
            academicOk;

        if (hiddenEndingUnlocked)
        {
            return new EndingResult(
                EndingType.HiddenEnding,
                "Hidden Ending: Wake Up Call",
                "你看见了校园循环背后的缝隙，也保住了继续前进的能力。新的学期不再只是重复。");
        }

        return new EndingResult(
            EndingType.NormalEnding,
            "Normal Ending: Semester Complete",
            "你完成了这一阶段的大学生活。并非所有问题都有答案，但你撑到了期末。");
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
