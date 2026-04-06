using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.achievementRepository;

public class AchievementRepository : IAchievementRepository
{
    private readonly SqlConnectionFactory _factory;

    public AchievementRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Achievement>> GetUserAchievementsAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
            SELECT a.Id,
                   a.Title,
                   a.Description,
                   CASE WHEN ua.Id IS NOT NULL THEN 1 ELSE 0 END AS IsUnlocked
            FROM Achievements a
            LEFT JOIN UserAchievements ua
                   ON ua.AchievementId = a.Id AND ua.UserId = @UserId
            ORDER BY IsUnlocked DESC, a.Id", conn);

        cmd.Parameters.AddWithValue("@UserId", userId);

        var achievements = new List<Achievement>();
        using var reader = await cmd.ExecuteReaderAsync();
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
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        // Insert every achievement whose condition is now met and hasn't been awarded yet.
        // Conditions are matched by Title so the logic is independent of IDENTITY seed order.
        var cmd = new SqlCommand(@"
            INSERT INTO UserAchievements (UserId, AchievementId)
            SELECT @UserId, a.Id
            FROM Achievements a
            WHERE NOT EXISTS (
                SELECT 1 FROM UserAchievements ua
                WHERE ua.UserId = @UserId AND ua.AchievementId = a.Id
            )
            AND (
                -- First Steps: attended at least 1 event
                (a.Title = 'First Steps'
                    AND EXISTS (SELECT 1 FROM AttendedEvents WHERE UserId = @UserId))

                -- Proper Host: created 3+ events
                OR (a.Title = 'Proper Host'
                    AND (SELECT COUNT(*) FROM Events WHERE AdminId = @UserId) >= 3)

                -- Distinguished Gentleperson: created 10+ events
                OR (a.Title = 'Distinguished Gentleperson'
                    AND (SELECT COUNT(*) FROM Events WHERE AdminId = @UserId) >= 10)

                -- Quest Solver: 25+ approved quest submissions
                OR (a.Title = 'Quest Solver'
                    AND (SELECT COUNT(*) FROM QuestMemories qm
                         JOIN Memories m ON m.MemoryId = qm.MemoryId
                         WHERE m.UserId = @UserId AND qm.Status = 'Approved') >= 25)

                -- Quest Master: 75+ approved quest submissions
                OR (a.Title = 'Quest Master'
                    AND (SELECT COUNT(*) FROM QuestMemories qm
                         JOIN Memories m ON m.MemoryId = qm.MemoryId
                         WHERE m.UserId = @UserId AND qm.Status = 'Approved') >= 75)

                -- Quest Champion: 150+ approved quest submissions
                OR (a.Title = 'Quest Champion'
                    AND (SELECT COUNT(*) FROM QuestMemories qm
                         JOIN Memories m ON m.MemoryId = qm.MemoryId
                         WHERE m.UserId = @UserId AND qm.Status = 'Approved') >= 150)

                -- Memory Keeper: 50+ memories with photos
                OR (a.Title = 'Memory Keeper'
                    AND (SELECT COUNT(*) FROM Memories
                         WHERE UserId = @UserId AND PhotoPath IS NOT NULL) >= 50)

                -- Social Butterfly: 100+ discussion messages
                OR (a.Title = 'Social Butterfly'
                    AND (SELECT COUNT(*) FROM Discussions WHERE UserId = @UserId) >= 100)

                -- Event Veteran: attended 10+ different events
                OR (a.Title = 'Event Veteran'
                    AND (SELECT COUNT(*) FROM AttendedEvents WHERE UserId = @UserId) >= 10)

                -- Perfectionist: 100% quest completion in at least one attended event
                OR (a.Title = 'Perfectionist'
                    AND EXISTS (
                        SELECT 1 FROM Events e
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
                                      AND qm.Status = 'Approved'
                                )
                          )
                    ))
            )", conn);

        cmd.Parameters.AddWithValue("@UserId", userId);
        await cmd.ExecuteNonQueryAsync();
    }
}
