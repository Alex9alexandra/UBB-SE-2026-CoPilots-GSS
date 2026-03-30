using System.Collections.Generic;
using System.Threading.Tasks;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories.Interfaces;

public interface IQuestRepository
{
    /// <summary>
    /// Adds a new quest to the database and returns the assigned ID.
    /// </summary>
    Task<int> AddQuestAsync(Event toEvent,Quest quest);

    /// <summary>
    /// Retrieves all quests associated with a specific event ID.
    /// </summary>
    Task<List<Quest>> GetQuestsAsync(Event fromEvent);

    /// <summary>
    /// Retrieves quests associated with a specific event ID.
    /// </summary>
    Task<Quest> GetQuestByIdAsync(int questId);

    /// <summary>
    /// Deletes a quest from the database by its unique identifier.
    /// </summary>
    Task DeleteQuestAsync(Quest quest);
    
}