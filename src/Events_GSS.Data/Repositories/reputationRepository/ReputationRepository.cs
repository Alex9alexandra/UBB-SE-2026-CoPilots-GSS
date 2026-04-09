// <copyright file="ReputationRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.reputationRepository;

using System.Net.NetworkInformation;

using System.Data;

using Events_GSS.Data.Database;

using Microsoft.Data.SqlClient;

/// <summary>
/// Implements the <see cref="IReputationRepository"/> interface, providing methods to manage and retrieve user reputation points and tiers in the system. This class interacts with a SQL database to perform operations related to user reputation, allowing for setting and retrieving reputation points and tiers based on user activity and achievements. The repository ensures that reputation data is stored and accessed efficiently, supporting the gamification features of the application.
/// </summary>
public class ReputationRepository : IReputationRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReputationRepository"/> class with the specified SQL connection factory. The connection factory is used to create database connections for executing SQL commands related to user reputation management.
    /// </summary>
    /// <param name="factory">The SQL connection factory used to create database connections.</param>
    public ReputationRepository(SqlConnectionFactory factory)
    {
        this.connectionFactory = factory;
    }

    /// <summary>
    /// Asynchronously sets the reputation points and tier for a specific user by their user ID. This method checks if a record for the user already exists in the users_RP_scores table; if it does not exist, it inserts a new record with the provided reputation points and tier. If a record already exists, it updates the existing record with the new reputation points and tier. This ensures that the user's reputation data is accurately maintained in the database, allowing for dynamic updates based on user activity and achievements.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to set the reputation points and tier.</param>
    /// <param name="reputationPoints">The reputation points to set for the user.</param>
    /// <param name="tier">The tier to set for the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetReputationAsync(int userId, int reputationPoints, string tier)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            @"
        IF NOT EXISTS (SELECT 1 FROM users_RP_scores WHERE UserId = @UserId)
            INSERT INTO users_RP_scores (UserId, ReputationPoints, Tier) 
            VALUES (@UserId, @ReputationPoints, @Tier)
        ELSE
            UPDATE users_RP_scores
            SET ReputationPoints = @ReputationPoints,
                Tier = @Tier
            WHERE UserId = @UserId", connection);

        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@ReputationPoints", SqlDbType.Int).Value = reputationPoints;
        command.Parameters.Add("@Tier", SqlDbType.NVarChar).Value = tier;

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Asynchronously retrieves the reputation points for a specific user by their user ID. This method executes an SQL SELECT command to fetch the reputation points from the users_RP_scores table based on the provided user ID. If the user does not have a record in the users_RP_scores table, it returns 0 as the default reputation points, ensuring that users without any recorded reputation are treated as having no points. This allows for efficient retrieval of a user's reputation points, which can be used to determine their tier and access to certain features in the application.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve reputation points.</param>
    /// <returns>A task that represents the asynchronous operation, containing the reputation points of the specified user.</returns>
    public async Task<int> GetReputationPointsAsync(int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            @"
            SELECT ISNULL(ReputationPoints, 0)
            FROM users_RP_scores
            WHERE UserId = @UserId", connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        var result = await command.ExecuteScalarAsync();
        return result is int reputation ? reputation : 0;
    }

    /// <summary>
    /// Asynchronously retrieves the tier for a specific user by their user ID. This method executes an SQL SELECT command to fetch the tier from the users_RP_scores table based on the provided user ID. If the user does not have a record in the users_RP_scores table, it returns "Newcomer" as the default tier, ensuring that users without any recorded reputation are treated as newcomers. This allows for efficient retrieval of a user's tier, which can be used to determine their access to certain features and benefits in the application based on their reputation level.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve the tier.</param>
    /// <returns>A task that represents the asynchronous operation, containing the tier of the specified user.</returns>
    public async Task<string> GetTierAsync(int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            @"
            SELECT ISNULL(Tier, @DefaultTier)
            FROM users_RP_scores
            WHERE UserId = @UserId", connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@DefaultTier", SqlDbType.NVarChar).Value = SharedReputationConstants.NewcomerTier;

        var result = await command.ExecuteScalarAsync();
        return result as string ?? SharedReputationConstants.NewcomerTier;
    }
}

/// <summary>
/// Defines shared constants for the reputation system, such as default tier names. This class provides a centralized location for any constant values related to user reputation, ensuring consistency across the application when referring to specific tiers or other fixed values in the reputation management logic.
/// </summary>
public static class SharedReputationConstants
{
    /// <summary>
    /// The default tier assigned to users who do not have any recorded reputation points. This constant is used as a fallback value when retrieving a user's tier from the database, ensuring that users without any reputation data are categorized as "Newcomer" by default. This allows for a clear distinction between users who have not yet earned any reputation and those who have achieved higher tiers based on their activity and contributions in the application.
    /// </summary>
    public const string NewcomerTier = "Newcomer";
}
