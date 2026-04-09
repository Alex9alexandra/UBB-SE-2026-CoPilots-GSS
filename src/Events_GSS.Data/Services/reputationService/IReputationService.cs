// <copyright file="IReputationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services.reputationService;

using Events_GSS.Data.Models;

/// <summary>
/// Defines the contract for the reputation service, which provides methods to manage and retrieve user reputation points, tiers, achievements, and permissions related to posting memories, messages, creating events, and attending events. This interface abstracts the business logic layer for user reputation management, allowing for different implementations that can interact with various data sources or storage mechanisms to calculate and maintain user reputation in the system.
/// </summary>
public interface IReputationService
{
    /// <summary>
    /// Asynchronously retrieves the reputation points for a specific user by their user ID. This method interacts with the underlying data repository to fetch the current reputation points of the specified user, which can be used to determine their tier, achievements, and permissions within the system. The reputation points are typically calculated based on user activity, contributions, and engagement in various events and interactions within the application.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve reputation points.</param>
    /// <returns>A task that represents the asynchronous operation, containing the reputation points of the specified user.</returns>
    Task<int> GetReputationPointsAsync(int userId);

    /// <summary>
    /// Asynchronously retrieves the tier for a specific user by their user ID. This method interacts with the underlying data repository to fetch the current tier of the specified user, which is typically determined based on their reputation points and achievements. The tier can be used to grant access to certain features, privileges, or rewards within the system, and it serves as an indicator of the user's standing and engagement level in the application.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve the tier.</param>
    /// <returns>A task that represents the asynchronous operation, containing the tier of the specified user.</returns>
    Task<string> GetTierAsync(int userId);

    /// <summary>
    /// Asynchronously retrieves the achievements for a specific user by their user ID. This method interacts with the underlying data repository to fetch the current achievements of the specified user, which can be used to determine their reputation points, tier, and permissions within the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve achievements.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of achievements for the specified user.</returns>
    Task<List<Achievement>> GetAchievementsAsync(int userId);

    /// <summary>
    /// Asynchronously determines whether a specific user can post memories based on their reputation points, tier, and achievements. This method interacts with the underlying data repository to fetch the necessary information and evaluate the user's permissions.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to check the permission.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean value indicating whether the user can post memories.</returns>
    Task<bool> CanPostMemoriesAsync(int userId);

    /// <summary>
    /// Asynchronously determines whether a specific user can post messages based on their reputation points, tier, and achievements. This method interacts with the underlying data repository to fetch the necessary information and evaluate the user's permissions.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to check the permission.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean value indicating whether the user can post messages.</returns>
    Task<bool> CanPostMessagesAsync(int userId);

    /// <summary>
    /// Asynchronously determines whether a specific user can create events based on their reputation points, tier, and achievements. This method interacts with the underlying data repository to fetch the necessary information and evaluate the user's permissions.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to check the permission.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean value indicating whether the user can create events.</returns>
    Task<bool> CanCreateEventsAsync(int userId);

    /// <summary>
    /// Asynchronously determines whether a specific user can attend events based on their reputation points, tier, and achievements. This method interacts with the underlying data repository to fetch the necessary information and evaluate the user's permissions.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to check the permission.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean value indicating whether the user can attend events.</returns>
    Task<bool> CanAttendEventsAsync(int userId);
}
