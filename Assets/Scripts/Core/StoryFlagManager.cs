using System.Collections.Generic;

public class StoryFlagManager
{
    private readonly HashSet<string> flags = new HashSet<string>();

    public bool AddFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
        {
            return false;
        }

        return flags.Add(flag.Trim());
    }

    public bool HasFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
        {
            return false;
        }

        return flags.Contains(flag.Trim());
    }

    public List<string> GetAllFlags()
    {
        List<string> allFlags = new List<string>(flags);
        allFlags.Sort();
        return allFlags;
    }

    public void Reset()
    {
        flags.Clear();
    }
}
