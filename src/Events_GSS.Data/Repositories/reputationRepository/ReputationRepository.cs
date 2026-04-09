using Events_GSS.Data.Database;
using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.reputationRepository;

public class ReputationRepository : IReputationRepository
{
    private readonly SqlConnectionFactory factory;

    public ReputationRepository(SqlConnectionFactory factory)
    {
        this.factory = factory;
    }

    public async Task UpdateReputationAsync(int userId, int delta)
    {
        using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        var ensure = new SqlCommand(@"
            IF NOT EXISTS (SELECT 1 FROM users_RP_scores WHERE UserId = @UserId)
                INSERT INTO users_RP_scores (UserId) VALUES (@UserId);", conn);
        ensure.Parameters.AddWithValue("@UserId", userId);
        await ensure.ExecuteNonQueryAsync();

        var cmd = new SqlCommand(@"
            UPDATE r
            SET r.ReputationPoints = v.NewRP,
                r.Tier = CASE
                    WHEN v.NewRP >= 1000 THEN 'Event Master'
                    WHEN v.NewRP >= 500  THEN 'Community Leader'
                    WHEN v.NewRP >= 200  THEN 'Organizer'
                    WHEN v.NewRP >= 50   THEN 'Contributor'
                    ELSE 'Newcomer'
                END
            FROM users_RP_scores r
            CROSS APPLY (
                SELECT CASE
                    WHEN r.ReputationPoints + @Delta < -1000 THEN -1000
                    ELSE r.ReputationPoints + @Delta
                END AS NewRP
            ) v
            WHERE r.UserId = @UserId", conn);

        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Delta", delta);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetReputationPointsAsync(int userId)
    {
        using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            "SELECT ISNULL((SELECT ReputationPoints FROM users_RP_scores WHERE UserId = @UserId), 0)", conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        var result = await cmd.ExecuteScalarAsync();
        return result is int rp ? rp : 0;
    }

    public async Task<string> GetTierAsync(int userId)
    {
        using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            "SELECT ISNULL((SELECT Tier FROM users_RP_scores WHERE UserId = @UserId), 'Newcomer')", conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        var result = await cmd.ExecuteScalarAsync();
        return result as string ?? "Newcomer";
    }
}
