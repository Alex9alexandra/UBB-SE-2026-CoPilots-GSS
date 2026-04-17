// <copyright file="MemoryRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;

    using Events_GSS.Data.Database;
    using Events_GSS.Data.Models;

    using Microsoft.Data.SqlClient;

    /// <summary>
    /// Repository for memory data.
    /// </summary>
    public class MemoryRepository : IMemoryRepository
    {
        private const string GetMemoriesByEventQuery = @"
            SELECT m.MemoryId, m.UserId, m.PhotoPath, m.Text, m.CreatedAt,
                   e.EventId, e.Name, e.AdminId
            FROM Memories m
            INNER JOIN Events e ON e.EventId = m.EventId
            WHERE m.EventId = @EventId
            ORDER BY m.CreatedAt DESC";

        private const string InsertMemoryQuery = @"
            INSERT INTO Memories (EventId, UserId, PhotoPath, Text, CreatedAt)
            OUTPUT INSERTED.MemoryId
            VALUES (@EventId, @UserId, @PhotoPath, @Text, @CreatedAt)";

        private const string DeleteMemoryQuery = "DELETE FROM Memories WHERE MemoryId = @MemoryId";

        private const string AddLikeQuery = "INSERT INTO MemoryLikes (MemoryId, UserId) VALUES (@MemoryId, @UserId)";

        private const string RemoveLikeQuery = "DELETE FROM MemoryLikes WHERE MemoryId = @MemoryId AND UserId = @UserId";

        private const string GetByIdQuery = @"
            SELECT m.MemoryId, m.PhotoPath, m.Text, m.CreatedAt,
                   e.EventId, e.Name as EventName, e.AdminId as CreatedById,
                   u.Id as AuthorId, u.Name as AuthorName, u.Email as AuthorEmail
            FROM Memories m
            INNER JOIN Events e ON e.EventId = m.EventId
            INNER JOIN Users u ON u.Id = m.UserId
            WHERE m.MemoryId = @MemoryId";

        private const string GetLikesQuery = "SELECT UserId FROM MemoryLikes WHERE MemoryId = @MemoryId";

        private readonly SqlConnectionFactory connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        public MemoryRepository(SqlConnectionFactory connectionFactory)
        {
            // THIS is how we fix the underscore error!
            this.connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Gets memories by event ID.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <returns>A list of memories.</returns>
        public async Task<List<Memory>> GetByEventAsync(int eventId)
        {
            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand(GetMemoriesByEventQuery, connection);

            command.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;

            using var reader = await command.ExecuteReaderAsync();
            var memories = new List<Memory>();

            while (await reader.ReadAsync())
            {
                memories.Add(new Memory
                {
                    MemoryId = (int)reader["MemoryId"],
                    PhotoPath = reader["PhotoPath"] == DBNull.Value ? null : (string)reader["PhotoPath"],
                    Text = reader["Text"] == DBNull.Value ? null : (string)reader["Text"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    Event = new Event
                    {
                        EventId = (int)reader["EventId"],
                        Name = (string)reader["Name"],
                        Admin = new User { UserId = (int)reader["AdminId"] },
                    },
                    Author = new User { UserId = (int)reader["UserId"] },
                });
            }

            return memories;
        }

        /// <summary>
        /// Adds a memory to the database.
        /// </summary>
        /// <param name="memory">The memory to add.</param>
        /// <returns>The ID of the new memory.</returns>
        public async Task<int> AddAsync(Memory memory)
        {
            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand(InsertMemoryQuery, connection);

            command.Parameters.Add("@EventId", SqlDbType.Int).Value = memory.Event.EventId;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = memory.Author.UserId;
            command.Parameters.Add("@PhotoPath", SqlDbType.NVarChar).Value = (object?)memory.PhotoPath ?? DBNull.Value;
            command.Parameters.Add("@Text", SqlDbType.NVarChar).Value = (object?)memory.Text ?? DBNull.Value;
            command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = memory.CreatedAt;

            var result = await command.ExecuteScalarAsync();
            return (int)result!;
        }

        /// <summary>
        /// Deletes a memory from the database.
        /// </summary>
        /// <param name="memoryId">The ID of the memory.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task DeleteAsync(int memoryId)
        {
            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand(DeleteMemoryQuery, connection);
            command.Parameters.Add("@MemoryId", SqlDbType.Int).Value = memoryId;
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Adds a like to a memory.
        /// </summary>
        /// <param name="memoryId">The memory ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task AddLikeAsync(int memoryId, int userId)
        {
            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand(AddLikeQuery, connection);
            command.Parameters.Add("@MemoryId", SqlDbType.Int).Value = memoryId;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Removes a like from a memory.
        /// </summary>
        /// <param name="memoryId">The memory ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task RemoveLikeAsync(int memoryId, int userId)
        {
            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand(RemoveLikeQuery, connection);
            command.Parameters.Add("@MemoryId", SqlDbType.Int).Value = memoryId;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Gets the list of user IDs who liked a memory.
        /// </summary>
        /// <param name="memoryId">The memory ID.</param>
        /// <returns>A list of user IDs.</returns>
        public async Task<List<int>> GetLikesAsync(int memoryId)
        {
            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand(GetLikesQuery, connection);
            command.Parameters.Add("@MemoryId", SqlDbType.Int).Value = memoryId;

            using var reader = await command.ExecuteReaderAsync();
            var userIds = new List<int>();

            while (await reader.ReadAsync())
            {
                userIds.Add((int)reader["UserId"]);
            }

            return userIds;
        }

        /// <summary>
        /// Gets a memory by its ID.
        /// </summary>
        /// <param name="memoryId">The memory ID.</param>
        /// <returns>The memory object.</returns>
        public async Task<Memory?> GetByIdAsync(int memoryId)
        {
            using var connection = this.connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand(GetByIdQuery, connection);
            command.Parameters.Add("@MemoryId", SqlDbType.Int).Value = memoryId;

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

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
                    Admin = new User { UserId = (int)reader["CreatedById"] },
                },
                Author = new User
                {
                    UserId = (int)reader["AuthorId"],
                    Name = (string)reader["AuthorName"],
                },
            };
        }
    }
}