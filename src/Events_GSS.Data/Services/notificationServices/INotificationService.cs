// <copyright file="INotificationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services.notificationServices;
using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

/// <summary>
/// Defines the contract for the notification service, which provides methods to manage and retrieve notifications for users in the system. This interface abstracts the business logic layer for notifications, allowing for different implementations that can interact with various data sources or storage mechanisms. The methods include notifying users with new notifications, retrieving notifications by user ID, and deleting notifications by their unique identifier.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Asynchronously sends a notification to a specific user by their user ID, including a title and description for the notification. This method abstracts the process of creating and storing a notification for the user, allowing for non-blocking execution when sending notifications in the system. The implementation of this method would typically involve creating a new notification entry in the data source and associating it with the specified user ID, ensuring that users receive timely updates and information through notifications.
    /// </summary>
    /// <param name="userId">The ID of the user to whom the notification will be sent.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="description">The description or content of the notification.</param>
    /// <returns>A task that represents the asynchronous operation of sending the notification.</returns>
    Task NotifyAsync(int userId, string title, string description);

    /// <summary>
    /// Asynchronously retrieves a list of notifications for a specific user by their user ID. This method allows for non-blocking execution when fetching notifications, enabling the system to efficiently retrieve and display notifications to users without causing delays or performance issues. The implementation of this method would typically involve querying the data source for notifications associated with the specified user ID and returning them as a list, allowing users to view their notifications in a timely manner.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve notifications.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of notifications for the specified user.</returns>
    Task<List<Notification>> GetNotificationsAsync(int userId);

    /// <summary>
    /// Asynchronously deletes a notification by its unique identifier. This method allows for non-blocking execution when removing notifications from the system, enabling users to manage their notifications efficiently without causing delays or performance issues. The implementation of this method would typically involve deleting the specified notification from the data source based on its unique identifier, ensuring that users can effectively manage and organize their notifications as needed.
    /// </summary>
    /// <param name="notificationId">The unique identifier of the notification to be deleted.</param>
    /// <returns>A task that represents the asynchronous operation of deleting the notification.</returns>
    Task DeleteAsync(int notificationId);
}
