using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

using static System.Runtime.InteropServices.JavaScript.JSType;
namespace Events_GSS.Data.Repositories.announcementRepository;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AnnouncementRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    
    public async Task<int> AddAsync(Announcement announcement, int eventId, int userId)
    {
        using (SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();
                string query = @"
                    INSERT INTO Announcements ( EventId, UserId, Message, Date, IsPinned, IsEdited)
                    OUTPUT INSERTED.AnnId
                    VALUES (@EventId, @UserId, @Message, @Date, 0, 0)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Message", announcement.Message);
                    command.Parameters.AddWithValue("@Date", announcement.Date);
                    command.Parameters.AddWithValue("@EventId", eventId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
        }
    }

    public async Task AddReactionAsync(int announcementId, int userId, string emoji)
    {
        using (SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();
                string query = @"
                    IF EXISTS (SELECT 1 FROM AnnouncementReactions WHERE AnnouncementId = @AnnouncementId AND UserId = @UserId)
                BEGIN
                    UPDATE AnnouncementReactions
                    SET Emoji = @Emoji
                    WHERE AnnouncementId = @AnnouncementId AND UserId = @UserId
                END
                ELSE
                BEGIN
                    INSERT INTO AnnouncementReactions (AnnouncementId, UserId, Emoji)
                    VALUES (@AnnouncementId, @UserId, @Emoji)
                END";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AnnouncementId", announcementId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Emoji", emoji);
                    await command.ExecuteNonQueryAsync();
                }
        }
    }

    public async Task DeleteAsync(int announcementId)
    {
        using (SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();
                string query = @"
                        DELETE FROM Announcements 
                        WHERE AnnId = @AnnId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AnnId", announcementId);
                    await command.ExecuteNonQueryAsync();
                }
        }
    }

    public async Task<List<Announcement>> GetByEventAsync(int eventId, int userId)
    {
        var announcements = new List<Announcement>();

        using (SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();
                string query = @"
                    SELECT 
                        a.AnnId, a.Message, a.Date, a.IsPinned, a.IsEdited,
                        u.Id as AuthorId, u.Name AS AuthorName,   
                        CAST(CASE WHEN r.UserId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsRead
                    FROM Announcements a
                    INNER JOIN Users u ON a.UserId = u.Id
                    LEFT JOIN AnnouncementReadReceipts r ON a.AnnId = r.AnnouncementId AND r.UserId = @UserId
                    WHERE a.EventId = @EventId
                    ORDER BY a.IsPinned DESC, a.Date DESC";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@EventId", eventId);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        announcements.Add(new Announcement(
                            id: reader.GetInt32(reader.GetOrdinal("AnnId")),
                            message: reader.GetString(reader.GetOrdinal("Message")),
                            date: reader.GetDateTime(reader.GetOrdinal("Date")))
                        {
                            IsPinned = reader.GetBoolean(reader.GetOrdinal("IsPinned")),
                            IsEdited = reader.GetBoolean(reader.GetOrdinal("IsEdited")),
                            IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                            Author = new User
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("AuthorId")),
                                Name = reader.GetString(reader.GetOrdinal("AuthorName"))
                            }
                        });
                    }
                }
                if (announcements.Count == 0) return announcements;

                var announcementIds = announcements.Select(a => a.Id).ToList();
                var idParams = string.Join(",", announcementIds.Select((_, i) => $"@aid{i}"));

                var rxnQuery = $@"
                    SELECT ar.Id, ar.AnnouncementId, ar.Emoji, ar.UserId, u.Name AS UserName
                    FROM AnnouncementReactions ar
                    INNER JOIN Users u ON ar.UserId = u.Id
                    WHERE ar.AnnouncementId IN ({idParams})";

                var allReactions = new List<(int AnnId, AnnouncementReaction Reaction)>();

                using (var cmd = new SqlCommand(rxnQuery, connection))
                {
                    for (int i = 0; i < announcementIds.Count; i++)
                        cmd.Parameters.AddWithValue($"@aid{i}", announcementIds[i]);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var annId = reader.GetInt32(reader.GetOrdinal("AnnouncementId"));
                        allReactions.Add((annId, new AnnouncementReaction
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Emoji = reader.GetString(reader.GetOrdinal("Emoji")),
                            AnnouncementId = annId,
                            Author = new User
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                Name = reader.GetString(reader.GetOrdinal("UserName"))
                            }
                        }));
                    }
                }

                var reactionsByAnn = allReactions
                    .GroupBy(r => r.AnnId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Reaction).ToList());

                foreach (var ann in announcements)
                {
                    if (reactionsByAnn.TryGetValue(ann.Id, out var reactions))
                        ann.Reactions = reactions;
                }

                return announcements;
        }
    }

    public async Task<List<AnnouncementReadReceipt>> GetReadReceiptsAsync(int announcementId)
    {
        using (SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();

                string query = @"
                    SELECT r.AnnouncementId, r.ReadAt,
                    u.Id AS UserId, u.Name AS UserName
                    FROM AnnouncementReadReceipts r
                    INNER JOIN Users u ON r.UserId = u.Id
                    WHERE r.AnnouncementId = @AnnId
                    ORDER BY r.ReadAt ASC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AnnId", announcementId);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        var receipts = new List<AnnouncementReadReceipt>();
                        while (await reader.ReadAsync())
                        {
                            receipts.Add(new AnnouncementReadReceipt
                            {
                                AnnouncementId = reader.GetInt32(reader.GetOrdinal("AnnouncementId")),
                                User = new User
                                {
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    Name = reader.GetString(reader.GetOrdinal("UserName"))
                                },
                                ReadAt = reader.GetDateTime(reader.GetOrdinal("ReadAt"))
                            });
                    }
                        return receipts;
                    }
                }
        }
    }

    public async Task MarkAsReadAsync(int announcementId, int userId)
    {
        using( SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();
                string query = @"
                    IF NOT EXISTS (SELECT 1 FROM AnnouncementReadReceipts WHERE AnnouncementId = @AnnouncementId AND UserId = @UserId)
                    BEGIN
                        INSERT INTO AnnouncementReadReceipts (AnnouncementId, UserId, ReadAt)
                        VALUES (@AnnouncementId, @UserId, GETUTCDATE())
                    END";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AnnouncementId", announcementId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    await command.ExecuteNonQueryAsync();
                }
        }
    }

    public async Task PinAsync(int announcementId, int eventId)
    {
        using (SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();
                string query = @"
                    UPDATE Announcements
                    SET IsPinned = 1
                    WHERE AnnId = @AnnId AND EventId = @EventId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AnnId", announcementId);
                    command.Parameters.AddWithValue("@EventId", eventId);
                    command.ExecuteNonQuery();
                }
        }
    }

    public async Task RemoveReactionAsync(int announcementId, int userId)
    {
        using (SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();
                string query = @"
                    DELETE FROM AnnouncementReactions 
                    WHERE AnnouncementId = @AnnouncementId AND UserId = @UserId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AnnouncementId", announcementId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    await command.ExecuteNonQueryAsync();
                }
        }
    }

    public async Task UnpinAsync(int eventId)
    {
        using (SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();

                string query = @"
                   UPDATE Announcements
                    SET IsPinned = 0
                    WHERE EventId = @EventId AND IsPinned = 1";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EventId", eventId);
                    await command.ExecuteNonQueryAsync();
                }
        }
    }

    public async Task UpdateAsync(int annId, string newMessage)
    {
        using (SqlConnection connection = _connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();

                string query = @"
                   UPDATE Announcements
                    SET Message = @Message, IsEdited = 1
                    WHERE AnnId = @AnnId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Message", newMessage);
                    command.Parameters.AddWithValue("@AnnId", annId);

                    await command.ExecuteNonQueryAsync();
                }
        }
    }

    public async Task<Announcement?> GetByIdAsync(int annId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
            SELECT a.AnnId, a.Message, a.Date, a.IsPinned, a.IsEdited,
            a.UserId, u.Name AS AuthorName
            FROM Announcements a
            INNER JOIN Users u ON a.UserId = u.Id
            WHERE a.AnnId = @AnnId";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("AnnId", annId);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new Announcement(
            id: reader.GetInt32(reader.GetOrdinal("AnnId")),
            message: reader.GetString(reader.GetOrdinal("Message")),
            date: reader.GetDateTime(reader.GetOrdinal("Date")))
            {
                IsPinned = reader.GetBoolean(reader.GetOrdinal("IsPinned")),
                IsEdited = reader.GetBoolean(reader.GetOrdinal("IsEdited")),
                Author = new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Name = reader.GetString(reader.GetOrdinal("AuthorName"))
                }
            };
    }

    public async Task<int> GetTotalParticipantsAsync(int eventId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            SELECT COUNT(*) FROM AttendedEvents WHERE EventId = @EventId";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId)
    {
        var counts = new Dictionary<int, int>();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        // For every event the user attends, count announcements they haven't read
        const string query = @"
            SELECT a.EventId, COUNT(*) AS UnreadCount
            FROM Announcements a
            INNER JOIN AttendedEvents ae ON a.EventId = ae.EventId AND ae.UserId = @UserId
            LEFT JOIN AnnouncementReadReceipts r ON a.AnnId = r.AnnouncementId AND r.UserId = @UserId
            WHERE r.Id IS NULL
            GROUP BY a.EventId";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            counts[reader.GetInt32(0)] = reader.GetInt32(1);
        }

        return counts;
    }

    public async Task<List<User>> GetAllParticipantsAsync(int eventId)
    {
        var users = new List<User>();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            SELECT u.Id, u.Name
            FROM AttendedEvents ae
            INNER JOIN Users u ON ae.UserId = u.Id
            WHERE ae.EventId = @EventId
            ORDER BY u.Name";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name"))
            });
        }

        return users;
    }

}
