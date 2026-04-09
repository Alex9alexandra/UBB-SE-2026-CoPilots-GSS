// <copyright file="IAchievementRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.achievementRepository;

using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

/// <summary>
/// Defines the contract for the achievement repository, which provides methods to manage and retrieve user achievements in the system.
/// </summary>
public interface IAchievementRepository
{
    /// <summary>
    /// Asynchronously retrieves the total number of events attended by the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose attended event count is to be retrieved. Must be a valid user ID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of events the user has
    /// attended.</returns>
    Task<int> GetAttendedEventsCountAsync(int userId);

    /// <summary>
    /// Asynchronously retrieves the number of events created by the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose created events are to be counted. Must be a valid user ID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total number of events created
    /// by the user.</returns>
    Task<int> GetCreatedEventsCountAsync(int userId);

    /// <summary>
    /// Asynchronously retrieves the number of approved quests for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose approved quests are to be counted.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of approved quests for
    /// the user.</returns>
    Task<int> GetApprovedQuestsCountAsync(int userId);

    /// <summary>
    /// Asynchronously retrieves the number of memories that contain photos for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose memories with photos are to be counted. Must be a valid user ID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of memories with photos
    /// for the specified user.</returns>
    Task<int> GetMemoriesWithPhotosCountAsync(int userId);

    /// <summary>
    /// Asynchronously retrieves the total number of messages associated with the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose message count is to be retrieved. Must be a non-negative integer.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total number of messages for the
    /// specified user.</returns>
    Task<int> GetMessagesCountAsync(int userId);

    /// <summary>
    /// Determines whether the specified user has at least one event with a perfect score.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check for perfect events.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the user
    /// has a perfect event; otherwise, <see langword="false"/>.</returns>
    Task<bool> HasPerfectEventAsync(int userId);

    /// <summary>
    /// Asynchronously retrieves a list of all available achievements.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Achievement"/>
    /// objects representing all achievements. The list will be empty if no achievements are available.</returns>
    Task<List<Achievement>> GetAllAchievementsAsync();

    /// <summary>
    /// Determines asynchronously whether the specified user has already unlocked the given achievement.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check.</param>
    /// <param name="achievementId">The unique identifier of the achievement to verify.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the user
    /// has already unlocked the achievement; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsAlreadyUnlockedAsync(int userId, int achievementId);

    /// <summary>
    /// Asynchronously unlocks the specified achievement for the given user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user for whom the achievement will be unlocked. Must be a valid user ID.</param>
    /// <param name="achievementId">The unique identifier of the achievement to unlock. Must correspond to an existing achievement.</param>
    /// <returns>A task that represents the asynchronous unlock operation.</returns>
    Task UnlockAchievementAsync(int userId, int achievementId);
}
