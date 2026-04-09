// <copyright file="AchievementService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services.achievementServices;

using System.Data;
using Events_GSS.Data.Database;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.achievementRepository;
using Microsoft.Data.SqlClient;

/// <summary>
/// Implements the <see cref="IAchievementService"/> interface, providing methods to manage and retrieve user achievements in the system. This class interacts with the achievement repository to perform operations related to user achievements, allowing for checking and awarding achievements based on user activity and milestones. The service ensures that achievements are accurately tracked and awarded to users as they engage with the platform, supporting the gamification features of the application.
/// </summary>
public class AchievementService : IAchievementService
{
    private readonly IAchievementRepository repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AchievementService"/> class with the specified achievement repository. The repository is used to access and manage user achievements, allowing the service to check and award achievements based on user activity and milestones. This constructor ensures that the service has the necessary dependencies to perform its functions effectively, enabling it to interact with the underlying data storage for achievements.
    /// </summary>
    /// <param name="repository">The achievement repository used to access and manage user achievements.</param>
    public AchievementService(IAchievementRepository repository)
    {
        this.repository = repository;
    }

    /// <summary>
    /// Asynchronously retrieves the list of achievements for a specific user by their user ID. This method interacts with the achievement repository to fetch all achievements associated with the user, allowing the application to display the user's progress and unlocked achievements. The method ensures that the achievement data is retrieved efficiently, supporting the gamification features of the application.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve achievements.</param>
    /// <returns>A task that represents the asynchronous operation, containing the list of achievements for the specified user.</returns>
    public async Task<List<Achievement>> GetUserAchievementsAsync(int userId)
    {
        return await this.repository.GetAllAchievementsAsync();
    }

    /// <summary>
    /// Asynchronously checks and awards achievements for a specific user based on their activity and milestones. This method interacts with the achievement repository to determine which achievements the user has met the criteria for and awards them accordingly. The method ensures that achievements are accurately tracked and awarded, supporting the gamification features of the application.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to check and award achievements.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CheckAndAwardAchievementsAsync(int userId)
    {
        int attendedEvents = await this.repository.GetAttendedEventsCountAsync(userId);
        int createdEvents = await this.repository.GetCreatedEventsCountAsync(userId);
        int approvedQuests = await this.repository.GetApprovedQuestsCountAsync(userId);
        int memoriesWithPhotos = await this.repository.GetMemoriesWithPhotosCountAsync(userId);
        int messages = await this.repository.GetMessagesCountAsync(userId);
        bool hasPerfectEvent = await this.repository.HasPerfectEventAsync(userId);

        var achievements = await this.repository.GetAllAchievementsAsync();

        foreach (var achievement in achievements)
        {
            if (await this.repository.IsAlreadyUnlockedAsync(userId, achievement.AchievementId))
            {
                continue;
            }

            if (this.IsConditionMet(
                achievement.Name,
                attendedEvents,
                createdEvents,
                approvedQuests,
                memoriesWithPhotos,
                messages,
                hasPerfectEvent))
            {
                await this.repository.UnlockAchievementAsync(userId, achievement.AchievementId);
            }
        }
    }

    /// <summary>
    /// Determines whether the specified achievement conditions are met based on the user's activity and milestones. This method evaluates the criteria for each achievement, such as the number of events attended, created, quests approved, memories with photos, messages sent, and whether the user has a perfect event. By comparing these metrics against predefined thresholds for each achievement, the method determines if the user qualifies for unlocking a specific achievement. This logic is central to ensuring that achievements are awarded accurately based on user engagement with the platform.
    /// </summary>
    /// <param name="title">The title of the achievement to check.</param>
    /// <param name="attendedEvents">The number of events the user has attended.</param>
    /// <param name="createdEvents">The number of events the user has created.</param>
    /// <param name="approvedQuests">The number of quests the user has approved.</param>
    /// <param name="memoriesWithPhotos">The number of memories with photos the user has created.</param>
    /// <param name="messages">The number of messages the user has sent.</param>
    /// <param name="hasPerfectEvent">Indicates whether the user has a perfect event.</param>
    /// <returns>True if the achievement conditions are met; otherwise, false.</returns>
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

    /// <summary>
    /// Defines constant threshold values for various achievements in the system. These constants represent the criteria that users must meet to unlock specific achievements, such as the number of events attended, created, quests approved, memories with photos, messages sent, and whether they have a perfect event. By centralizing these threshold values in a static class, the application can easily manage and update achievement criteria without modifying the core logic of the achievement service.
    /// </summary>
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
}
