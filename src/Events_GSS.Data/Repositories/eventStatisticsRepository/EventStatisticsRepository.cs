// <copyright file="EventStatisticsRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Events_GSS.Data.Repositories.eventStatisticsRepository;

using System.Data;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

/// <summary>
/// Implements the <see cref="IEventStatisticsRepository"/> interface to provide methods for retrieving various statistics related to events, such as participant overviews, engagement breakdowns, leaderboards, and quest analytics. This repository interacts with the database using raw SQL queries to efficiently gather and compute the required data for event statistics.
/// </summary>
public class EventStatisticsRepository : IEventStatisticsRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStatisticsRepository"/> class with the specified <see cref="SqlConnectionFactory"/>. The connection factory is used to create database connections for executing SQL queries to retrieve event statistics.
    /// </summary>
    /// <param name="connectionFactory">The factory used to create SQL connections.</param>
    public EventStatisticsRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Retrieves an overview of participants for a given event, including the total number of participants, the number of active participants (those who have engaged in discussions, memories, or quests), and the engagement rate. The method executes a SQL query that counts the total attendees and identifies active participants based on their interactions with the event's features. The results are returned as a <see cref="ParticipantOverview"/> object containing the relevant statistics.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the participant overview.</param>
    /// <returns>A <see cref="ParticipantOverview"/> object containing the total participants, active participants, and engagement rate.</returns>
    public async Task<ParticipantOverview> GetParticipantOverviewAsync(int eventId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
            SELECT 
                COUNT(*) AS TotalParticipants,
                (
                    SELECT COUNT(DISTINCT UserId)
                    FROM (
                        SELECT UserId FROM Discussions WHERE EventId = @EventId
                        UNION
                        SELECT UserId FROM Memories WHERE EventId = @EventId
                        UNION
                        SELECT m.UserId
                        FROM QuestMemories qm
                        INNER JOIN Memories m ON qm.MemoryId = m.MemoryId
                        INNER JOIN Quests q ON qm.QuestId = q.QuestId
                        WHERE q.EventId = @EventId
                    ) active
                ) AS ActiveParticipants
            FROM AttendedEvents
            WHERE EventId = @EventId;";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            int totalParticipants = (int)reader["TotalParticipants"];
            int activeParticipants = (int)reader["ActiveParticipants"];
            return new ParticipantOverview
            {
                TotalParticipants = totalParticipants,
                ActiveParticipants = activeParticipants,
                EngagementRate = 0,
            };
        }

        return new ParticipantOverview();
    }

    /// <summary>
    /// Retrieves a breakdown of engagement statistics for a given event, including the total number of discussion messages, memories, quest submissions, approved quests, and denied quests. The method executes a SQL query that aggregates these counts based on the event ID and returns the results as an <see cref="EngagementBreakdown"/> object containing the relevant statistics for the event's engagement.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the engagement breakdown.</param>
    /// <returns>A <see cref="EngagementBreakdown"/> object containing the engagement statistics for the event.</returns>
    public async Task<EngagementBreakdown> GetEngagementBreakdownAsync(int eventId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
            SELECT
                (SELECT COUNT(*) FROM Discussions WHERE EventId = @EventId) AS TotalMessages,
                (SELECT COUNT(*) FROM Memories WHERE EventId = @EventId) AS TotalMemories,
                (
                    SELECT COUNT(*)
                    FROM QuestMemories qm
                    INNER JOIN Quests q ON qm.QuestId = q.QuestId
                    WHERE q.EventId = @EventId
                ) AS TotalSubmissions,
                (
                    SELECT COUNT(*)
                    FROM QuestMemories qm
                    INNER JOIN Quests q ON qm.QuestId = q.QuestId
                    WHERE q.EventId = @EventId AND qm.Status = @ApprovedStatus
                ) AS ApprovedQuests,
                (
                    SELECT COUNT(*)
                    FROM QuestMemories qm
                    INNER JOIN Quests q ON qm.QuestId = q.QuestId
                    WHERE q.EventId = @EventId AND qm.Status = @RejectedStatus
                ) AS DeniedQuests;";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
        command.Parameters.Add("@ApprovedStatus", SqlDbType.NVarChar).Value = StatisticsConstants.ApprovedStatus;
        command.Parameters.Add("@RejectedStatus", SqlDbType.NVarChar).Value = StatisticsConstants.RejectedStatus;

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            int totalMessages = (int)reader["TotalMessages"];
            int totalMemories = (int)reader["TotalMemories"];
            int totalSubmissions = (int)reader["TotalSubmissions"];
            int approved = (int)reader["ApprovedQuests"];
            int denied = (int)reader["DeniedQuests"];

            return new EngagementBreakdown
            {
                TotalDiscussionMessages = totalMessages,
                TotalMemories = totalMemories,
                TotalQuestSubmissions = totalSubmissions,
                ApprovedQuests = approved,
                DeniedQuests = denied,
                ApprovedQuestsRate = 0,
                DeniedQuestsRate = 0,
            };
        }

        return new EngagementBreakdown();
    }

    /// <summary>
    /// Retrieves a leaderboard of participants for a given event, ranked by their total engagement score. The score is calculated based on the number of discussion messages, memories, and completed quests, with configurable weights for each type of engagement. The method executes a SQL query that aggregates these counts for each participant, applies the scoring formula, and returns a list of <see cref="LeaderboardEntry"/> objects containing the participant's name, tier, total messages, memories, quests completed, and total score. The leaderboard is ordered by total score in descending order and limited to a specified number of top entries.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve the leaderboard.</param>
    /// <returns>A list of <see cref="LeaderboardEntry"/> objects representing the top participants in the event.</returns>
    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int eventId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
            SELECT TOP (@Limit)
                u.Name AS UserName,
                ISNULL(urp.Tier, @DefaultTier) AS Tier,
                ISNULL(msg.cnt, 0) AS MessagesCount,
                ISNULL(mem.cnt, 0) AS MemoriesCount,
                ISNULL(qst.cnt, 0) AS QuestsCompleted,
                ISNULL(msg.cnt, 0) 
                    + (ISNULL(mem.cnt, 0) * @MemoryWeight) 
                    + (ISNULL(qst.cnt, 0) * @QuestWeight) AS TotalScore
            FROM AttendedEvents ae
            INNER JOIN Users u ON ae.UserId = u.Id
            LEFT JOIN users_RP_scores urp ON urp.UserId = u.Id
            LEFT JOIN (
                SELECT UserId, COUNT(*) AS cnt
                FROM Discussions
                WHERE EventId = @EventId
                GROUP BY UserId
            ) msg ON msg.UserId = u.Id
            LEFT JOIN (
                SELECT UserId, COUNT(*) AS cnt
                FROM Memories
                WHERE EventId = @EventId
                GROUP BY UserId
            ) mem ON mem.UserId = u.Id
            LEFT JOIN (
                SELECT m.UserId, COUNT(*) AS cnt
                FROM QuestMemories qm
                INNER JOIN Memories m ON qm.MemoryId = m.MemoryId
                INNER JOIN Quests q ON qm.QuestId = q.QuestId
                WHERE q.EventId = @EventId AND qm.Status = @ApprovedStatus
                GROUP BY m.UserId
            ) qst ON qst.UserId = u.Id
            WHERE ae.EventId = @EventId
            ORDER BY TotalScore DESC;";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
        command.Parameters.Add("@DefaultTier", SqlDbType.NVarChar).Value = StatisticsConstants.DefaultTier;
        command.Parameters.Add("@ApprovedStatus", SqlDbType.NVarChar).Value = StatisticsConstants.ApprovedStatus;
        command.Parameters.Add("@MemoryWeight", SqlDbType.Int).Value = StatisticsConstants.MemoryScoreWeight;
        command.Parameters.Add("@QuestWeight", SqlDbType.Int).Value = StatisticsConstants.QuestScoreWeight;
        command.Parameters.Add("@Limit", SqlDbType.Int).Value = StatisticsConstants.LeaderboardLimit;

        var entries = new List<LeaderboardEntry>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new LeaderboardEntry
            {
                UserName = (string)reader["UserName"],
                Tier = (string)reader["Tier"],
                TotalMessages = (int)reader["MessagesCount"],
                TotalMemories = (int)reader["MemoriesCount"],
                QuestsCompleted = (int)reader["QuestsCompleted"],
                TotalScore = (int)reader["TotalScore"],
            });
        }

        return entries;
    }

    /// <summary>
    /// Retrieves analytics data for quests associated with a given event, including the name of each quest and the total number of times it has been completed (approved). The method executes a SQL query that joins the Quests and QuestMemories tables to count the number of approved completions for each quest in the specified event. The results are returned as a list of <see cref="QuestAnalyticsEntry"/> objects containing the quest name and total completed quests, ordered by the number of completions in descending order.
    /// </summary>
    /// <param name="eventId">The ID of the event for which to retrieve quest analytics.</param>
    /// <returns>A list of <see cref="QuestAnalyticsEntry"/> objects containing quest analytics data.</returns>
    public async Task<List<QuestAnalyticsEntry>> GetQuestAnalyticsAsync(int eventId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
            SELECT
                q.Name AS QuestName,
                COUNT(CASE WHEN qm.Status = @ApprovedStatus THEN 1 END) AS CompletedCount
            FROM Quests q
            LEFT JOIN QuestMemories qm ON q.QuestId = qm.QuestId
            WHERE q.EventId = @EventId
            GROUP BY q.QuestId, q.Name
            ORDER BY CompletedCount DESC;";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
        command.Parameters.Add("@ApprovedStatus", SqlDbType.NVarChar).Value = StatisticsConstants.ApprovedStatus;

        var entries = new List<QuestAnalyticsEntry>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new QuestAnalyticsEntry
            {
                QuestName = (string)reader["QuestName"],
                TotalCompletedQuests = (int)reader["CompletedCount"],
            });
        }

        return entries;
    }

    /// <summary>
    /// Defines constant values used in the statistics calculations, such as weights for scoring, default tier names, status values for quests, and limits for leaderboard entries. These constants help maintain consistency across the repository and make it easier to adjust scoring criteria or other parameters in the future without modifying the core logic of the methods.
    /// </summary>
    private static class StatisticsConstants
    {
        public const int MemoryScoreWeight = 2;
        public const int QuestScoreWeight = 3;

        public const string DefaultTier = "Newcomer";
        public const string ApprovedStatus = "Approved";
        public const string RejectedStatus = "Rejected";

        public const int LeaderboardLimit = 100;
    }
}