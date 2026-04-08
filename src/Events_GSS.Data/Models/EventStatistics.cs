namespace Events_GSS.Data.Models;

public class ParticipantOverview
{
    public int TotalParticipants { get; set; }
    public int ActiveParticipants { get; set; }
    public double EngagementRate { get; set; }
}

public class EngagementBreakdown
{
    public int TotalDiscussionMessages { get; set; }
    public int TotalMemories { get; set; }
    public int TotalQuestSubmissions { get; set; }
    public int ApprovedQuests { get; set; }
    public int DeniedQuests { get; set; }
    public double ApprovedQuestsRate { get; set; }
    public double DeniedQuestsRate { get; set; }
}

public class LeaderboardDefault
{
    public const string DefaultTier= "Newcomer";
}

public class LeaderboardEntry
{
    public string UserName { get; set; } = string.Empty;
    public string Tier { get; set; } =LeaderboardDefault.DefaultTier;
    public int TotalMessages { get; set; }
    public int TotalMemories { get; set; }
    public int QuestsCompleted { get; set; }
    public int TotalScore { get; set; }
}

public class QuestAnalyticsEntry
{
    public string QuestName { get; set; } = string.Empty;
    public int TotalCompletedQuests { get; set; }
}
