using System;
using System.Data;
using Events_GSS.Data.Models;
using Microsoft.Data.SqlClient;
using Events_GSS.Data.Database;

namespace Events_GSS.Data.Repositories.eventRepository;

public class EventRepository: IEventRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public EventRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<Event>> GetAllPublicActiveAsync()
    {
        var events = new List<Event>();
        using var connection=_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = new SqlCommand(@"
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

    public async Task<Event?> GetByIdAsync(int eventId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
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

    public async Task AddAsync(Event eventEntity)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
            INSERT INTO Events 
                (Name, Location, StartDateTime, EndDateTime, 
                 IsPublic, Description, MaximumPeople, EventBannerPath, CategoryId, AdminId)
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

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Event eventEntity)
        {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
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

    public async Task DeleteAsync(int eventId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand("DELETE FROM Events WHERE EventId = @EventId", conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        await cmd.ExecuteNonQueryAsync();
    }

    private static Event MapEvent(SqlDataReader reader) => new()
    {
        EventId = reader.GetInt32("EventId"),
        Name = reader.GetString("Name"),
        Location=reader.GetString("Location"),
        StartDateTime = reader.GetDateTime("StartDateTime"),
        EndDateTime = reader.GetDateTime("EndDateTime"),
        IsPublic = reader.GetBoolean("IsPublic"),
        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
        MaximumPeople = reader.IsDBNull("MaximumPeople") ? null : reader.GetInt32("MaximumPeople"),
        EventBannerPath = reader.IsDBNull("EventBannerPath") ? null : reader.GetString("EventBannerPath"),

        Category=reader.IsDBNull("CatId") ? null : new Category
        {
            CategoryId = reader.GetInt32("CatId"),
            Title = reader.GetString("CategoryTitle")
        },
        Admin =reader.IsDBNull("UserId") ? null : new User
        {
            UserId = reader.GetInt32("UserId"),
            Name = reader.GetString("UserName")
        },
        SlowModeSeconds = reader.IsDBNull("SlowModeSeconds") ? null : reader.GetInt32("SlowModeSeconds"),
        EnrolledCount = reader.GetInt32("EnrolledCount"),
    };
}
