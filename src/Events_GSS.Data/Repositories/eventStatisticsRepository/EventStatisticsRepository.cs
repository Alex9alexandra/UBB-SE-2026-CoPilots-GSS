// <copyright file="EventStatisticsRepository.cs" company="UBB-SE-2026-GSS">
// Copyright (c) UBB-SE-2026-GSS. All rights reserved.
// </copyright>

using System.Data;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.eventStatisticsRepository;

public class EventStatisticsRepository : IEventStatisticsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;


    private static class StatisticsConstants
    {
        public const int MemoryScoreWeight = 2;
        public const int QuestScoreWeight = 3;

        public const string DefaultTier = "Newcomer";
        public const string ApprovedStatus = "Approved";
        public const string RejectedStatus = "Rejected";

        public const int LeaderboardLimit = 100;
    }

    public EventStatisticsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ParticipantOverview> GetParticipantOverviewAsync(int eventId)
    {
        using var connection = _connectionFactory.CreateConnection();
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

    public async Task<EngagementBreakdown> GetEngagementBreakdownAsync(int eventId)
    {
        using var connection = _connectionFactory.CreateConnection();
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

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int eventId)
    {
        using var connection = _connectionFactory.CreateConnection();
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
                TotalScore = (int)reader["TotalScore"]
            });
        }

        return entries;
    }

    public async Task<List<QuestAnalyticsEntry>> GetQuestAnalyticsAsync(int eventId)
    {
        using var connection = _connectionFactory.CreateConnection();
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
                TotalCompletedQuests = (int)reader["CompletedCount"]
            });
        }

        return entries;
    }
}