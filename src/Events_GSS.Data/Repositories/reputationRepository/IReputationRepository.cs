namespace Events_GSS.Data.Repositories.reputationRepository;

public interface IReputationRepository
{
    Task UpdateReputationAsync(int userId, int delta);
    Task<int> GetReputationPointsAsync(int userId);
    Task<string> GetTierAsync(int userId);
}
