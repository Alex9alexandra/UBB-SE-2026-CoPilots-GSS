// <copyright file="AnnouncementRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.announcementRepository;

using System.Data;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

/// <summary>
/// A repository to hold all announcements.
/// </summary>
public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnnouncementRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory"> Initializes connection with database. </param>
    public AnnouncementRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    /// <inheritdoc/>
    public async Task<int> AddAnnouncementAsync(Announcement announcement, int eventId, int userId)
    {
        using (SqlConnection connection = this.connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO Announcements ( EventId, UserId, Message, Date, IsPinned, IsEdited)
                    OUTPUT INSERTED.AnnId
                    VALUES (@EventId, @UserId, @Message, @Date, 0, 0)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Message", SqlDbType.NVarChar, 500).Value = announcement.Message;
                    command.Parameters.Add("@Date", SqlDbType.DateTime2).Value = announcement.Date;
                    command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
        }
    }

    /// <inheritdoc/>
    public async Task InsertReactionAsync(int announcementId, int userId, string emoji)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
        INSERT INTO AnnouncementReactions (AnnouncementId, UserId, Emoji)
        VALUES (@AnnouncementId, @UserId, @Emoji)";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@AnnouncementId", SqlDbType.Int).Value = announcementId;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@Emoji", SqlDbType.NVarChar, 10).Value = emoji;

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateReactionAsync(int announcementId, int userId, string emoji)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
        UPDATE AnnouncementReactions
        SET Emoji = @Emoji
        WHERE AnnouncementId = @AnnouncementId AND UserId = @UserId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@AnnouncementId", SqlDbType.Int).Value = announcementId;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@Emoji", SqlDbType.NVarChar, 10).Value = emoji;

        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteAnnouncementAsync(int announcementId)
    {
        using (SqlConnection connection = this.connectionFactory.CreateConnection())
        {
                await connection.OpenAsync();

                string query = @"
                        DELETE FROM Announcements 
                        WHERE AnnId = @AnnId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                command.Parameters.Add("@AnnId", SqlDbType.Int).Value = announcementId;
                await command.ExecuteNonQueryAsync();
                }
        }
    }

    // <summary>
    // It converts a row from the database into a C# object (an Announcement)
    // </summary>
    private Announcement MapAnnouncement(SqlDataReader reader)
    {
        return new Announcement(
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
                Name = reader.GetString(reader.GetOrdinal("AuthorName")),
            },
        };
    }

    /// <inheritdoc/>
    public async Task<List<Announcement>> GetAnnouncementsByEventAsync(int eventId, int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        return await this.GetAnnouncementsAsync(connection, eventId, userId);
    }

    public async Task<List<AnnouncementReadReceipt>> GetReadReceiptsAsync(int announcementId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
        SELECT r.AnnouncementId, r.ReadAt,
               u.Id AS UserId, u.Name AS UserName
        FROM AnnouncementReadReceipts r
        INNER JOIN Users u ON r.UserId = u.Id
        WHERE r.AnnouncementId = @AnnId
        ORDER BY r.ReadAt ASC";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@AnnId", SqlDbType.Int).Value = announcementId;

        using var reader = await command.ExecuteReaderAsync();

        var receipts = new List<AnnouncementReadReceipt>();

        while (await reader.ReadAsync())
        {
            receipts.Add(this.MapReadReceipt(reader));
        }

        return receipts;
    }

    // <summary>
    // It converts a row from the database into a C# object (an AnnouncementRadReceipt)
    // </summary>
    private AnnouncementReadReceipt MapReadReceipt(SqlDataReader reader)
    {
        return new AnnouncementReadReceipt
        {
            AnnouncementId = reader.GetInt32(reader.GetOrdinal("AnnouncementId")),
            User = new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Name = reader.GetString(reader.GetOrdinal("UserName")),
            },
            ReadAt = reader.GetDateTime(reader.GetOrdinal("ReadAt")),
        };
    }

    public async Task InsertReadReceiptAsync(int announcementId, int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
        INSERT INTO AnnouncementReadReceipts (AnnouncementId, UserId, ReadAt)
        VALUES (@AnnouncementId, @UserId, GETUTCDATE())";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@AnnouncementId", SqlDbType.Int).Value = announcementId;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> HasUserReadAsync(int announcementId, int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
        SELECT 1
        FROM AnnouncementReadReceipts
        WHERE AnnouncementId = @AnnouncementId
          AND UserId = @UserId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@AnnouncementId", SqlDbType.Int).Value = announcementId;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        var result = await command.ExecuteScalarAsync();

        return result != null;
    }

    /// <inheritdoc/>
    public async Task PinAsync(int announcementId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
        UPDATE Announcements
        SET IsPinned = 1
        WHERE AnnId = @AnnId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@AnnId", SqlDbType.Int).Value = announcementId;

        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task RemoveReactionAsync(int announcementId, int userId)
    {
        using (SqlConnection connection = this.connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();
            string query = @"
                DELETE FROM AnnouncementReactions 
                WHERE AnnouncementId = @AnnouncementId AND UserId = @UserId";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@AnnouncementId", SqlDbType.Int).Value = announcementId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    /// <inheritdoc/>
    public async Task UnpinAnnouncementAsync(int eventId)
    {
        using (SqlConnection connection = this.connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();

            string query = @"
                UPDATE Announcements
                SET IsPinned = 0
                WHERE EventId = @EventId AND IsPinned = 1";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAnnouncementAsync(int announcementId, string newMessage)
    {
        using (SqlConnection connection = this.connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();

            string query = @"
                UPDATE Announcements
                SET Message = @Message, IsEdited = 1
                WHERE AnnId = @AnnId";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Message", SqlDbType.NVarChar, 500).Value = newMessage;
                command.Parameters.Add("@AnnId", SqlDbType.Int).Value = announcementId;

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    /// <inheritdoc/>
    public async Task<Announcement?> GetAnnouncementByIdAsync(int announcementId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
            SELECT a.AnnId, a.Message, a.Date, a.IsPinned, a.IsEdited,
            a.UserId, u.Name AS AuthorName
            FROM Announcements a
            INNER JOIN Users u ON a.UserId = u.Id
            WHERE a.AnnId = @AnnId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@AnnId", SqlDbType.Int).Value = announcementId;

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

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
                    Name = reader.GetString(reader.GetOrdinal("AuthorName")),
                },
            };
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalParticipantsAsync(int eventId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
            SELECT COUNT(*) FROM AttendedEvents WHERE EventId = @EventId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId)
    {
        var counts = new Dictionary<int, int>();

        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // For every event the user attends, count announcements they haven't read
        const string query = @"
            SELECT a.EventId, COUNT(*) AS UnreadCount
            FROM Announcements a
            INNER JOIN AttendedEvents ae ON a.EventId = ae.EventId AND ae.UserId = @UserId
            LEFT JOIN AnnouncementReadReceipts r ON a.AnnId = r.AnnouncementId AND r.UserId = @UserId
            WHERE r.Id IS NULL
            GROUP BY a.EventId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var eventId = reader.GetInt32(0);
            var unreadCount = reader.GetInt32(1);
            counts[eventId] = unreadCount;
        }

        return counts;
    }

    /// <inheritdoc/>
    public async Task<List<User>> GetAllParticipantsAsync(int eventId)
    {
        var users = new List<User>();

        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
            SELECT u.Id, u.Name
            FROM AttendedEvents ae
            INNER JOIN Users u ON ae.UserId = u.Id
            WHERE ae.EventId = @EventId
            ORDER BY u.Name";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
            });
        }

        return users;
    }

    /// <inheritdoc/>
    public async Task<string?> GetUserReactionAsync(int announcementId, int userId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = @"
        SELECT Emoji 
        FROM AnnouncementReactions 
        WHERE AnnouncementId = @AnnouncementId 
          AND UserId = @UserId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@AnnouncementId", SqlDbType.Int).Value = announcementId;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        var result = await command.ExecuteScalarAsync();

        return result as string;
    }

    private async Task<List<Announcement>> GetAnnouncementsAsync(
    SqlConnection connection,
    int eventId,
    int userId)
    {
        var result = new List<Announcement>();

        // gets all announcements and sorts them based on pinned ones
        // returns 0 if there are no announcements
        const string query = @"
                            SELECT 
                                a.AnnId,
                                a.Message,
                                a.Date,
                                a.IsPinned,
                                a.IsEdited,
                                u.Id AS AuthorId,
                                u.Name AS AuthorName,
                                CAST(CASE 
                                    WHEN r.UserId IS NOT NULL THEN 1 
                                    ELSE 0 
                                END AS BIT) AS IsRead
                            FROM Announcements a
                            INNER JOIN Users u ON a.UserId = u.Id
                            LEFT JOIN AnnouncementReadReceipts r 
                                ON a.AnnId = r.AnnouncementId 
                                AND r.UserId = @UserId
                            WHERE a.EventId = @EventId
                            ORDER BY a.IsPinned DESC, a.Date DESC";

        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(this.MapAnnouncement(reader));
        }

        return result;
    }

    public async Task<List<(int AnnouncementId, AnnouncementReaction Reaction)>> GetReactionsAsync(
    List<int> announcementIds)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var result = new List<(int, AnnouncementReaction)>();

        if (announcementIds == null || announcementIds.Count == 0)
        {
            return result;
        }

        var idParameters = string.Join(",", announcementIds.Select((_, i) => $"@id{i}"));

        var query = $@"
        SELECT ar.Id, ar.AnnouncementId, ar.Emoji, ar.UserId, u.Name AS UserName
        FROM AnnouncementReactions ar
        INNER JOIN Users u ON ar.UserId = u.Id
        WHERE ar.AnnouncementId IN ({idParameters})";

        using var command = new SqlCommand(query, connection);

        for (int i = 0; i < announcementIds.Count; i++)
        {
            command.Parameters.AddWithValue($"@id{i}", announcementIds[i]);
        }

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var announcementId = reader.GetInt32(reader.GetOrdinal("AnnouncementId"));

            var reaction = new AnnouncementReaction
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Emoji = reader.GetString(reader.GetOrdinal("Emoji")),
                AnnouncementId = announcementId,
                Author = new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Name = reader.GetString(reader.GetOrdinal("UserName")),
                },
            };

            result.Add((announcementId, reaction));
        }

        return result;
    }
}
