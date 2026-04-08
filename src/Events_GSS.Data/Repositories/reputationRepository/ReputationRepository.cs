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

    public static class ReputationConstants
    {
        public const int MinReputation = -1000;

        public const int ContributorThreshold = 50;
        public const int OrganizerThreshold = 200;
        public const int CommunityLeaderThreshold = 500;
        public const int EventMasterThreshold = 1000;

        public const string NewcomerTier = "Newcomer";
        public const string ContributorTier = "Contributor";
        public const string OrganizerTier = "Organizer";
        public const string CommunityLeaderTier = "Community Leader";
        public const string EventMasterTier = "Event Master";
    }

    private string CalculateTier(int reputationPoints)
    {
        if (reputationPoints >= ReputationConstants.EventMasterThreshold)
            return ReputationConstants.EventMasterTier;

        if (reputationPoints >= ReputationConstants.CommunityLeaderThreshold)
            return ReputationConstants.CommunityLeaderTier;

        if (reputationPoints >= ReputationConstants.OrganizerThreshold)
            return ReputationConstants.OrganizerTier;

        if (reputationPoints >= ReputationConstants.ContributorThreshold)
            return ReputationConstants.ContributorTier;

        return ReputationConstants.NewcomerTier;
    }
    public async Task UpdateReputationAsync(int userId, int delta)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var ensureUserExistsCommand = new SqlCommand(@"
            IF NOT EXISTS (SELECT 1 FROM users_RP_scores WHERE UserId = @UserId)
                INSERT INTO users_RP_scores (UserId, ReputationPoints, Tier) 
                VALUES (@UserId, 0, @DefaultTier);", connection);

        ensureUserExistsCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        ensureUserExistsCommand.Parameters.Add("@DefaultTier", SqlDbType.NVarChar).Value = ReputationConstants.NewcomerTier;
        await ensureUserExistsCommand.ExecuteNonQueryAsync();

        int currentReputation = await GetReputationPointsAsync(userId);
        int newReputation = Math.Max(
            ReputationConstants.MinReputation,
            currentReputation + delta
        );

        string newTier = CalculateTier(newReputation);

        var updateReputationCommand = new SqlCommand(@"
            UPDATE users_RP_scores
            SET ReputationPoints = @NewRP,
                Tier = @Tier
            WHERE UserId = @UserId", connection);

        updateReputationCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        updateReputationCommand.Parameters.Add("@NewRP", SqlDbType.Int).Value = newReputation;
        updateReputationCommand.Parameters.Add("@Tier", SqlDbType.NVarChar).Value = newTier;

        await updateReputationCommand.ExecuteNonQueryAsync();
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
        command.Parameters.Add("@DefaultTier", SqlDbType.NVarChar).Value = ReputationConstants.NewcomerTier;

        var result = await command.ExecuteScalarAsync();
        return result as string ?? ReputationConstants.NewcomerTier;
    }
}
