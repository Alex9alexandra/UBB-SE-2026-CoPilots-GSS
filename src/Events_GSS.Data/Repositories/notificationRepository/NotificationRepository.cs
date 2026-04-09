// <copyright file="NotificationRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.notificationRepository;

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

/// <summary>
/// Implements the <see cref="INotificationRepository"/> interface, providing methods to manage and retrieve notifications for users in the system. This class interacts with a SQL database to perform CRUD operations on notifications, allowing for adding new notifications, retrieving notifications by user ID, and deleting notifications by their unique identifier.
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationRepository"/> class with the specified SQL connection factory. The connection factory is used to create database connections for executing SQL commands related to notifications.
    /// </summary>
    /// <param name="factory">The SQL connection factory used to create database connections.</param>
    public NotificationRepository(SqlConnectionFactory factory)
    {
        this.connectionFactory = factory;
    }

    /// <summary>
    /// Asynchronously adds a new notification to the data source. This method takes the user ID, title, description, and creation timestamp as parameters to create a new notification entry in the database. The method executes an SQL INSERT command to add the notification to the Notifications table, allowing for non-blocking execution when adding notifications to the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom the notification is being created.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="description">The description or content of the notification.</param>
    /// <param name="createdAt">The timestamp indicating when the notification was created.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task AddAsync(int userId, string title, string description, DateTime createdAt)
    {
        const string query = @"
                INSERT INTO Notifications (UserId, Title, Description, CreatedAt)
                VALUES (@UserId, @Title, @Description, @CreatedAt)";

        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@Title", SqlDbType.NVarChar).Value = title;
        command.Parameters.Add("@Description", SqlDbType.NVarChar).Value = description;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime).Value = createdAt;

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Asynchronously retrieves a list of notifications for a specific user by their user ID. This method executes an SQL SELECT command to fetch notifications from the database, joining the Notifications table with the Users table to include user information and the users_RP_scores table to include reputation points. The results are ordered by the creation timestamp in descending order, allowing for efficient retrieval of a user's notifications along with relevant user details and reputation points.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve notifications.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of notifications for the specified user.</returns>
    public async Task<List<Notification>> GetByUserIdAsync(int userId)
    {
        const string query = @"
                SELECT n.Id,
                       n.Title,
                       n.Description,
                       n.CreatedAt,
                       u.Id AS UserId,
                       u.Name AS UserName,
                       ISNULL(urp.ReputationPoints, 0) AS ReputationPoints
                FROM Notifications n
                INNER JOIN Users u ON n.UserId = u.Id
                LEFT JOIN users_RP_scores urp ON u.Id = urp.UserId
                WHERE n.UserId = @UserId
                ORDER BY n.CreatedAt DESC";

        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        var notifications = new List<Notification>();

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            notifications.Add(new Notification
            {
                Id = (int)reader["Id"],
                Title = (string)reader["Title"],
                Description = (string)reader["Description"],
                CreatedAt = (DateTime)reader["CreatedAt"],
                User = new User
                {
                    UserId = (int)reader["UserId"],
                    Name = (string)reader["UserName"],
                    ReputationPoints = (int)reader["ReputationPoints"],
                },
            });
        }

        return notifications;
    }

    /// <summary>
    /// Asynchronously deletes a notification by its unique identifier. This method executes an SQL DELETE command to remove the specified notification from the Notifications table in the database, allowing for efficient deletion of notifications based on their ID.
    /// </summary>
    /// <param name="notificationId">The ID of the notification to delete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteAsync(int notificationId)
    {
        const string query = @"DELETE FROM Notifications WHERE Id = @NotificationId";
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(query, connection);

        command.Parameters.Add("@NotificationId", SqlDbType.Int).Value = notificationId;

        await command.ExecuteNonQueryAsync();
    }
}
