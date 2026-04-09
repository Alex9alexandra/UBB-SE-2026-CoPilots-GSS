// <copyright file="EventStatisticsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services.eventStatisticsServices;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.eventStatisticsRepository;

/// <summary>
/// Implements the <see cref="IEventStatisticsService"/> interface, providing methods to retrieve various statistics and analytics related to events, such as participant overviews, engagement breakdowns, leaderboards, and quest analytics. This class interacts with the event statistics repository to fetch the necessary data and perform calculations to provide insights into event performance and participant engagement.
/// </summary>
public class EventStatisticsService : IEventStatisticsService
{
    private readonly IEventStatisticsRepository repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStatisticsService"/> class with the specified event statistics repository. The repository is used to retrieve data related to event statistics, allowing the service to perform calculations and provide insights based on the retrieved data.
    /// </summary>
    /// <param name="repository">The event statistics repository used to access event-related data.</param>
    public EventStatisticsService(IEventStatisticsRepository repository)
    {
        this.repository = repository;
    }

    /// <summary>
    /// Asynchronously retrieves an overview of participants for a specific event, including the total number of participants, the number of active participants, and the engagement rate. The engagement rate is calculated as the percentage of active participants out of the total participants, providing insights into participant engagement for the event.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the participant overview.</param>
    /// <returns>A task that represents the asynchronous operation, containing the participant overview for the specified event.</returns>
    public async Task<ParticipantOverview> GetParticipantOverviewAsync(int eventId)
    {
        var result = await this.repository.GetParticipantOverviewAsync(eventId);

        if (result.TotalParticipants > 0)
        {
            result.EngagementRate = Math.Round(
                (double)result.ActiveParticipants / result.TotalParticipants * 100,
                2);
        }
        else
        {
            result.EngagementRate = 0;
        }

        return result;
    }

    /// <summary>
    /// Asynchronously retrieves a breakdown of engagement for a specific event, including the total number of discussion messages, memories, quest submissions, approved quests, and denied quests. Additionally, it calculates the approval and denial rates for quest submissions, providing insights into participant engagement and the success rate of quests within the event.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the engagement breakdown.</param>
    /// <returns>A task that represents the asynchronous operation, containing the engagement breakdown for the specified event.</returns>
    public async Task<EngagementBreakdown> GetEngagementBreakdownAsync(int eventId)
    {
        var result = await this.repository.GetEngagementBreakdownAsync(eventId);
        if (result == null)
        {
            return null;
        }

        if (result.TotalQuestSubmissions > 0)
        {
            result.ApprovedQuestsRate = Math.Round(
                (double)result.ApprovedQuests / result.TotalQuestSubmissions * 100,
                2);

            result.DeniedQuestsRate = Math.Round(
                100 - result.ApprovedQuestsRate,
                2);
        }
        else
        {
            result.ApprovedQuestsRate = 0;
            result.DeniedQuestsRate = 0;
        }

        return result;
    }

    /// <summary>
    /// Asynchronously retrieves a leaderboard for a specific event, which includes a list of participants ranked based on their total score. Each entry in the leaderboard contains the participant's username, tier, total messages, total memories, quests completed, and total score. The leaderboard provides insights into the top-performing participants in the event and their engagement levels across various activities.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the leaderboard.</param>
    /// <returns>A task that represents the asynchronous operation, containing the leaderboard for the specified event.</returns>
    public Task<List<LeaderboardEntry>> GetLeaderboardAsync(int eventId)
    {
        return this.repository.GetLeaderboardAsync(eventId);
    }

    /// <summary>
    /// Asynchronously retrieves analytics for quests in a specific event, including a list of quests with their names and the total number of times each quest was completed. This information provides insights into the popularity and engagement levels of different quests within the event, allowing organizers to identify which quests are most appealing to participants and potentially adjust future events based on this data.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve quest analytics.</param>
    /// <returns>A task that represents the asynchronous operation, containing the quest analytics for the specified event.</returns>
    public Task<List<QuestAnalyticsEntry>> GetQuestAnalyticsAsync(int eventId)
    {
        return this.repository.GetQuestAnalyticsAsync(eventId);
    }
}
