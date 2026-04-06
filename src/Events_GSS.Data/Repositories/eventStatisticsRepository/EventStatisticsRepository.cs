using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.eventStatisticsRepository;

public class EventStatisticsRepository : IEventStatisticsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public EventStatisticsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ParticipantOverview> GetParticipantOverviewAsync(int eventId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            DECLARE @Total INT;
            DECLARE @Active INT;

            SELECT @Total = COUNT(*) FROM AttendedEvents WHERE EventId = @EventId;

            SELECT @Active = COUNT(DISTINCT UserId) FROM (
                SELECT UserId FROM Discussions WHERE EventId = @EventId
                UNION
                SELECT UserId FROM Memories WHERE EventId = @EventId
                UNION
                SELECT m.UserId
                FROM QuestMemories qm
                INNER JOIN Memories m ON qm.MemoryId = m.MemoryId
                INNER JOIN Quests q ON qm.QuestId = q.QuestId
                WHERE q.EventId = @EventId
            ) active;

            SELECT @Total AS TotalParticipants, @Active AS ActiveParticipants;";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            int total = (int)reader["TotalParticipants"];
            int active = (int)reader["ActiveParticipants"];
            return new ParticipantOverview
            {
                TotalParticipants = total,
                ActiveParticipants = active,
                EngagementRate = total > 0 ? Math.Round((double)active / total * 100, 2) : 0
            };
        }

        return new ParticipantOverview();
    }

    public async Task<EngagementBreakdown> GetEngagementBreakdownAsync(int eventId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            DECLARE @Messages INT;
            DECLARE @Memories INT;
            DECLARE @TotalSubmissions INT;
            DECLARE @Approved INT;
            DECLARE @Denied INT;

            SELECT @Messages = COUNT(*) FROM Discussions WHERE EventId = @EventId;
            SELECT @Memories = COUNT(*) FROM Memories WHERE EventId = @EventId;

            SELECT @TotalSubmissions = COUNT(*)
            FROM QuestMemories qm
            INNER JOIN Quests q ON qm.QuestId = q.QuestId
            WHERE q.EventId = @EventId;

            SELECT @Approved = COUNT(*)
            FROM QuestMemories qm
            INNER JOIN Quests q ON qm.QuestId = q.QuestId
            WHERE q.EventId = @EventId AND qm.Status = 'Approved';

            SELECT @Denied = COUNT(*)
            FROM QuestMemories qm
            INNER JOIN Quests q ON qm.QuestId = q.QuestId
            WHERE q.EventId = @EventId AND qm.Status = 'Rejected';

            SELECT @Messages AS TotalMessages,
                   @Memories AS TotalMemories,
                   @TotalSubmissions AS TotalSubmissions,
                   @Approved AS ApprovedQuests,
                   @Denied AS DeniedQuests;";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            int totalMessages = (int)reader["TotalMessages"];
            int totalMemories = (int)reader["TotalMemories"];
            int totalSubmissions = (int)reader["TotalSubmissions"];
            int approved = (int)reader["ApprovedQuests"];
            int denied = (int)reader["DeniedQuests"];

            double approvedRate = totalSubmissions > 0
                ? Math.Round((double)approved / totalSubmissions * 100, 2)
                : 0;

            return new EngagementBreakdown
            {
                TotalDiscussionMessages = totalMessages,
                TotalMemories = totalMemories,
                TotalQuestSubmissions = totalSubmissions,
                ApprovedQuests = approved,
                DeniedQuests = denied,
                ApprovedQuestsRate = approvedRate,
                DeniedQuestsRate = totalSubmissions > 0 ? Math.Round(100.0 - approvedRate, 2) : 0
            };
        }

        return new EngagementBreakdown();
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int eventId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            SELECT TOP 100
                u.Name AS UserName,
                ISNULL(urp.Tier, 'Newcomer') AS Tier,
                ISNULL(msg.cnt, 0) AS MessagesCount,
                ISNULL(mem.cnt, 0) AS MemoriesCount,
                ISNULL(qst.cnt, 0) AS QuestsCompleted,
                ISNULL(msg.cnt, 0) + (ISNULL(mem.cnt, 0) * 2) + (ISNULL(qst.cnt, 0) * 3) AS TotalScore
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
                WHERE q.EventId = @EventId AND qm.Status = 'Approved'
                GROUP BY m.UserId
            ) qst ON qst.UserId = u.Id
            WHERE ae.EventId = @EventId
            ORDER BY TotalScore DESC;";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        var entries = new List<LeaderboardEntry>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new LeaderboardEntry
            {
                UserName = (string)reader["UserName"],
                Tier = (string)reader["Tier"],
                MessagesCount = (int)reader["MessagesCount"],
                MemoriesCount = (int)reader["MemoriesCount"],
                QuestsCompleted = (int)reader["QuestsCompleted"],
                TotalScore = (int)reader["TotalScore"]
            });
        }

        return entries;
    }

    public async Task<List<QuestAnalyticsEntry>> GetQuestAnalyticsAsync(int eventId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            SELECT
                q.Name AS QuestName,
                COUNT(CASE WHEN qm.Status = 'Approved' THEN 1 END) AS CompletedCount
            FROM Quests q
            LEFT JOIN QuestMemories qm ON q.QuestId = qm.QuestId
            WHERE q.EventId = @EventId
            GROUP BY q.QuestId, q.Name
            ORDER BY CompletedCount DESC;";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        var entries = new List<QuestAnalyticsEntry>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new QuestAnalyticsEntry
            {
                QuestName = (string)reader["QuestName"],
                CompletedCount = (int)reader["CompletedCount"]
            });
        }

        return entries;
    }
}
