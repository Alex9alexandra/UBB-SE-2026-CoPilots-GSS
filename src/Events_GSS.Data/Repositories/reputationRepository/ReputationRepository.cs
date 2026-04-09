using System.Data;

using Events_GSS.Data.Database;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.reputationRepository;

public class ReputationRepository : IReputationRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    public ReputationRepository(SqlConnectionFactory factory)
    {
        connectionFactory = factory;
    }

    public async Task SetReputationAsync(int userId, int reputationPoints, string tier)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(@"
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

    public async Task<int> GetReputationPointsAsync(int userId)
    {
        using var connection = connectionFactory.CreateConnection();
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

    public async Task<string> GetTierAsync(int userId)
    {
        using var connection = connectionFactory.CreateConnection();
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

public static class SharedReputationConstants
{
    public const string NewcomerTier = "Newcomer";
}
