using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.notificationRepository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly SqlConnectionFactory connectionFactory;

        public NotificationRepository(SqlConnectionFactory factory)
        {
            connectionFactory = factory;
        }

        public async Task AddAsync(Notification notification)
        {
            const string query = @"
                INSERT INTO Notifications (UserId, Title, Description, CreatedAt)
                VALUES (@UserId, @Title, @Description, @CreatedAt)";

            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = notification.User.UserId;
            command.Parameters.Add("@Title", SqlDbType.NVarChar).Value = notification.Title;
            command.Parameters.Add("@Description", SqlDbType.NVarChar).Value = notification.Description;
            command.Parameters.Add("@CreatedAt", SqlDbType.DateTime).Value = notification.CreatedAt;

            await command.ExecuteNonQueryAsync();

        }
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

            using var connection = connectionFactory.CreateConnection();
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
                        ReputationPoints = (int)reader["ReputationPoints"]
                    }
                });
            }

            return notifications;
        }

        public async Task DeleteAsync(int notificationId)
        {
            const string query = @"DELETE FROM Notifications WHERE Id = @NotificationId";
            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);

            command.Parameters.Add("@NotificationId", SqlDbType.Int).Value = notificationId;

            await command.ExecuteNonQueryAsync();
        }
    }
}
