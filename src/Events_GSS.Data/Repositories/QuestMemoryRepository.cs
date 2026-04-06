using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories;

public class QuestMemoryRepository : IQuestMemoryRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public QuestMemoryRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> AddMemoryAsync(Memory proofMemory)
    {
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string addQuery = @"
                INSERT INTO Memories (EventId, UserId, PhotoPath, Text, CreatedAt)
                OUTPUT INSERTED.MemoryId
                VALUES (@EventId, @UserId, @PhotoPath, @Text, @CreatedAt)";

            using SqlCommand cmd = new SqlCommand(addQuery, connection);

            cmd.Parameters.AddWithValue("@EventId", proofMemory.Event.EventId);
            cmd.Parameters.AddWithValue("@UserId", proofMemory.Author.UserId);
            cmd.Parameters.AddWithValue("@PhotoPath", (object?)proofMemory.PhotoPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Text", (object?)proofMemory.Text ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", proofMemory.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SQL Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while creating the memory as proof.", ex);
        }
    }

    public async Task SubmitProofAsync(Quest quest, Memory proof)
    {
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string insertQuery = @"
                INSERT INTO QuestMemories (QuestId, MemoryId)
                VALUES (@QuestId, @MemoryId)";

            using SqlCommand cmd = new SqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("@QuestId", quest.Id);
            cmd.Parameters.AddWithValue("@MemoryId", proof.MemoryId);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            Debug.WriteLine($"SQL Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while submitting the proof.", ex);
        }
    }

    public async Task<List<QuestMemory>> GetSubmissionsStatusForUser(List<Quest> quests, User user)
    {
        var proofs = new List<QuestMemory>();

        if (quests.Count == 0) return proofs;

        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            
            var questIds = quests.Select(q => q.Id).ToList();
            var paramNames = questIds.Select((_, i) => $"@QuestId{i}").ToList();
            var inClause = string.Join(",", paramNames);

            string query = $@"
                SELECT qm.QuestId, qm.Status, qm.MemoryId
                FROM QuestMemories qm
                INNER JOIN Memories m ON qm.MemoryId = m.MemoryId
                WHERE qm.QuestId IN ({inClause})
                AND m.UserId = @UserId";

            using SqlCommand cmd = new SqlCommand(query, connection);

            for (int i = 0; i < questIds.Count; i++)
                cmd.Parameters.AddWithValue(paramNames[i], questIds[i]);

            cmd.Parameters.AddWithValue("@UserId", user.UserId);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                QuestMemoryStatus proofStatus;
                QuestMemoryStatus.TryParse((string)reader["Status"], out proofStatus);
                proofs.Add(new QuestMemory
                {
                    ForQuest = quests.Find(q => q.Id == (int)reader["QuestId"])!,
                    Proof = new Memory{MemoryId = (int)reader["MemoryId"]},
                    ProofStatus = proofStatus
                });
            }
        }
        catch (SqlException ex)
        {
            Debug.WriteLine($"SQL Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while retrieving the proofs.", ex);
        }
        Debug.WriteLine($"Retrieved {proofs.Count} proofs for user {user.UserId}");
        return proofs;
    }

    public async Task<List<QuestMemory>> GetProofsForQuestAsync(Quest quest)
    {
        var proofs = new List<QuestMemory>();
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string query = @"
                SELECT qm.QuestMemoryId, qm.QuestId, qm.MemoryId, qm.Status,
                       m.UserId, m.PhotoPath, m.Text, m.CreatedAt, m.EventId
                FROM QuestMemories qm
                JOIN Memories m ON qm.MemoryId = m.MemoryId
                WHERE qm.QuestId = @QuestId
                AND qm.Status='Submitted'";

            using SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@QuestId", quest.Id);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                proofs.Add(new QuestMemory
                {
                    ForQuest = quest,
                    Proof = new Memory
                    {
                        MemoryId = (int)reader["MemoryId"],
                        Author = new User { UserId = (int)reader["UserId"] },
                        PhotoPath = reader["PhotoPath"] == DBNull.Value ? null : (string)reader["PhotoPath"],
                        Text = reader["Text"] == DBNull.Value ? null : (string)reader["Text"],
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        Event = new Event { EventId = (int)reader["EventId"] }
                    },
                    ProofStatus = QuestMemoryStatus.Submitted
                });
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SQL Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while retrieving the proofs.", ex);
        }

        return proofs;
    }

    public async Task ChangeProofStatusAsync(QuestMemory proof)
    {
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string updateQuery = @"
                UPDATE QuestMemories
                SET Status = @Status
                WHERE QuestId = @QuestId AND MemoryId = @MemoryId";

            using SqlCommand cmd = new SqlCommand(updateQuery, connection);
            cmd.Parameters.AddWithValue("@Status", proof.ProofStatus.ToString());
            cmd.Parameters.AddWithValue("@QuestId", proof.ForQuest.Id);
            cmd.Parameters.AddWithValue("@MemoryId", proof.Proof!.MemoryId);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            Debug.WriteLine($"SQL Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while changing proof status.", ex);
        }
    }

    public async Task DeleteProofAsync(QuestMemory proof)
    {
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string deleteQuery = @"
                DELETE FROM QuestMemories
                WHERE QuestId = @QuestId AND MemoryId = @MemoryId";

            using SqlCommand cmd = new SqlCommand(deleteQuery, connection);
            cmd.Parameters.AddWithValue("@QuestId", proof.ForQuest.Id);
            cmd.Parameters.AddWithValue("@MemoryId", proof.Proof!.MemoryId);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            Debug.WriteLine($"SQL Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while deleting the proof.", ex);
        }
    }
}