using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories.achievementRepository;

public interface IAchievementRepository
{
    Task<List<Achievement>> GetUserAchievementsAsync(int userId);
    Task CheckAndAwardAchievementsAsync(int userId);
}
