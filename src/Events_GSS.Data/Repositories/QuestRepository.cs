using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.Interfaces;

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
        try{
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
                command.Parameters.AddWithValue("@PrereqId", DBNull.Value);
            else
                command.Parameters.AddWithValue("@PrereqId", quest.PrerequisiteQuest.Id);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch(SqlException ex)
        {
            Console.WriteLine($"SQL Exception: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while creating the quest.", ex);
        }
    }

    public async Task<List<Quest>> GetQuestsAsync(Event fromEvent)
    {
        var quests = new List<Quest>();

        using SqlConnection connection = _connectionFactory.CreateConnection();
        try{
            await connection.OpenAsync();

            const string query = 
                    "SELECT " +
                    "q.*, "+ 
                    "p.Name AS P_Name, "+
                    "p.Description AS P_Description, "+
                    "p.Difficulty AS P_Difficulty "+
                    "FROM Quests q "+
                    "LEFT JOIN Quests p ON q.PrerequisiteQuestId = p.QuestId "+
                    "WHERE q.EventId = @EventId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventId", fromEvent.EventId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                quests.Add(await MapQuestFromReader(reader));
            }

            return quests;
        }
        catch(SqlException ex)
        {
            Console.WriteLine($"SQL Exception: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while retrieving quests.", ex);
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
            
            Quest quest= await MapQuestFromReader(reader);
            return quest;
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SQL Exception: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while retrieving quest.", ex);
        }
    }
    
    public async Task DeleteQuestAsync(Quest quest)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string query = "DELETE FROM Quests WHERE QuestId = @QuestId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@QuestId", quest.Id);

        await command.ExecuteNonQueryAsync();
    }

    // Helpers
    private async Task<Quest> MapQuestFromReader(SqlDataReader reader)
    {
        var quest = new Quest
        {
            Id = reader.GetInt32(reader.GetOrdinal("QuestId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Description = reader.GetString(reader.GetOrdinal("Description")),
            Difficulty = reader.GetInt32(reader.GetOrdinal("Difficulty")),

        };

        int prereqOrdinal = reader.GetOrdinal("PrerequisiteQuestId");
        if (!reader.IsDBNull(prereqOrdinal))
        {

            var prereqQuest = new Quest
            {
                Id=reader.GetInt32(reader.GetOrdinal("PrerequisiteQuestId")),
                Name = reader.GetString(reader.GetOrdinal("P_Name")),
                Description = reader.GetString(reader.GetOrdinal("P_Description")),
                Difficulty = reader.GetInt32(reader.GetOrdinal("P_Difficulty")),
            };
            quest.PrerequisiteQuest = prereqQuest;

        }
        

        return quest;
    }
}