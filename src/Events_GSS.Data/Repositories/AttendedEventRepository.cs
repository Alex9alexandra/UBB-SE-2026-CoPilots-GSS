using System.Data;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;


using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories
{
    /// <summary>
    /// Repository for managing attended event records in the database.
    /// </summary>
    public class AttendedEventRepository : IAttendedEventRepository
    {
        private readonly SqlConnectionFactory connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendedEventRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">The SQL connection connectionFactory.</param>
        public AttendedEventRepository(SqlConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Maps a single reader row to an AttendedEvent object.
        /// Expects the query to have aliased conflicting column names (e.g. EventName, UserName).
        /// </summary>
        /// <param name="reader">The SQL data reader.</param>
        /// <returns>An <see cref="AttendedEvent"/> object mapped from the reader row.</returns>
        private static AttendedEvent MapRow(SqlDataReader reader)
        {
            var category = reader["CategoryId"] == DBNull.Value ? null : new Category
            {
                CategoryId = (int)reader["CategoryId"],
                Title = (string)reader["CategoryTitle"],
            };

            var admin = new User
            {
                UserId = (int)reader["AdminId"],
                Name = (string)reader["AdminName"],
                ReputationPoints = reader["AdminRP"] == DBNull.Value ? 0 : (int)reader["AdminRP"],
            };

            var @event = new Event
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
                Admin = admin,
            };

            var user = new User
            {
                UserId = (int)reader["UserId"],
                Name = (string)reader["UserName"],
                ReputationPoints = reader["ReputationPoints"] == DBNull.Value ? 0 : (int)reader["ReputationPoints"],
            };

            return new AttendedEvent(
                @event,
                user,
                (DateTime)reader["EnrollmentDate"],
                (bool)reader["IsArchived"],
                (bool)reader["IsFavourite"]);
        }

        /// <summary>
        /// Base SELECT query used by all read queries to avoid repeating the JOIN logic.
        /// </summary>
        private const string SelectBase = @"
            SELECT
            ae.IsArchived, ae.IsFavourite, ae.EnrollmentDate,
            e.EventId, e.Name AS EventName, e.Location,
            e.StartDateTime, e.EndDateTime, e.IsPublic,
            e.Description, e.MaximumPeople, e.EventBannerPath,
            e.SlowModeSeconds,
            c.CategoryId, c.Title AS CategoryTitle,
            u.Id AS UserId, u.Name AS UserName,
            ISNULL(urp.ReputationPoints, 0) AS ReputationPoints,
            a.Id AS AdminId, a.Name AS AdminName,
            ISNULL(arp.ReputationPoints, 0) AS AdminRP
            FROM AttendedEvents ae
            INNER JOIN Events e ON ae.EventId = e.EventId
            INNER JOIN Users u ON ae.UserId = u.Id
            LEFT JOIN users_RP_scores urp ON urp.UserId = u.Id
            LEFT JOIN Categories c ON e.CategoryId = c.CategoryId
            INNER JOIN Users a ON e.AdminId = a.Id
            LEFT JOIN users_RP_scores arp ON arp.UserId = a.Id";

        /// <summary>
        /// Adds a new attended event record to the database.
        /// </summary>
        /// <param name="attendedEvent">The attended event to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddAsync(AttendedEvent attendedEvent)
        {
            const string query = @"
                INSERT INTO AttendedEvents (EventId, UserId, EnrollmentDate, IsArchived, IsFavourite)
                VALUES (@EventId, @UserId, @EnrollmentDate, @IsArchived, @IsFavourite)";

            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventId", attendedEvent.Event.EventId);
            command.Parameters.AddWithValue("@UserId", attendedEvent.User.UserId);
            command.Parameters.AddWithValue("@EnrollmentDate", attendedEvent.EnrollmentDate);
            command.Parameters.AddWithValue("@IsArchived", attendedEvent.IsArchived);
            command.Parameters.AddWithValue("@IsFavourite", attendedEvent.IsFavourite);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Deletes an attended event record from the database.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteAsync(int eventId, int userId)
        {
            const string query = @"
                DELETE FROM AttendedEvents
                WHERE EventId = @EventId AND UserId = @UserId";

            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventId", eventId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Updates the archived status of an attended event.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="isArchived">The new archived status.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateIsArchivedAsync(int eventId, int userId, bool isArchived)
        {
            const string query = @"
                UPDATE AttendedEvents
                SET IsArchived = @IsArchived
                WHERE EventId = @EventId AND UserId = @UserId";

            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@IsArchived", isArchived);
            command.Parameters.AddWithValue("@EventId", eventId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Updates the favorite status of an attended event.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="isFavourite">The new favorite status.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateIsFavouriteAsync(int eventId, int userId, bool isFavourite)
        {
            const string query = @"
                UPDATE AttendedEvents
                SET IsFavourite = @IsFavourite
                WHERE EventId = @EventId AND UserId = @UserId";

            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@IsFavourite", isFavourite);
            command.Parameters.AddWithValue("@EventId", eventId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Gets a specific attended event record by event ID and user ID.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The attended event if found; otherwise, null.</returns>
        public async Task<AttendedEvent?> GetAsync(int eventId, int userId)
        {
            string query = SelectBase + @"
                WHERE ae.EventId = @EventId AND ae.UserId = @UserId";

            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventId", eventId);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapRow(reader);
            }

            return null;
        }

        /// <summary>
        /// Gets all attended events for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A list of attended events for the user.</returns>
        public async Task<List<AttendedEvent>> GetByUserIdAsync(int userId)
        {
            string query = SelectBase + @"
                WHERE ae.UserId = @UserId";

            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<AttendedEvent>();
            while (await reader.ReadAsync())
            {
                results.Add(MapRow(reader));
            }

            return results;
        }

        /// <summary>
        /// Gets events that both the current user and a friend are enrolled in.
        /// Used by requirement 5.8 View Events in Common.
        /// </summary>
        /// <param name="userId">The current user's ID.</param>
        /// <param name="friendId">The friend's user ID.</param>
        /// <returns>A list of attended events that both users have in common.</returns>
        public async Task<List<AttendedEvent>> GetCommonEventsAsync(int userId, int friendId)
        {
            // We join AttendedEvents twice — once for the current user, once for the friend —
            // and return the current user's AttendedEvent rows for the matching events.
            string query = SelectBase + @"
                INNER JOIN AttendedEvents ae2 ON e.EventId = ae2.EventId
                WHERE ae.UserId = @UserId AND ae2.UserId = @FriendId";

            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@FriendId", friendId);

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<AttendedEvent>();
            while (await reader.ReadAsync())
            {
                results.Add(MapRow(reader));
            }

            return results;
        }

        /// <summary>
        /// Gets the total number of attendees for a specific event.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <returns>The count of attendees for the event.</returns>
        public async Task<int> GetAttendeeCountAsync(int eventId)
        {
            const string query = "SELECT COUNT(*) FROM AttendedEvents WHERE EventId = @EventId";

            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventId", eventId);

            var result = await command.ExecuteScalarAsync();
            return result is int count ? count : 0;
        }
    }
}