using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.Interfaces;
using Events_GSS.Data.Services.Interfaces;

namespace Events_GSS.Data.Services;

public class QuestService : IQuestService
{
    private readonly IQuestRepository _questRepository;

    public QuestService(IQuestRepository questRepository)
    {
        _questRepository = questRepository;
    }

    /// <summary>
    /// Adds a new quest. Does not matter if quest is preset or custom.
    /// </summary>
    public async Task<int> AddQuestAsync(Event toEvent , Quest quest)
    {
        return await _questRepository.AddQuestAsync(toEvent, quest);
    }


    /// <summary>
    /// Gets all quests for a specific event.
    /// </summary>
    public async Task<List<Quest>> GetQuestsAsync(Event fromEvent)
    {
        return await _questRepository.GetQuestsAsync(fromEvent);
    }

    /// <summary>
    /// Deletes a quest.
    /// </summary>
    public async Task DeleteQuestAsync(Quest quest)
    {
        await _questRepository.DeleteQuestAsync(quest);
    }





    public async Task<List<Quest>> GetPresetQuestsAsync()
    {
        List<Quest> presets = new List<Quest>()
        {
            new Quest
            {
                Id = 1,
                Name = "Most memorable memory",
                Description = "Submit the photo you think is most relevant for you for the event. Explain why.",
                Difficulty = 3
            },
            new Quest
            {
                Id = 2, Name = "Flower admirer", Description = "Submit a photo with flower(s)!", Difficulty = 1
            },
            new Quest
            {
                Id = 3,
                Name = "Group photo!",
                Description = "Submit a photo with you and your friends!",
                Difficulty = 2
            }
        };
        return presets;
    }
}