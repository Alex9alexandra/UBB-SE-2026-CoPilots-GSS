namespace Events_GSS.Data.Services.achievementServices;

using System.Data;
using Events_GSS.Data.Database;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.achievementRepository;
using Microsoft.Data.SqlClient;
public class AchievementService : IAchievementService
{
    private readonly IAchievementRepository _repository;

    public AchievementService(IAchievementRepository repository)
    {
        _repository = repository;
    }

    private static class AchievementConstants
    {
        public const int ProperHostThreshold = 3;
        public const int DistinguishedHostThreshold = 10;
        public const int QuestSolverThreshold = 25;
        public const int QuestMasterThreshold = 75;
        public const int QuestChampionThreshold = 150;
        public const int MemoryKeeperThreshold = 50;
        public const int SocialButterflyThreshold = 100;
        public const int EventVeteranThreshold = 10;
    }

    public async Task<List<Achievement>> GetUserAchievementsAsync(int userId)
    {
        return await _repository.GetAllAchievementsAsync();
    }

    public async Task CheckAndAwardAchievementsAsync(int userId)
    {
        int attendedEvents = await _repository.GetAttendedEventsCountAsync(userId);
        int createdEvents = await _repository.GetCreatedEventsCountAsync(userId);
        int approvedQuests = await _repository.GetApprovedQuestsCountAsync(userId);
        int memoriesWithPhotos = await _repository.GetMemoriesWithPhotosCountAsync(userId);
        int messages = await _repository.GetMessagesCountAsync(userId);
        bool hasPerfectEvent = await _repository.HasPerfectEventAsync(userId);

        var achievements = await _repository.GetAllAchievementsAsync();

        foreach (var achievement in achievements)
        {
            if (await _repository.IsAlreadyUnlockedAsync(userId, achievement.AchievementId))
                continue;

            if (IsConditionMet(
                achievement.Name,
                attendedEvents,
                createdEvents,
                approvedQuests,
                memoriesWithPhotos,
                messages,
                hasPerfectEvent))
            {
                await _repository.UnlockAchievementAsync(userId, achievement.AchievementId);
            }
        }
    }

    private bool IsConditionMet(
        string title,
        int attendedEvents,
        int createdEvents,
        int approvedQuests,
        int memoriesWithPhotos,
        int messages,
        bool hasPerfectEvent)
    {
        return title switch
        {
            "First Steps" => attendedEvents >= 1,
            "Proper Host" => createdEvents >= AchievementConstants.ProperHostThreshold,
            "Distinguished Gentleperson" => createdEvents >= AchievementConstants.DistinguishedHostThreshold,
            "Quest Solver" => approvedQuests >= AchievementConstants.QuestSolverThreshold,
            "Quest Master" => approvedQuests >= AchievementConstants.QuestMasterThreshold,
            "Quest Champion" => approvedQuests >= AchievementConstants.QuestChampionThreshold,
            "Memory Keeper" => memoriesWithPhotos >= AchievementConstants.MemoryKeeperThreshold,
            "Social Butterfly" => messages >= AchievementConstants.SocialButterflyThreshold,
            "Event Veteran" => attendedEvents >= AchievementConstants.EventVeteranThreshold,
            "Perfectionist" => hasPerfectEvent,
            _ => false
        };
    }
}
