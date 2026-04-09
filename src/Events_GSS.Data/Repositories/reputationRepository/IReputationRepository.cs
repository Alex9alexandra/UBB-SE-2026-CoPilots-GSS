namespace Events_GSS.Data.Repositories.reputationRepository;

public interface IReputationRepository
{
    Task<int> GetReputationPointsAsync(int userId);

    Task<string> GetTierAsync(int userId);

    Task SetReputationAsync(int userId, int reputationPoints, string tier);
}
