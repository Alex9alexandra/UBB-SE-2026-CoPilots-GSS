using System.Data;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;


using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories
{
    public class AttendedEventRepository : IAttendedEventRepository
    {
        private readonly SqlConnectionFactory _factory;

        public AttendedEventRepository(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        // Maps a single reader row to an AttendedEvent object.
        // Expects the query to have aliased conflicting column names (e.g. EventName, UserName).
        private static AttendedEvent MapRow(SqlDataReader reader)
        {
            var category = reader["CategoryId"] == DBNull.Value ? null : new Category
            {
                CategoryId = (int)reader["CategoryId"],
                Title = (string)reader["CategoryTitle"]
            };

            var admin = new User
            {
                UserId = (int)reader["AdminId"],
                Name = (string)reader["AdminName"],
                ReputationPoints = (int)reader["AdminRP"]
            };

            var ev = new Event
            {
                EventId = (int)reader["EventId"],
                Name = (string)reader["EventName"],
                Location = (string)reader["Location"],
                StartDateTime = (DateTime)reader["StartDateTime"],
                EndDateTime = (DateTime)reader["EndDateTime"],
                IsPublic = (bool)reader["IsPublic"],
                Description = reader["Description"] as string,
                MaximumPeople = reader["MaximumPeople"] == DBNull.Value ? null : (int?)reader["MaximumPeople"],
                EventBannerPath = reader["EventBannerPath"] as string,
                SlowModeSeconds = reader["SlowModeSeconds"] == DBNull.Value ? null : (int?)reader["SlowModeSeconds"],
                Category = category,
                Admin = admin
            };

            var user = new User
            {
                UserId = (int)reader["UserId"],
                Name = (string)reader["UserName"],
                ReputationPoints = (int)reader["ReputationPoints"]
            };

            return new AttendedEvent(ev, user,
                (DateTime)reader["EnrollmentDate"],
                (bool)reader["IsArchived"],
                (bool)reader["IsFavourite"]);
        }

        // Base SELECT used by all read queries — avoids repeating the JOIN everywhere.
        private const string SelectBase = @"
            SELECT
            ae.IsArchived, ae.IsFavourite, ae.EnrollmentDate,
            e.EventId, e.Name AS EventName, e.Location,
            e.StartDateTime, e.EndDateTime, e.IsPublic,
            e.Description, e.MaximumPeople, e.EventBannerPath,
            e.SlowModeSeconds,
            c.CategoryId, c.Title AS CategoryTitle,
            u.Id AS UserId, u.Name AS UserName, u.ReputationPoints,
            a.Id AS AdminId, a.Name AS AdminName, a.ReputationPoints AS AdminRP
            FROM AttendedEvents ae
            INNER JOIN Events e ON ae.EventId = e.EventId
            INNER JOIN Users u ON ae.UserId = u.Id
            LEFT JOIN Categories c ON e.CategoryId = c.CategoryId
            INNER JOIN Users a ON e.AdminId = a.Id";

        public async Task AddAsync(AttendedEvent attendedEvent)
        {
            const string query = @"
                INSERT INTO AttendedEvents (EventId, UserId, EnrollmentDate, IsArchived, IsFavourite)
                VALUES (@EventId, @UserId, @EnrollmentDate, @IsArchived, @IsFavourite)";

            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventId", attendedEvent.Event.EventId);
            command.Parameters.AddWithValue("@UserId", attendedEvent.User.UserId);
            command.Parameters.AddWithValue("@EnrollmentDate", attendedEvent.EnrollmentDate);
            command.Parameters.AddWithValue("@IsArchived", attendedEvent.IsArchived);
            command.Parameters.AddWithValue("@IsFavourite", attendedEvent.IsFavourite);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int eventId, int userId)
        {
            const string query = @"
                DELETE FROM AttendedEvents
                WHERE EventId = @EventId AND UserId = @UserId";

            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventId", eventId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateIsArchivedAsync(int eventId, int userId, bool isArchived)
        {
            const string query = @"
                UPDATE AttendedEvents
                SET IsArchived = @IsArchived
                WHERE EventId = @EventId AND UserId = @UserId";

            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@IsArchived", isArchived);
            command.Parameters.AddWithValue("@EventId", eventId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateIsFavouriteAsync(int eventId, int userId, bool isFavourite)
        {
            const string query = @"
                UPDATE AttendedEvents
                SET IsFavourite = @IsFavourite
                WHERE EventId = @EventId AND UserId = @UserId";

            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@IsFavourite", isFavourite);
            command.Parameters.AddWithValue("@EventId", eventId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<AttendedEvent?> GetAsync(int eventId, int userId)
        {
            string query = SelectBase + @"
                WHERE ae.EventId = @EventId AND ae.UserId = @UserId";

            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventId", eventId);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return MapRow(reader);

            return null;
        }

        public async Task<List<AttendedEvent>> GetByUserIdAsync(int userId)
        {
            string query = SelectBase + @"
                WHERE ae.UserId = @UserId";

            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<AttendedEvent>();
            while (await reader.ReadAsync())
                results.Add(MapRow(reader));

            return results;
        }

        // Returns events that both the current user and a friend are enrolled in.
        // Used by requirement 5.8 View Events in Common.
        public async Task<List<AttendedEvent>> GetCommonEventsAsync(int userId, int friendId)
        {
            // We join AttendedEvents twice — once for the current user, once for the friend —
            // and return the current user's AttendedEvent rows for the matching events.
            string query = SelectBase + @"
                INNER JOIN AttendedEvents ae2 ON e.Id = ae2.EventId
                WHERE ae.UserId = @UserId AND ae2.UserId = @FriendId";

            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@FriendId", friendId);

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<AttendedEvent>();
            while (await reader.ReadAsync())
                results.Add(MapRow(reader));

            return results;
        }
    }
}