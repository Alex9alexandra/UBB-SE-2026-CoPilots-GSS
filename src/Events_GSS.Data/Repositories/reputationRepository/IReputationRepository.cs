// <copyright file="IReputationRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.reputationRepository;

/// <summary>
/// Defines methods for managing and retrieving user reputation points and tiers in the system.
/// </summary>
public interface IReputationRepository
{
    /// <summary>
    /// Asynchronously retrieves the reputation points for a specific user by their user ID.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve reputation points.</param>
    /// <returns>A task that represents the asynchronous operation, containing the reputation points of the specified user.</returns>
    Task<int> GetReputationPointsAsync(int userId);

    /// <summary>
    /// Asynchronously retrieves the tier for a specific user by their user ID.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve the tier.</param>
    /// <returns>A task that represents the asynchronous operation, containing the tier of the specified user.</returns>
    Task<string> GetTierAsync(int userId);

    /// <summary>
    /// Asynchronously sets the reputation points and tier for a specific user by their user ID.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to set the reputation points and tier.</param>
    /// <param name="reputationPoints">The reputation points to set for the user.</param>
    /// <param name="tier">The tier to set for the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetReputationAsync(int userId, int reputationPoints, string tier);
}
