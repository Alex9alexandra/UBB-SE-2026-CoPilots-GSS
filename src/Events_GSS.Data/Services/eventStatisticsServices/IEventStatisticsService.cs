// <copyright file="IEventStatisticsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services.eventStatisticsServices;

using Events_GSS.Data.Models;

/// <summary>
/// Defines the contract for the event statistics service, which provides methods to retrieve various statistics and analytics related to events, such as participant overviews, engagement breakdowns, leaderboards, and quest analytics. This interface abstracts the business logic layer for event statistics, allowing for different implementations that can interact with various data sources or storage mechanisms through the underlying repositories. The methods include retrieving participant overviews, engagement breakdowns, leaderboards, and quest analytics for a specified event ID.
/// </summary>
public interface IEventStatisticsService
{
    /// <summary>
    /// Retrieves an overview of participants for a given event, including total participants, active participants, and engagement rate. This method provides a summary of participant involvement in the specified event, allowing for insights into overall participation and engagement levels.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the participant overview.</param>
    /// <returns>A task that represents the asynchronous operation, containing the participant overview for the specified event.</returns>
    Task<ParticipantOverview> GetParticipantOverviewAsync(int eventId);

    /// <summary>
    /// Retrieves a breakdown of engagement metrics for a given event, including total discussion messages, total memories, total quest submissions, approved quests, denied quests, and their respective rates. This method provides detailed insights into the various forms of engagement that occurred during the specified event, allowing for a comprehensive analysis of participant interactions and contributions.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the engagement breakdown.</param>
    /// <returns>A task that represents the asynchronous operation, containing the engagement breakdown for the specified event.</returns>
    Task<EngagementBreakdown> GetEngagementBreakdownAsync(int eventId);

    /// <summary>
    /// Retrieves a leaderboard for a given event, ranking participants based on their total score, which is calculated from their engagement metrics such as total messages, total memories, and quests completed. This method provides a ranked list of participants for the specified event, allowing for recognition of top contributors and fostering a sense of competition and achievement among participants.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the leaderboard.</param>
    /// <returns>A task that represents the asynchronous operation, containing the leaderboard for the specified event.</returns>
    Task<List<LeaderboardEntry>> GetLeaderboardAsync(int eventId);

    /// <summary>
    /// Retrieves analytics for quests in a given event, including the total number of times each quest was completed. This method provides insights into the popularity and engagement levels of different quests within the specified event, allowing for a comprehensive analysis of participant interactions and contributions.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the quest analytics.</param>
    /// <returns>A task that represents the asynchronous operation, containing the quest analytics for the specified event.</returns>
    Task<List<QuestAnalyticsEntry>> GetQuestAnalyticsAsync(int eventId);
}
