// <copyright file="IAchievementService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services.achievementServices;

using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

/// <summary>
/// Defines the contract for the achievement service, which provides methods to manage and retrieve user achievements in the system. This interface abstracts the business logic layer for achievements, allowing for different implementations that can interact with various data sources or storage mechanisms. The methods include retrieving a list of achievements for a specific user and checking and awarding achievements based on user actions or milestones.
/// </summary>
public interface IAchievementService
{
    /// <summary>
    /// Retrieves a list of achievements for a specific user. This method takes the user's ID as a parameter and returns a list of Achievement objects that represent the achievements the user has earned or is eligible for. The implementation of this method may involve querying a database or an external service to gather the relevant achievement data for the user.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve achievements.</param>
    /// <returns>A task that represents the asynchronous operation, containing the list of achievements for the specified user.</returns>
    Task<List<Achievement>> GetUserAchievementsAsync(int userId);

    /// <summary>
    /// Checks and awards achievements for a specific user based on their activity and milestones. This method evaluates the user's actions and determines if they qualify for any new achievements, awarding them accordingly.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to check and award achievements.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CheckAndAwardAchievementsAsync(int userId);
}
