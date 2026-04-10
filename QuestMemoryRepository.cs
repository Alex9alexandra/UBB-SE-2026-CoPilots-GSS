using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

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

            using SqlCommand command = new SqlCommand(addQuery, connection);

            command.Parameters.AddWithValue("@EventId", proofMemory.Event.EventId);
            command.Parameters.AddWithValue("@UserId", proofMemory.Author.UserId);
            command.Parameters.AddWithValue("@PhotoPath", (object?)proofMemory.PhotoPath ?? DBNull.Value);
            command.Parameters.AddWithValue("@Text", (object?)proofMemory.Text ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", proofMemory.CreatedAt);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception exception)
        {
            throw new Exception("Error creating memory.", exception);
        }
    }

    public async Task SubmitProofAsync(Quest quest, Memory proof)
    {
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string insertQuery = @"
                INSERT INTO QuestMemories (QuestId, MemoryId, Status)
                VALUES (@QuestId, @MemoryId, 'Submitted')";

            using SqlCommand command = new SqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@QuestId", quest.Id);
            command.Parameters.AddWithValue("@MemoryId", proof.MemoryId);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception exception)
        {
            throw new Exception("Error submitting proof.", exception);
        }
    }

    public async Task<List<QuestMemory>> GetRawSubmissionsForUser(User user)
    {
        var results = new List<QuestMemory>();
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string query = @"
                SELECT qm.QuestId, qm.Status, qm.MemoryId
                FROM QuestMemories qm
                INNER JOIN Memories m ON qm.MemoryId = m.MemoryId
                WHERE m.UserId = @UserId";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", user.UserId);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                QuestMemoryStatus.TryParse((string)reader["Status"], out QuestMemoryStatus status);
                results.Add(new QuestMemory
                {
                    ForQuest = new Quest { Id = (int)reader["QuestId"] },
                    Proof = new Memory { MemoryId = (int)reader["MemoryId"] },
                    ProofStatus = status
                });
            }
        }
        catch (Exception exception)
        {
            throw new Exception("Error retrieving raw submissions.", exception);
        }

        return results;
    }

    public async Task<List<QuestMemory>> GetProofsForQuestAsync(Quest quest)
    {
        var proofs = new List<QuestMemory>();
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string query = @"
                SELECT qm.MemoryId, qm.Status, m.UserId, m.PhotoPath, m.Text, m.CreatedAt, m.EventId
                FROM QuestMemories qm
                JOIN Memories m ON qm.MemoryId = m.MemoryId
                WHERE qm.QuestId = @QuestId AND qm.Status = 'Submitted'";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuestId", quest.Id);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
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
        catch (Exception exception)
        {
            throw new Exception("Error retrieving proofs.", exception);
        }
        return proofs;
    }

    public async Task ChangeProofStatusAsync(QuestMemory proof)
    {
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();
            const string query = "UPDATE QuestMemories SET Status = @Status WHERE QuestId = @QuestId AND MemoryId = @MemoryId";
            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Status", proof.ProofStatus.ToString());
            command.Parameters.AddWithValue("@QuestId", proof.ForQuest.Id);
            command.Parameters.AddWithValue("@MemoryId", proof.Proof!.MemoryId);
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception exception)
        {
            throw new Exception("Error updating status.", exception);
        }
    }

    public async Task DeleteProofAsync(QuestMemory proof)
    {
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();
            const string query = "DELETE FROM QuestMemories WHERE QuestId = @QuestId AND MemoryId = @MemoryId";
            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuestId", proof.ForQuest.Id);
            command.Parameters.AddWithValue("@MemoryId", proof.Proof!.MemoryId);
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception exception)
        {
            throw new Exception("Error deleting proof.", exception);
        }
    }
}