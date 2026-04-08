using System.Data;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.achievementRepository;

public class AchievementRepository : IAchievementRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    public AchievementRepository(SqlConnectionFactory factory)
    {
        connectionFactory = factory;
    }

    private static class AchievementConstants
    {
        public const string ApprovedStatus = "Approved";

        public const int ProperHostThreshold = 3;
        public const int DistinguishedHostThreshold = 10;

        public const int QuestSolverThreshold = 25;
        public const int QuestMasterThreshold = 75;
        public const int QuestChampionThreshold = 150;

        public const int MemoryKeeperThreshold = 50;
        public const int SocialButterflyThreshold = 100;
        public const int EventVeteranThreshold = 10;
    }

    public async Task<List<Achievement>> GetUserAchievementsAsync(int userId)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(@"
            SELECT a.Id,
                   a.Title,
                   a.Description,
                   CASE WHEN ua.Id IS NOT NULL THEN 1 ELSE 0 END AS IsUnlocked
            FROM Achievements a
            LEFT JOIN UserAchievements ua
                   ON ua.AchievementId = a.Id AND ua.UserId = @UserId
            ORDER BY IsUnlocked DESC, a.Id", connection);

        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        var achievements = new List<Achievement>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            achievements.Add(new Achievement
            {
                AchievementId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                IsUnlocked = reader.GetInt32(3) == 1
            });
        }

        return achievements;
    }

    public async Task CheckAndAwardAchievementsAsync(int userId)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();

        int attendedEvents = await GetCountAsync(connection,
            "SELECT COUNT(*) FROM AttendedEvents WHERE UserId = @UserId", userId);

        int createdEvents = await GetCountAsync(connection,
            "SELECT COUNT(*) FROM Events WHERE AdminId = @UserId", userId);

        int approvedQuests = await GetCountAsync(connection, @"
            SELECT COUNT(*)
            FROM QuestMemories qm
            JOIN Memories m ON m.MemoryId = qm.MemoryId
            WHERE m.UserId = @UserId AND qm.Status = @Status",
            userId, ("@Status", AchievementConstants.ApprovedStatus));

        int memoriesWithPhotos = await GetCountAsync(connection,
            "SELECT COUNT(*) FROM Memories WHERE UserId = @UserId AND PhotoPath IS NOT NULL", userId);

        int messages = await GetCountAsync(connection,
            "SELECT COUNT(*) FROM Discussions WHERE UserId = @UserId", userId);

        bool hasPerfectEvent = await HasPerfectEventAsync(connection, userId);


        var achievements = await GetAllAchievementsAsync(connection);

        foreach (var achievement in achievements)
        {
            if (await IsAlreadyUnlocked(connection, userId, achievement.AchievementId))
                continue;

            if (IsConditionMet(achievement.Name, attendedEvents, createdEvents,
                approvedQuests, memoriesWithPhotos, messages, hasPerfectEvent))
            {
                await UnlockAchievement(connection, userId, achievement.AchievementId);
            }
        }

    }

    private bool IsConditionMet(string title,
        int attendedEvents,
        int createdEvents,
        int approvedQuests,
        int memoriesWithPhotos,
        int messages,
        bool hasPerfectEvent)
    {
        if (title == "First Steps")
            return attendedEvents >= 1;

        if (title == "Proper Host")
            return createdEvents >= AchievementConstants.ProperHostThreshold;

        if (title == "Distinguished Gentleperson")
            return createdEvents >= AchievementConstants.DistinguishedHostThreshold;

        if (title == "Quest Solver")
            return approvedQuests >= AchievementConstants.QuestSolverThreshold;

        if (title == "Quest Master")
            return approvedQuests >= AchievementConstants.QuestMasterThreshold;

        if (title == "Quest Champion")
            return approvedQuests >= AchievementConstants.QuestChampionThreshold;

        if (title == "Memory Keeper")
            return memoriesWithPhotos >= AchievementConstants.MemoryKeeperThreshold;

        if (title == "Social Butterfly")
            return messages >= AchievementConstants.SocialButterflyThreshold;

        if (title == "Event Veteran")
            return attendedEvents >= AchievementConstants.EventVeteranThreshold;

        if (title == "Perfectionist")
            return hasPerfectEvent;

        return false;
    }

    private async Task<int> GetCountAsync(SqlConnection connection, string query, int userId, params (string, object)[] extraParams)
    {
        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        foreach (var (name, value) in extraParams)
            command.Parameters.Add(name, SqlDbType.NVarChar).Value = value;

        var result = await command.ExecuteScalarAsync();
        return result is int count ? count : 0;
    }

    private async Task<bool> HasPerfectEventAsync(SqlConnection connection, int userId)
    {
        var command = new SqlCommand(@"
            SELECT 1
            FROM Events e
            JOIN AttendedEvents ae ON ae.EventId = e.EventId AND ae.UserId = @UserId
            WHERE EXISTS (SELECT 1 FROM Quests q WHERE q.EventId = e.EventId)
              AND NOT EXISTS (
                  SELECT 1 FROM Quests q
                  WHERE q.EventId = e.EventId
                    AND NOT EXISTS (
                        SELECT 1 FROM QuestMemories qm
                        JOIN Memories m ON m.MemoryId = qm.MemoryId
                        WHERE qm.QuestId = q.QuestId
                          AND m.UserId = @UserId
                          AND qm.Status = @Status
                    )
              )", connection);

        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@Status", SqlDbType.NVarChar).Value = AchievementConstants.ApprovedStatus;

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private async Task<List<Achievement>> GetAllAchievementsAsync(SqlConnection connection)
    {
        var command = new SqlCommand("SELECT Id, Title FROM Achievements", connection);

        var list = new List<Achievement>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new Achievement
            {
                AchievementId = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return list;
    }

    private async Task<bool> IsAlreadyUnlocked(SqlConnection connection, int userId, int achievementId)
    {
        var command = new SqlCommand(@"
            SELECT 1 FROM UserAchievements
            WHERE UserId = @UserId AND AchievementId = @AchievementId", connection);

        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@AchievementId", SqlDbType.Int).Value = achievementId;

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private async Task UnlockAchievement(SqlConnection connection, int userId, int achievementId)
    {
        var command = new SqlCommand(@"
            INSERT INTO UserAchievements (UserId, AchievementId)
            VALUES (@UserId, @AchievementId)", connection);

        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@AchievementId", SqlDbType.Int).Value = achievementId;

        await command.ExecuteNonQueryAsync();
    }
}
