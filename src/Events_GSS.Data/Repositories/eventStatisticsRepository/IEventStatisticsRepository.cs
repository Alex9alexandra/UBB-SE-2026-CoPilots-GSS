// <copyright file="IEventStatisticsRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.eventStatisticsRepository;

using Events_GSS.Data.Models;

/// <summary>
/// Defines the contract for the event statistics repository, which provides methods to retrieve various statistics and analytics related to events, such as participant overviews, engagement breakdowns, leaderboards, and quest analytics. This interface abstracts the data access layer for event statistics, allowing for different implementations that can interact with various data sources or storage mechanisms.
/// </summary>
public interface IEventStatisticsRepository
{
    /// <summary>
    /// Retrieves an overview of participants for a given event, including total participants, active participants, and engagement rate. This method provides a summary of participant involvement in the specified event, which can be used for reporting and analysis purposes.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the participant overview.</param>
    /// <returns>A <see cref="ParticipantOverview"/> object containing the participant overview statistics.</returns>
    Task<ParticipantOverview> GetParticipantOverviewAsync(int eventId);

    /// <summary>
    /// Retrieves a breakdown of engagement statistics for a given event, including total discussion messages, memories, quest submissions, approved quests, and denied quests. This method provides detailed insights into the engagement levels of participants in the specified event.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the engagement breakdown.</param>
    /// <returns>A <see cref="EngagementBreakdown"/> object containing the engagement statistics.</returns>
    Task<EngagementBreakdown> GetEngagementBreakdownAsync(int eventId);

    /// <summary>
    /// Retrieves the leaderboard for a given event, ranking participants based on their performance or contributions. This method provides a competitive overview of participant achievements in the specified event.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the leaderboard.</param>
    /// <returns>A list of <see cref="LeaderboardEntry"/> objects representing the leaderboard rankings.</returns>
    Task<List<LeaderboardEntry>> GetLeaderboardAsync(int eventId);

    /// <summary>
    /// Retrieves analytics for quests in a given event, including completion rates, success rates, and other relevant metrics. This method provides insights into the effectiveness and engagement of quests within the specified event.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve quest analytics.</param>
    /// <returns>A list of <see cref="QuestAnalyticsEntry"/> objects containing the quest analytics data.</returns>
    Task<List<QuestAnalyticsEntry>> GetQuestAnalyticsAsync(int eventId);
}
