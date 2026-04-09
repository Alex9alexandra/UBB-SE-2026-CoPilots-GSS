namespace Events_GSS.Data.Repositories.achievementRepository;

using System.Data;
using Events_GSS.Data.Database;
using Events_GSS.Data.Models;
using Microsoft.Data.SqlClient;
public class AchievementRepository : IAchievementRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AchievementRepository(SqlConnectionFactory factory)
    {
        _connectionFactory = factory;
    }

    public async Task<int> GetAttendedEventsCountAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT COUNT(*) FROM AttendedEvents WHERE UserId = @UserId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        return (int)await command.ExecuteScalarAsync();
    }

    public async Task<int> GetCreatedEventsCountAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT COUNT(*) FROM Events WHERE AdminId = @UserId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        return (int)await command.ExecuteScalarAsync();
    }

    public async Task<int> GetApprovedQuestsCountAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(@"
                SELECT COUNT(*)
                FROM QuestMemories qm
                JOIN Memories m ON m.MemoryId = qm.MemoryId
                WHERE m.UserId = @UserId AND qm.Status = @Status",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Status", "Approved");

        return (int)await command.ExecuteScalarAsync();
    }

    public async Task<int> GetMemoriesWithPhotosCountAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT COUNT(*) FROM Memories WHERE UserId = @UserId AND PhotoPath IS NOT NULL",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        return (int)await command.ExecuteScalarAsync();
    }

    public async Task<int> GetMessagesCountAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT COUNT(*) FROM Discussions WHERE UserId = @UserId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        return (int)await command.ExecuteScalarAsync();
    }

    public async Task<bool> HasPerfectEventAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

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
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Status", "Approved");

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<List<Achievement>> GetAllAchievementsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand("SELECT Id, Title FROM Achievements", connection);

        var achievements = new List<Achievement>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            achievements.Add(new Achievement
            {
                AchievementId = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }
        return achievements;
    }

    public async Task<bool> IsAlreadyUnlockedAsync(int userId, int achievementId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT 1 FROM UserAchievements WHERE UserId=@UserId AND AchievementId=@AchievementId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@AchievementId", achievementId);

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    public async Task UnlockAchievementAsync(int userId, int achievementId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "INSERT INTO UserAchievements(UserId, AchievementId) VALUES(@UserId, @AchievementId)",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@AchievementId", achievementId);

        await command.ExecuteNonQueryAsync();
    }
}
