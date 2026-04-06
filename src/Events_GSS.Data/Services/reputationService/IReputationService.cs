using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.reputationService;

public interface IReputationService
{
    Task<int> GetReputationPointsAsync(int userId);
    Task<string> GetTierAsync(int userId);
    Task<List<Achievement>> GetAchievementsAsync(int userId);

    Task<bool> CanPostMemoriesAsync(int userId);
    Task<bool> CanPostMessagesAsync(int userId);
    Task<bool> CanCreateEventsAsync(int userId);
    Task<bool> CanAttendEventsAsync(int userId);
}
