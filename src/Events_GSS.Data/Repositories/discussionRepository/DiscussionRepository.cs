using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories;

public class DiscussionRepository : IDiscussionRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    public DiscussionRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    // ── Messages ──────────────────────────────────────────────────────────────
    public async Task<List<DiscussionMessage>> GetByEventAsync(int eventId, int currentUserId)
    {
        var messages = new List<DiscussionMessage>();

        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string messageSelectionQuery = @"
            SELECT
                d.DiscussionId,  d.Message,  d.MediaPath,
                d.Date,          d.IsEdited,
                d.ReplyToId,
                u.Id  AS AuthorId,   u.Name AS AuthorName,
                r.DiscussionId   AS ReplyId,
                r.Message        AS ReplyMessage,
                ru.Id            AS ReplyAuthorId,
                ru.Name          AS ReplyAuthorName
            FROM Discussions d
            INNER JOIN Users u  ON d.UserId = u.Id
            LEFT  JOIN Discussions r ON d.ReplyToId = r.DiscussionId
            LEFT  JOIN Users ru     ON r.UserId    = ru.Id
            WHERE d.EventId = @EventId
            ORDER BY d.Date ASC";

        using (var cmd = new SqlCommand(messageSelectionQuery, conn))
        {
            cmd.Parameters.AddWithValue("@EventId", eventId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var msg = new DiscussionMessage(
                    id: reader.GetInt32(reader.GetOrdinal("DiscussionId")),
                    message: reader.IsDBNull(reader.GetOrdinal("Message"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Message")),
                    date: reader.GetDateTime(reader.GetOrdinal("Date")))
                {
                    MediaPath = reader.IsDBNull(reader.GetOrdinal("MediaPath"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("MediaPath")),
                    IsEdited = reader.GetBoolean(reader.GetOrdinal("IsEdited")),
                    Author = new User
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("AuthorId")),
                        Name = reader.GetString(reader.GetOrdinal("AuthorName"))
                    }
                };

                if (!reader.IsDBNull(reader.GetOrdinal("ReplyId")))
                {
                    msg.ReplyTo = new DiscussionMessage(
                        id: reader.GetInt32(reader.GetOrdinal("ReplyId")),
                        message: reader.IsDBNull(reader.GetOrdinal("ReplyMessage"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ReplyMessage")),
                        date: DateTime.MinValue)
                    {
                        Author = new User
                        {
                            UserId = reader.GetInt32(reader.GetOrdinal("ReplyAuthorId")),
                            Name = reader.GetString(reader.GetOrdinal("ReplyAuthorName"))
                        }
                    };
                }

                messages.Add(msg);
            }
        }

        if (messages.Count == 0)
        {
            return messages;
        }
        // Batch-load reactions
        var messageIds = messages.Select(m => m.Id).ToList();
        var idParams = string.Join(",", messageIds.Select((_, i) => $"@mid{i}"));

        var reactionSelectionQuery = $@"
            SELECT dr.Id, dr.MessageId, dr.Emoji, dr.UserId, u.Name AS UserName
            FROM DiscussionReactions dr
            INNER JOIN Users u ON dr.UserId = u.Id
            WHERE dr.MessageId IN ({idParams})";

        var allReactions = new List<(int MessageId, DiscussionReaction Reaction)>();

        using (var cmd = new SqlCommand(reactionSelectionQuery, conn))
        {
            for (int i = 0; i < messageIds.Count; i++)
            {
                cmd.Parameters.AddWithValue($"@mid{i}", messageIds[i]);
            }
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var messageId = reader.GetInt32(reader.GetOrdinal("MessageId"));
                var reaction = new DiscussionReaction
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Emoji = reader.GetString(reader.GetOrdinal("Emoji")),
                    Message = new DiscussionMessage(messageId, null, DateTime.MinValue),
                    Author = new User
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Name = reader.GetString(reader.GetOrdinal("UserName"))
                    }
                };
                allReactions.Add((messageId, reaction));
            }
        }

        var reactionsByMessage = allReactions
            .GroupBy(r => r.MessageId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Reaction).ToList());

        foreach (var message in messages)
        {
            if (reactionsByMessage.TryGetValue(message.Id, out var reactions))
            {
                message.Reactions = reactions;
            }
        }

        return messages;
    }

    public async Task<DiscussionMessage?> GetByIdAsync(int messageId)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            SELECT d.DiscussionId, d.Message, d.Date, d.UserId,
                   u.Name AS AuthorName
            FROM Discussions d
            INNER JOIN Users u ON d.UserId = u.Id
            WHERE d.DiscussionId = @Id";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", messageId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new DiscussionMessage(
            id: reader.GetInt32(reader.GetOrdinal("DiscussionId")),
            message: reader.IsDBNull(reader.GetOrdinal("Message"))
                ? null
                : reader.GetString(reader.GetOrdinal("Message")),
            date: reader.GetDateTime(reader.GetOrdinal("Date")))
        {
            Author = new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Name = reader.GetString(reader.GetOrdinal("AuthorName"))
            }
        };
    }

    public async Task<int> AddAsync(DiscussionMessage message)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            INSERT INTO Discussions
                (EventId, UserId, Message, MediaPath, Date, ReplyToId, IsEdited)
            OUTPUT INSERTED.DiscussionId
            VALUES
                (@EventId, @UserId, @Message, @MediaPath, @Date, @ReplyToId, 0)";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", message.AssociatedEvent!.EventId);
        cmd.Parameters.AddWithValue("@UserId", message.Author!.UserId);
        cmd.Parameters.AddWithValue("@Message", (object?)message.Message ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MediaPath", (object?)message.MediaPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Date", message.DateCreated);
        cmd.Parameters.AddWithValue("@ReplyToId",
            message.ReplyTo is not null ? message.ReplyTo.Id : DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task DeleteAsync(int messageId)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string detachReplies = @"
            UPDATE Discussions SET ReplyToId = NULL WHERE ReplyToId = @Id";

        using (var cmd = new SqlCommand(detachReplies, conn))
        {
            cmd.Parameters.AddWithValue("@Id", messageId);
            await cmd.ExecuteNonQueryAsync();
        }

        const string delete = @"DELETE FROM Discussions WHERE DiscussionId = @Id";

        using (var cmd = new SqlCommand(delete, conn))
        {
            cmd.Parameters.AddWithValue("@Id", messageId);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<DateTime?> GetLastUserMessageDateAsync(int eventId, int userId)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            SELECT TOP 1 Date
            FROM Discussions
            WHERE EventId = @EventId AND UserId = @UserId
            ORDER BY Date DESC";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);
        cmd.Parameters.AddWithValue("@UserId", userId);

        var result = await cmd.ExecuteScalarAsync();
        return result is DateTime dt ? dt : null;
    }

    // ── Reactions ─────────────────────────────────────────────────────────────
    public async Task AddReactionAsync(int messageId, int userId, string emoji)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            IF EXISTS (SELECT 1 FROM DiscussionReactions WHERE MessageId = @MsgId AND UserId = @UserId)
                UPDATE DiscussionReactions
                SET Emoji = @Emoji
                WHERE MessageId = @MsgId AND UserId = @UserId
            ELSE
                INSERT INTO DiscussionReactions (MessageId, UserId, Emoji)
                VALUES (@MsgId, @UserId, @Emoji)";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@MsgId", messageId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Emoji", emoji);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveReactionAsync(int messageId, int userId)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            DELETE FROM DiscussionReactions
            WHERE MessageId = @MsgId AND UserId = @UserId";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@MsgId", messageId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<DiscussionReaction>> GetReactionsAsync(int messageId)
    {
        var reactions = new List<DiscussionReaction>();

        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            SELECT dr.Id, dr.Emoji, dr.UserId, u.Name AS UserName
            FROM DiscussionReactions dr
            INNER JOIN Users u ON dr.UserId = u.Id
            WHERE dr.MessageId = @MsgId";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@MsgId", messageId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reactions.Add(new DiscussionReaction
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Emoji = reader.GetString(reader.GetOrdinal("Emoji")),
                Message = new DiscussionMessage(messageId, null, DateTime.MinValue),
                Author = new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Name = reader.GetString(reader.GetOrdinal("UserName"))
                }
            });
        }

        return reactions;
    }

    // ── Mutes ─────────────────────────────────────────────────────────────────
    public async Task<DiscussionMute?> GetMuteAsync(int eventId, int userId)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            SELECT dm.Id, dm.MutedUntil, dm.IsPermanent, dm.CreatedAt,
                   dm.MutedByUserId, mb.Name AS MutedByName
            FROM DiscussionMutes dm
            INNER JOIN Users mb ON dm.MutedByUserId = mb.Id
            WHERE dm.EventId = @EventId AND dm.MutedUserId = @UserId";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new DiscussionMute
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            EventId = eventId,
            MutedUser = new User { UserId = userId },
            MutedBy = new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("MutedByUserId")),
                Name = reader.GetString(reader.GetOrdinal("MutedByName"))
            },
            MutedUntil = reader.IsDBNull(reader.GetOrdinal("MutedUntil"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("MutedUntil")),
            IsPermanent = reader.GetBoolean(reader.GetOrdinal("IsPermanent")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    public async Task MuteAsync(DiscussionMute mute)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string remove = @"
            DELETE FROM DiscussionMutes
            WHERE EventId = @EventId AND MutedUserId = @UserId";

        using (var cmd = new SqlCommand(remove, conn))
        {
            cmd.Parameters.AddWithValue("@EventId", mute.EventId);
            cmd.Parameters.AddWithValue("@UserId", mute.MutedUser.UserId);
            await cmd.ExecuteNonQueryAsync();
        }

        const string insert = @"
            INSERT INTO DiscussionMutes
                (EventId, MutedUserId, MutedByUserId, MutedUntil, IsPermanent, CreatedAt)
            VALUES
                (@EventId, @MutedUserId, @MutedByUserId, @MutedUntil, @IsPermanent, @CreatedAt)";

        using (var cmd = new SqlCommand(insert, conn))
        {
            cmd.Parameters.AddWithValue("@EventId", mute.EventId);
            cmd.Parameters.AddWithValue("@MutedUserId", mute.MutedUser.UserId);
            cmd.Parameters.AddWithValue("@MutedByUserId", mute.MutedBy.UserId);
            cmd.Parameters.AddWithValue("@MutedUntil", (object?)mute.MutedUntil ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsPermanent", mute.IsPermanent);
            cmd.Parameters.AddWithValue("@CreatedAt", mute.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task UnmuteAsync(int eventId, int userId)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            DELETE FROM DiscussionMutes
            WHERE EventId = @EventId AND MutedUserId = @UserId";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Slow Mode ─────────────────────────────────────────────────────────────
    public async Task SetSlowModeAsync(int eventId, int? seconds)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync();

        const string query = @"
            UPDATE Events SET SlowModeSeconds = @Seconds WHERE EventId = @EventId";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);
        cmd.Parameters.AddWithValue("@Seconds", (object?)seconds ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Participants ──────────────────────────────────────────────────────────
    public async Task<List<User>> GetEventParticipantsAsync(int eventId)
    {
        var users = new List<User>();

        using var conn = connectionFactory.CreateConnection();
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
