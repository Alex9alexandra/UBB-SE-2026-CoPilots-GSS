// <copyright file="AchievementRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.achievementRepository;

using System.Data;
using Events_GSS.Data.Database;
using Events_GSS.Data.Models;
using Microsoft.Data.SqlClient;

/// <summary>
/// Provides methods for managing user achievements, including retrieving achievement counts and unlocking achievements.
/// </summary>
public class AchievementRepository : IAchievementRepository
{
    /// <summary>
    /// Provides access to the factory used for creating SQL database connections.
    /// </summary>
    /// <remarks>This field is typically used to obtain new instances of SQL connections for database
    /// operations. The specific implementation of the factory may affect connection pooling and resource
    /// management.</remarks>
    private readonly SqlConnectionFactory connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AchievementRepository"/> class using the specified SQL connection factory.
    /// </summary>
    /// <param name="factory">The SqlConnectionFactory used to create database connections for repository operations. Cannot be null.</param>
    public AchievementRepository(SqlConnectionFactory factory)
    {
        this.connectionFactory = factory;
    }

    /// <summary>
    /// Asynchronously retrieves the total number of events attended by the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose attended events count is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of events attended by the
    /// user.</returns>
    public async Task<int> GetAttendedEventsCountAsync(int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT COUNT(*) FROM AttendedEvents WHERE UserId = @UserId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        return (int)await command.ExecuteScalarAsync();
    }

    /// <summary>
    /// Asynchronously retrieves the number of events created by the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose created events are to be counted.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of events created by the
    /// specified user.</returns>
    public async Task<int> GetCreatedEventsCountAsync(int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT COUNT(*) FROM Events WHERE AdminId = @UserId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        return (int)await command.ExecuteScalarAsync();
    }

    /// <summary>
    /// Asynchronously retrieves the number of approved quests associated with the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose approved quests are to be counted.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of approved quests for
    /// the specified user.</returns>
    public async Task<int> GetApprovedQuestsCountAsync(int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            @"
                SELECT COUNT(*)
                FROM QuestMemories qm
                JOIN Memories m ON m.MemoryId = qm.MemoryId
                WHERE m.UserId = @UserId AND qm.Status = @Status",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Status", "Approved");

        return (int)await command.ExecuteScalarAsync();
    }

    /// <summary>
    /// Asynchronously retrieves the number of memories that have associated photos for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose memories with photos are to be counted.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of memories with photos
    /// for the specified user.</returns>
    public async Task<int> GetMemoriesWithPhotosCountAsync(int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT COUNT(*) FROM Memories WHERE UserId = @UserId AND PhotoPath IS NOT NULL",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        return (int)await command.ExecuteScalarAsync();
    }

    /// <summary>
    /// Asynchronously retrieves the total number of discussion messages associated with the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose discussion message count is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of discussion messages
    /// for the specified user.</returns>
    public async Task<int> GetMessagesCountAsync(int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT COUNT(*) FROM Discussions WHERE UserId = @UserId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        return (int)await command.ExecuteScalarAsync();
    }

    /// <summary>
    /// Determines whether the specified user has attended an event where all associated quests have approved memories.
    /// </summary>
    /// <remarks>A perfect event is defined as one in which the user has attended and all quests for that
    /// event have at least one approved memory submitted by the user.</remarks>
    /// <param name="userId">The unique identifier of the user to check for perfect event attendance.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the user
    /// has attended at least one event where every quest has an approved memory; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> HasPerfectEventAsync(int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            @"
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

    /// <summary>
    /// Asynchronously retrieves all achievements from the data store.
    /// </summary>
    /// <remarks>This method opens a database connection and executes a query to retrieve all achievements.
    /// The caller is responsible for handling any exceptions that may occur during database access.</remarks>
    /// <returns>A list of <see cref="Achievement"/> objects representing all achievements. The list is empty if no achievements
    /// are found.</returns>
    public async Task<List<Achievement>> GetAllAchievementsAsync()
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand("SELECT Id, Title FROM Achievements", connection);

        var achievements = new List<Achievement>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            achievements.Add(new Achievement
            {
                AchievementId = reader.GetInt32(0),
                Name = reader.GetString(1),
            });
        }

        return achievements;
    }

    /// <summary>
    /// Determines whether the specified achievement has already been unlocked by the given user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose achievements are being checked.</param>
    /// <param name="achievementId">The unique identifier of the achievement to check for unlock status.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the
    /// achievement has already been unlocked by the user; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> IsAlreadyUnlockedAsync(int userId, int achievementId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT 1 FROM UserAchievements WHERE UserId=@UserId AND AchievementId=@AchievementId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@AchievementId", achievementId);

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    /// <summary>
    /// Asynchronously unlocks the specified achievement for a user by recording it in the database.
    /// </summary>
    /// <remarks>This method adds a record to the UserAchievements table to indicate that the specified user
    /// has unlocked the specified achievement. The operation is performed asynchronously and requires an open database
    /// connection.</remarks>
    /// <param name="userId">The unique identifier of the user for whom the achievement will be unlocked.</param>
    /// <param name="achievementId">The unique identifier of the achievement to unlock for the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UnlockAchievementAsync(int userId, int achievementId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            "INSERT INTO UserAchievements(UserId, AchievementId) VALUES(@UserId, @AchievementId)",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@AchievementId", achievementId);

        await command.ExecuteNonQueryAsync();
    }
}
