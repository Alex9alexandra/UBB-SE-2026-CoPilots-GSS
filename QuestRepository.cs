using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Diagnostics;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories;

public class QuestRepository : IQuestRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public QuestRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> AddQuestAsync(Event toEvent, Quest quest)
    {
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string addQuery = @"
                INSERT INTO Quests (Name, Description, Difficulty, EventId, PrerequisiteQuestId)
                OUTPUT INSERTED.QuestId
                VALUES (@Name, @Description, @Difficulty, @EventId, @PrereqId)";

            using SqlCommand command = new SqlCommand(addQuery, connection);

            command.Parameters.AddWithValue("@Name", quest.Name);
            command.Parameters.AddWithValue("@Description", quest.Description);
            command.Parameters.AddWithValue("@Difficulty", quest.Difficulty);
            command.Parameters.AddWithValue("@EventId", toEvent.EventId);

            if (quest.PrerequisiteQuest is null)
            {
                command.Parameters.AddWithValue("@PrereqId", DBNull.Value);
            }
            else
            {
                command.Parameters.AddWithValue("@PrereqId", quest.PrerequisiteQuest.Id);
            }

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (SqlException sqlException)
        {
            Debug.WriteLine($"SQL Exception: {sqlException.Message}");
            throw;
        }
        catch (Exception exception)
        {
            throw new Exception("An unexpected error occurred while creating the quest.", exception);
        }
    }

    public async Task<List<Quest>> GetQuestsAsync(Event fromEvent)
    {
        var quests = new List<Quest>();

        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string query =
                    "SELECT " +
                    "quest.*, " +
                    "prerequisite.Name AS P_Name, " +
                    "prerequisite.Description AS P_Description, " +
                    "prerequisite.Difficulty AS P_Difficulty " +
                    "FROM Quests quest " +
                    "LEFT JOIN Quests prerequisite ON quest.PrerequisiteQuestId = prerequisite.QuestId " +
                    "WHERE quest.EventId = @EventId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventId", fromEvent.EventId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                quests.Add(await MapQuestFromReader(reader));
            }

            return quests;
        }
        catch (SqlException sqlException)
        {
            Debug.WriteLine($"SQL Exception: {sqlException.Message}");
            throw;
        }
        catch (Exception exception)
        {
            throw new Exception("An unexpected error occurred while retrieving quests.", exception);
        }
    }

    public async Task<Quest> GetQuestByIdAsync(int questId)
    {
        using SqlConnection connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();

            const string query = "SELECT * FROM Quests WHERE QuestId = @QuestId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuestId", questId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return await MapQuestFromReader(reader);
            }
            return null;
        }
        catch (SqlException sqlException)
        {
            Debug.WriteLine($"SQL Exception: {sqlException.Message}");
            throw;
        }
        catch (Exception exception)
        {
            throw new Exception("An unexpected error occurred while retrieving quest.", exception);
        }
    }

    public async Task DeleteQuestAsync(Quest quest)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string deleteMemoryQuery = "DELETE FROM QuestMemories WHERE QuestId = @QuestId";
        const string deleteQuestQuery = "DELETE FROM Quests WHERE QuestId = @QuestId";

        using var memoryCommand = new SqlCommand(deleteMemoryQuery, connection);
        memoryCommand.Parameters.AddWithValue("@QuestId", quest.Id);
        await memoryCommand.ExecuteNonQueryAsync();

        using var questCommand = new SqlCommand(deleteQuestQuery, connection);
        questCommand.Parameters.AddWithValue("@QuestId", quest.Id);
        await questCommand.ExecuteNonQueryAsync();
    }

    private async Task<Quest> MapQuestFromReader(SqlDataReader reader)
    {
        var quest = new Quest
        {
            Id = reader.GetInt32(reader.GetOrdinal("QuestId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Description = reader.GetString(reader.GetOrdinal("Description")),
            Difficulty = reader.GetInt32(reader.GetOrdinal("Difficulty")),
        };

        int prerequisiteOrdinal = reader.GetOrdinal("PrerequisiteQuestId");
        if (!reader.IsDBNull(prerequisiteOrdinal))
        {
            var prerequisiteQuest = new Quest
            {
                Id = reader.GetInt32(reader.GetOrdinal("PrerequisiteQuestId")),
                Name = reader.GetString(reader.GetOrdinal("P_Name")),
                Description = reader.GetString(reader.GetOrdinal("P_Description")),
                Difficulty = reader.GetInt32(reader.GetOrdinal("P_Difficulty")),
            };
            quest.PrerequisiteQuest = prerequisiteQuest;
        }

        return quest;
    }
}