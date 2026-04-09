// <copyright file="EventRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.eventRepository;

using System;
using System.Data;
using Events_GSS.Data.Models;
using Microsoft.Data.SqlClient;
using Events_GSS.Data.Database;

/// <summary>
/// Implements the <see cref="IEventRepository"/> interface to provide data access methods for managing events in the system.
/// </summary>
public class EventRepository : IEventRepository
{
    /// <summary>
    /// Provides access to the factory used for creating SQL database connections.
    /// </summary>
    private readonly SqlConnectionFactory connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventRepository"/> class using the specified SQL connection factory.
    /// </summary>
    /// <param name="connectionFactory">The factory used to create SQL connections for database operations. Cannot be null.</param>
    public EventRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Asynchronously retrieves a list of all public and active events from the database, including their associated category and admin user information. An event is considered active if its end date and time is in the future. The results are ordered by the start date and time of the events in ascending order.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Event"/> objects representing all public and active events.</returns>
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
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            events.Add(MapEvent(reader));
        }

        return events;
    }

    /// <summary>
    /// Asynchronously retrieves a specific event from the database by its unique identifier, including its associated category and admin user information. If the event with the specified ID does not exist, the method returns null.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Event"/> object representing the event with the specified ID, or null if the event does not exist.</returns>
    public async Task<Event?> GetByIdAsync(int eventId)
    {
        using var conn = this.connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            @"
            SELECT e.*, c.CategoryId as CatId, c.Title as CategoryTitle,
                u.Id as UserId, u.Name as UserName,
                (SELECT COUNT(*) FROM AttendedEvents ae WHERE ae.EventId = e.EventId) AS EnrolledCount
            FROM Events e
            LEFT JOIN Categories c ON e.CategoryId = c.CategoryId
            LEFT JOIN Users u ON e.AdminId = u.Id
            WHERE e.EventId = @EventId", conn);

        cmd.Parameters.AddWithValue("@EventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapEvent(reader) : null;
    }

    /// <summary>
    /// Asynchronously adds a new event to the database and returns the unique identifier of the newly created event. The method takes an <see cref="Event"/> object as input, which contains the details of the event to be added. If the event is successfully added to the database, the method returns the generated event ID. If there is an error during the insertion process, an exception may be thrown.
    /// </summary>
    /// <param name="eventEntity">The <see cref="Event"/> object representing the event to be added.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unique identifier of the newly created event.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="eventEntity"/> or its required properties are null.</exception>
    public async Task<int> AddAsync(Event eventEntity)
    {
        using var conn = this.connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            @"
            INSERT INTO Events 
                (Name, Location, StartDateTime, EndDateTime, 
                 IsPublic, Description, MaximumPeople, EventBannerPath, CategoryId, AdminId)
            OUTPUT INSERTED.EventId
            VALUES 
                (@Name, @Location, @Start, @End,
                 @IsPublic, @Desc, @MaxPeople, @Banner, @CategoryId, @AdminId)", conn);

        cmd.Parameters.AddWithValue("@Name", eventEntity.Name);
        cmd.Parameters.AddWithValue("@Location", eventEntity.Location);
        cmd.Parameters.Add("@Start", System.Data.SqlDbType.DateTime2).Value = eventEntity.StartDateTime;
        cmd.Parameters.Add("@End", System.Data.SqlDbType.DateTime2).Value = eventEntity.EndDateTime;
        cmd.Parameters.AddWithValue("@IsPublic", eventEntity.IsPublic);
        cmd.Parameters.AddWithValue("@Desc", (object?)eventEntity.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxPeople", (object?)eventEntity.MaximumPeople ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Banner", (object?)eventEntity.EventBannerPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CategoryId", (object?)eventEntity.Category?.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AdminId", eventEntity.Admin?.UserId ?? throw new ArgumentNullException("AdminId is required"));

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Asynchronously updates the details of an existing event in the database based on the provided <see cref="Event"/> object. The method identifies the event to be updated using the unique identifier (EventId) contained within the <paramref name="eventEntity"/>. If the event with the specified ID exists, its details will be updated with the new values provided in the <paramref name="eventEntity"/>. If the event does not exist, no changes will be made to the database. The method does not return a value, but it may throw exceptions if there are issues during the update process, such as database connectivity problems or invalid data.
    /// </summary>
    /// <param name="eventEntity">The <see cref="Event"/> object containing the updated event details.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="eventEntity"/> or its required properties are null.</exception>
    public async Task UpdateAsync(Event eventEntity)
        {
        using var conn = this.connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand(
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
            WHERE EventId = @EventId", conn);

        cmd.Parameters.AddWithValue("@EventId", eventEntity.EventId);
        cmd.Parameters.AddWithValue("@Name", eventEntity.Name);
        cmd.Parameters.AddWithValue("@Location", eventEntity.Location);
        cmd.Parameters.AddWithValue("@Start", eventEntity.StartDateTime);
        cmd.Parameters.AddWithValue("@End", eventEntity.EndDateTime);
        cmd.Parameters.AddWithValue("@IsPublic", eventEntity.IsPublic);
        cmd.Parameters.AddWithValue("@Desc", (object?)eventEntity.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxPeople", (object?)eventEntity.MaximumPeople ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Banner", (object?)eventEntity.EventBannerPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CategoryId", (object?)eventEntity.Category?.CategoryId ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Asynchronously deletes an event from the database based on the provided unique identifier (eventId). The method removes the event with the specified ID from the database. If the event with the given ID does not exist, no changes will be made to the database. The method does not return a value, but it may throw exceptions if there are issues during the deletion process, such as database connectivity problems or invalid data.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to be deleted.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="eventId"/> is not valid.</exception>
    public async Task DeleteAsync(int eventId)
    {
        using var conn = this.connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand("DELETE FROM Events WHERE EventId = @EventId", conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        await cmd.ExecuteNonQueryAsync();
    }

    private static Event MapEvent(SqlDataReader reader) => new ()
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
