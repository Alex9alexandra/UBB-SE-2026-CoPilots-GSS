// <copyright file="EventStatistics.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models;

/// <summary>
/// ... Represents the statistics of an event, including participant overview, engagement breakdown, leaderboard entries, and quest analytics.
/// </summary>
public class ParticipantOverview
{
    /// <summary>
    /// Gets or sets the total number of participants.
    /// </summary>
    public int TotalParticipants { get; set; }

    /// <summary>
    /// Gets or sets the number of participants who are currently active.
    /// </summary>
    public int ActiveParticipants { get; set; }

    /// <summary>
    /// Gets or sets the engagement rate as a percentage value.
    /// </summary>
    public double EngagementRate { get; set; }
}

/// <summary>
/// Represents a breakdown of user engagement metrics, including counts and rates for discussions, memories, and quest
/// submissions.
/// </summary>
/// <remarks>Use this class to access or report detailed engagement statistics, such as the number of discussion
/// messages, memories, and quest outcomes. The rates for approved and denied quests are expressed as percentage values
/// to facilitate analysis of quest approval trends.</remarks>
public class EngagementBreakdown
{
    /// <summary>
    /// Gets or sets the total number of discussion messages.
    /// </summary>
    public int TotalDiscussionMessages { get; set; }

    /// <summary>
    /// Gets or sets the total number of memories.
    /// </summary>
    public int TotalMemories { get; set; }

    /// <summary>
    /// Gets or sets the total number of quest submissions.
    /// </summary>
    public int TotalQuestSubmissions { get; set; }

    /// <summary>
    /// Gets or sets the number of approved quests.
    /// </summary>
    public int ApprovedQuests { get; set; }

    /// <summary>
    /// Gets or sets the number of denied quests.
    /// </summary>
    public int DeniedQuests { get; set; }

    /// <summary>
    /// Gets or sets the rate of approved quests as a percentage value.
    /// </summary>
    public double ApprovedQuestsRate { get; set; }

    /// <summary>
    /// Gets or sets the rate of denied quests as a percentage value.
    /// </summary>
    public double DeniedQuestsRate { get; set; }
}

/// <summary>
/// Provides default values used for leaderboard tiers.
/// </summary>
/// <remarks>This class defines constants that represent default settings for leaderboard functionality. Use these
/// values when initializing or resetting leaderboard tiers to ensure consistency across the application.</remarks>
public class LeaderboardDefault
{
    /// <summary>
    /// Represents the default tier assigned to new users.
    /// </summary>
    public const string DefaultTier = "Newcomer";
}

/// <summary>
/// Represents a single entry in a leaderboard, containing user information and performance metrics.
/// </summary>
/// <remarks>A LeaderboardEntry typically includes the user's name, tier, and various statistics such as total
/// messages, memories, quests completed, and total score. This class is commonly used to display or process leaderboard
/// rankings in applications that track user achievements.</remarks>
public class LeaderboardEntry
{
    /// <summary>
    /// Gets or sets the user name associated with the current context.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tier associated with the leaderboard entry.
    /// </summary>
    public string Tier { get; set; } = LeaderboardDefault.DefaultTier;

    /// <summary>
    /// Gets or sets the total number of messages associated with the leaderboard entry.
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the total number of memories associated with the current context.
    /// </summary>
    public int TotalMemories { get; set; }

    /// <summary>
    /// Gets or sets the total number of quests that have been completed.
    /// </summary>
    public int QuestsCompleted { get; set; }

    /// <summary>
    /// Gets or sets the total score value.
    /// </summary>
    public int TotalScore { get; set; }
}

/// <summary>
/// Represents an analytics entry containing information about a quest and the total number of times it has been
/// completed.
/// </summary>
public class QuestAnalyticsEntry
{
    /// <summary>
    /// Gets or sets the name of the quest.
    /// </summary>
    public string QuestName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of quests that have been completed.
    /// </summary>
    public int TotalCompletedQuests { get; set; }
}
