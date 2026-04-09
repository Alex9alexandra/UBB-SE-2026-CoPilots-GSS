// <copyright file="EventRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.eventRepository;

using System.Data;
using Events_GSS.Data.Models;
using Microsoft.Data.SqlClient;
using Events_GSS.Data.Database;

/// <summary>
/// Repository for managing event data operations.
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The SQL connection factory.</param>
    public EventRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Gets all public and active events.
    /// </summary>
    /// <returns>A list of public active events.</returns>
    public async Task<List<Event>> GetAllPublicActiveAsync()
    {
        var events = new List<Event>();
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            @"
            SELECT E.*, C.CategoryId as CatId, C.Title as CategoryTitle,
                u.Id as UserId, u.Name as UserName,
                (SELECT COUNT(*) FROM AttendedEvents AE WHERE AE.EventId = E.EventId) AS EnrolledCount
            FROM Events E
            LEFT JOIN Categories C ON E.CategoryId = C.CategoryId
            LEFT JOIN Users u ON E.AdminId = u.Id
            WHERE E.IsPublic = 1 AND E.EndDateTime > GETUTCDATE()
            ORDER BY E.StartDateTime ASC", connection);

        using var dataReader = await command.ExecuteReaderAsync();
        while (await dataReader.ReadAsync())
        {
            events.Add(MapEvent(dataReader));
        }

        return events;
    }

    /// <summary>
    /// Gets an event by its identifier.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>The event if found; otherwise, null.</returns>
    public async Task<Event?> GetByIdAsync(int eventId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            @"
            SELECT e.*, c.CategoryId as CatId, c.Title as CategoryTitle,
                u.Id as UserId, u.Name as UserName,
                (SELECT COUNT(*) FROM AttendedEvents ae WHERE ae.EventId = e.EventId) AS EnrolledCount
            FROM Events e
            LEFT JOIN Categories c ON e.CategoryId = c.CategoryId
            LEFT JOIN Users u ON e.AdminId = u.Id
            WHERE e.EventId = @EventId", connection);

        command.Parameters.AddWithValue("@EventId", eventId);

        using var dataReader = await command.ExecuteReaderAsync();
        return await dataReader.ReadAsync() ? MapEvent(dataReader) : null;
    }

    /// <summary>
    /// Adds a new event.
    /// </summary>
    /// <param name="eventEntity">The event to add.</param>
    /// <returns>The identifier of the newly created event.</returns>
    public async Task<int> AddAsync(Event eventEntity)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            @"
            INSERT INTO Events 
                (Name, Location, StartDateTime, EndDateTime, 
                 IsPublic, Description, MaximumPeople, EventBannerPath, CategoryId, AdminId)
            OUTPUT INSERTED.EventId
            VALUES 
                (@Name, @Location, @Start, @End,
                 @IsPublic, @Desc, @MaxPeople, @Banner, @CategoryId, @AdminId)", connection);

        command.Parameters.AddWithValue("@Name", eventEntity.Name);
        command.Parameters.AddWithValue("@Location", eventEntity.Location);
        command.Parameters.Add("@Start", System.Data.SqlDbType.DateTime2).Value = eventEntity.StartDateTime;
        command.Parameters.Add("@End", System.Data.SqlDbType.DateTime2).Value = eventEntity.EndDateTime;
        command.Parameters.AddWithValue("@IsPublic", eventEntity.IsPublic);
        command.Parameters.AddWithValue("@Desc", (object?)eventEntity.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@MaxPeople", (object?)eventEntity.MaximumPeople ?? DBNull.Value);
        command.Parameters.AddWithValue("@Banner", (object?)eventEntity.EventBannerPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@CategoryId", (object?)eventEntity.Category?.CategoryId ?? DBNull.Value);

        if (eventEntity.Admin == null)
        {
            throw new ArgumentException("Admin is required.", nameof(eventEntity));
        }

        command.Parameters.AddWithValue("@AdminId", eventEntity.Admin.UserId);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventEntity">The event to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateAsync(Event eventEntity)
        {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(
            @"
            UPDATE Events SET
                Name = @Name,
                Location= @Location,
                StartDateTime = @Start,
                EndDateTime = @End,
                IsPublic = @IsPublic,
                Description = @Desc,
                MaximumPeople = @MaxPeople,
                EventBannerPath = @Banner,
                CategoryId = @CategoryId
            WHERE EventId = @EventId", connection);

        command.Parameters.AddWithValue("@EventId", eventEntity.EventId);
        command.Parameters.AddWithValue("@Name", eventEntity.Name);
        command.Parameters.AddWithValue("@Location", eventEntity.Location);
        command.Parameters.AddWithValue("@Start", eventEntity.StartDateTime);
        command.Parameters.AddWithValue("@End", eventEntity.EndDateTime);
        command.Parameters.AddWithValue("@IsPublic", eventEntity.IsPublic);
        command.Parameters.AddWithValue("@Desc", (object?)eventEntity.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@MaxPeople", (object?)eventEntity.MaximumPeople ?? DBNull.Value);
        command.Parameters.AddWithValue("@Banner", (object?)eventEntity.EventBannerPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@CategoryId", (object?)eventEntity.Category?.CategoryId ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Deletes an event by its identifier.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteAsync(int eventId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand("DELETE FROM Events WHERE EventId = @EventId", connection);
        command.Parameters.AddWithValue("@EventId", eventId);

        await command.ExecuteNonQueryAsync();
    }

    private static Event MapEvent(SqlDataReader reader)
    {
        return new ()
        {
            EventId = reader.GetInt32("EventId"),
            Name = reader.GetString("Name"),
            Location = reader.GetString("Location"),
            StartDateTime = reader.GetDateTime("StartDateTime"),
            EndDateTime = reader.GetDateTime("EndDateTime"),
            IsPublic = reader.GetBoolean("IsPublic"),
            Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
            MaximumPeople = reader.IsDBNull("MaximumPeople") ? null : reader.GetInt32("MaximumPeople"),
            EventBannerPath = reader.IsDBNull("EventBannerPath") ? null : reader.GetString("EventBannerPath"),

            Category = reader.IsDBNull("CatId") ? null : new Category
            {
                CategoryId = reader.GetInt32("CatId"),
                Title = reader.GetString("CategoryTitle"),
            },
            Admin = reader.IsDBNull("UserId") ? null : new User
            {
                UserId = reader.GetInt32("UserId"),
                Name = reader.GetString("UserName"),
            },
            SlowModeSeconds = reader.IsDBNull("SlowModeSeconds") ? null : reader.GetInt32("SlowModeSeconds"),
            EnrolledCount = reader.GetInt32("EnrolledCount"),
        };
    }
}
