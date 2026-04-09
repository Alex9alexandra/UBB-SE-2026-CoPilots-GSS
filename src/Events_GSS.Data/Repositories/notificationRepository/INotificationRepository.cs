// <copyright file="INotificationRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.notificationRepository;

using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

/// <summary>
/// Defines the contract for the notification repository, which provides methods to manage and retrieve notifications for users in the system. This interface abstracts the data access layer for notifications, allowing for different implementations that can interact with various data sources or storage mechanisms. The methods include adding new notifications, retrieving notifications by user ID, and deleting notifications by their unique identifier.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Asynchronously adds a new notification to the data source. This method takes the user ID, title, description, and creation timestamp as parameters to create a new notification entry. The method returns a task that represents the asynchronous operation, allowing for non-blocking execution when adding notifications to the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom the notification is being created.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="description">The description or content of the notification.</param>
    /// <param name="createdAt">The timestamp indicating when the notification was created.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(int userId, string title, string description, DateTime createdAt);

    /// <summary>
    /// Asynchronously retrieves notifications for a specific user by their ID. This method returns a list of notifications associated with the given user ID, allowing for the retrieval of all notifications for a particular user.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve notifications.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of notifications for the specified user.</returns>
    Task<List<Notification>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Asynchronously deletes a notification by its unique identifier. This method removes the specified notification from the data source.
    /// </summary>
    /// <param name="notificationId">The ID of the notification to delete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(int notificationId);
}