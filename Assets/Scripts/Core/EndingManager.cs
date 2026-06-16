public class EndingManager
{
    public string GetFailureEnding(StatType failedStat)
    {
        switch (failedStat)
        {
            case StatType.Body:
                return "Failure Ending: Your body gave out. You need rest before everything else.";
            case StatType.Mental:
                return "Failure Ending: Your stress collapsed your routine. The semester stops here.";
            case StatType.Academic:
                return "Failure Ending: Your academic standing fell too low to continue.";
            case StatType.Social:
                return "Failure Ending: Isolation caught up with you. Campus life faded away.";
            case StatType.Money:
                return "Failure Ending: Your budget hit zero. You can no longer continue this semester.";
            default:
                return "Failure Ending: The semester ended unexpectedly.";
        }
    }
}
