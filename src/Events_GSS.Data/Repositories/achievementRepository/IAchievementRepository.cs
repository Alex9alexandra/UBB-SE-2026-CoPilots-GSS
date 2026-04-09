using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.achievementRepository;

public interface IAchievementRepository
{
    Task<int> GetAttendedEventsCountAsync(int userId);
    Task<int> GetCreatedEventsCountAsync(int userId);
    Task<int> GetApprovedQuestsCountAsync(int userId);
    Task<int> GetMemoriesWithPhotosCountAsync(int userId);
    Task<int> GetMessagesCountAsync(int userId);
    Task<bool> HasPerfectEventAsync(int userId);

    Task<List<Achievement>> GetAllAchievementsAsync();
    Task<bool> IsAlreadyUnlockedAsync(int userId, int achievementId);
    Task UnlockAchievementAsync(int userId, int achievementId);
}
