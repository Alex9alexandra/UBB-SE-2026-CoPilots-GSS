using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.Interfaces;
using Events_GSS.Data.Database;

namespace Events_GSS.Data.Repositories
{

    public class MemoryRepository : IMemoryRepository
    {
        private readonly SqlConnectionFactory _factory;

        public MemoryRepository(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<Memory>> GetByEventAsync(int eventId)
        {
            string getMemoriesByEventQuery = @"SELECT MemoryId, UserId, PhotoPath, Text, CreatedAt 
                                               FROM Memories 
                                               WHERE EventId = @EventId
                                               ORDER BY CreatedAt DESC";

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(getMemoriesByEventQuery, conn);
            cmd.Parameters.AddWithValue("@EventId", eventId);
            using var reader = await cmd.ExecuteReaderAsync();

            var memories = new List<Memory>();
            while (await reader.ReadAsync())
            {
                memories.Add(new Memory
                {
                    MemoryId = (int)reader["MemoryId"],
                    PhotoPath = reader["PhotoPath"] == DBNull.Value ? null : (string)reader["PhotoPath"],
                    Text = reader["Text"] == DBNull.Value ? null : (string)reader["Text"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    Author = new User { UserId = (int)reader["UserId"] }
                });
            }
            return memories;
        }

        public async Task<int> AddAsync(Memory memory)
        {
            string insertMemorySql = @"
                INSERT INTO Memories (EventId, UserId, PhotoPath, Text, CreatedAt)
                OUTPUT INSERTED.MemoryId
                VALUES (@EventId, @UserId, @PhotoPath, @Text, @CreatedAt)";

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(insertMemorySql, conn);
            cmd.Parameters.AddWithValue("@EventId", memory.Event.EventId);
            cmd.Parameters.AddWithValue("@UserId", memory.Author.UserId);
            cmd.Parameters.AddWithValue("@PhotoPath", (object?)memory.PhotoPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Text", (object?)memory.Text ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", memory.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            return (int)result!;
        }

        public async Task DeleteAsync(int memoryId)
        {
            string deleteMemorySql = "DELETE FROM Memories WHERE MemoryId = @MemoryId";

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(deleteMemorySql, conn);
            cmd.Parameters.AddWithValue("@MemoryId", memoryId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddLikeAsync(int memoryId, int userId)
        {
            string addLikeSql = @"INSERT INTO MemoryLikes (MemoryId, UserId) VALUES (@MemoryId, @UserId)";

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(addLikeSql, conn);
            cmd.Parameters.AddWithValue("@MemoryId", memoryId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RemoveLikeAsync(int memoryId, int userId)
        {
            string removeLikeSql = @"DELETE FROM MemoryLikes WHERE MemoryId = @MemoryId AND UserId = @UserId";

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(removeLikeSql, conn);
            cmd.Parameters.AddWithValue("@MemoryId", memoryId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> GetLikesCountAsync(int memoryId)
        {
            string getLikesCountForSpecificMemory = @"SELECT COUNT(*) FROM MemoryLikes WHERE MemoryId = @MemoryId";
            
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(getLikesCountForSpecificMemory, conn);
            cmd.Parameters.AddWithValue("@MemoryId", memoryId);

            var result = await cmd.ExecuteScalarAsync();
            return (int)result!;
        }

        public async Task<bool> HasLikedAsync(int memoryId, int userId)
        {
            string sql = @"
                SELECT COUNT(1)
                FROM MemoryLikes
                WHERE MemoryId = @MemoryId AND UserId = @UserId";

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MemoryId", memoryId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            var result = await cmd.ExecuteScalarAsync();
            return (int)result! > 0;
        }
        public async Task<Memory?> GetByIdAsync(int memoryId)
        {
            string sql = @"
        SELECT m.MemoryId, m.PhotoPath, m.Text, m.CreatedAt,
               e.EventId, e.Name as EventName, e.AdminId as CreatedById,
               u.Id as AuthorId, u.Name as AuthorName, u.Email as AuthorEmail
        FROM Memories m
        INNER JOIN Events e ON e.EventId = m.EventId
        INNER JOIN Users u ON u.Id = m.UserId
        WHERE m.MemoryId = @MemoryId";

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MemoryId", memoryId);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new Memory
            {
                MemoryId = (int)reader["MemoryId"],
                PhotoPath = reader["PhotoPath"] == DBNull.Value ? null : (string)reader["PhotoPath"],
                Text = reader["Text"] == DBNull.Value ? null : (string)reader["Text"],
                CreatedAt = (DateTime)reader["CreatedAt"],
                Event = new Event
                {
                    EventId = (int)reader["EventId"],
                    Name = (string)reader["EventName"],
                    Admin = new User { UserId = (int)reader["CreatedById"] }
                },
                Author = new User
                {
                    UserId = (int)reader["AuthorId"],
                    Name = (string)reader["AuthorName"],
                }
            };
        }

    }
}