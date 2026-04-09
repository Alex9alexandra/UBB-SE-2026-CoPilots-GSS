// <copyright file="NotificationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services.notificationServices;
using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.notificationRepository;

/// <summary>
/// Implements the <see cref="INotificationService"/> interface, providing methods to manage and retrieve notifications for users in the system. This class serves as a service layer that interacts with the notification repository to perform operations such as adding new notifications, retrieving notifications by user ID, and deleting notifications by their unique identifier. The service layer abstracts the underlying data access logic, allowing for separation of concerns and easier maintenance of the notification-related functionality in the application.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository notificationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class with the specified notification repository. The repository is injected into the service, allowing for dependency inversion and enabling the service to interact with the data access layer for managing notifications. This constructor sets up the necessary dependencies for the service to function properly, ensuring that it can perform its operations related to notifications effectively.
    /// </summary>
    /// <param name="notificationRepository">The notification repository to be used by the service.</param>
    public NotificationService(INotificationRepository notificationRepository)
    {
        this.notificationRepository = notificationRepository;
    }

    /// <summary>
    /// Asynchronously adds a new notification for a specific user. This method takes the user ID, title, and description as parameters to create a new notification entry. It uses the notification repository to add the notification to the data source, setting the creation timestamp to the current UTC time. This allows for non-blocking execution when adding notifications to the system, ensuring that users can receive timely updates and information through notifications.
    /// </summary>
    /// <param name="userId">The ID of the user to whom the notification will be sent.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="description">The description or content of the notification.</param>
    /// <returns>A task that represents the asynchronous operation of sending the notification.</returns>
    public async Task NotifyAsync(int userId, string title, string description)
    {
        await this.notificationRepository.AddAsync(
            userId,
            title,
            description,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Asynchronously retrieves a list of notifications for a specific user based on their user ID. This method interacts with the notification repository to fetch all notifications associated with the given user ID, allowing users to view their notifications in the application. The retrieved notifications are returned as a list, enabling users to access and manage their notifications effectively within the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve notifications.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of notifications for the specified user.</returns>
    public async Task<List<Notification>> GetNotificationsAsync(int userId)
    {
        return await this.notificationRepository.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Asynchronously deletes a notification based on its unique identifier. This method uses the notification repository to remove the specified notification from the data source, allowing users to manage their notifications by deleting those that are no longer relevant or needed. The operation is performed asynchronously to ensure that it does not block the execution of other tasks in the application, providing a smooth user experience when managing notifications.
    /// </summary>
    /// <param name="notificationId">The ID of the notification to be deleted.</param>
    /// <returns>A task that represents the asynchronous operation of deleting the notification.</returns>
    public async Task DeleteAsync(int notificationId)
    {
        await this.notificationRepository.DeleteAsync(notificationId);
    }
}
