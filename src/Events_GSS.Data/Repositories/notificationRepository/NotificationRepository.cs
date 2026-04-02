using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.notificationRepository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly SqlConnectionFactory _factory;

        public NotificationRepository(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task AddAsync(Notification notification)
        {
            const string query = @"
                INSERT INTO Notifications (UserId, Title, Description, CreatedAt)
                VALUES (@Id, @UserId, @Title, @Description, @CreatedAt)";

            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", notification.User.UserId);
            command.Parameters.AddWithValue("@Title", notification.Title);
            command.Parameters.AddWithValue("@Description", notification.Description);
            command.Parameters.AddWithValue("@CreatedAt", notification.CreatedAt);

            await command.ExecuteNonQueryAsync();

        }
        public async Task<List<Notification>> GetByUserIdAsync(int userId)
        {
            const string query = @"
            SELECT n.Id, n.Title, n.Description, n.CreatedAt, u.Id AS UserId, u.Name AS UserName, u.ReputationPoints
            FROM Notifications n
            INNER JOIN Users u ON n.UserId = u.Id
            WHERE n.UserId = @UserId";

            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            var results = new List<Notification>();

            while (await reader.ReadAsync())
            {
                results.Add(new Notification
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

            return results;
        }

        public async Task DeleteAsync(int notificationId)
        {
            const string query = @"DELETE FROM Notifications WHERE Id = @NotificationId";
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@NotificationId", notificationId);

            await command.ExecuteNonQueryAsync();
        }
    }
}
