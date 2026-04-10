using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.Interfaces;

namespace Events_GSS.Data.Services;

public class QuestService : IQuestService
{
    private readonly IQuestRepository _questRepository;

    public QuestService(IQuestRepository questRepository)
    {
        _questRepository = questRepository;
    }

    public async Task<int> AddQuestAsync(Event targetEvent, Quest quest)
    {
        return await _questRepository.AddQuestAsync(targetEvent, quest);
    }

    public async Task<List<Quest>> GetQuestsAsync(Event sourceEvent)
    {
        return await _questRepository.GetQuestsAsync(sourceEvent);
    }

    public async Task DeleteQuestAsync(Quest quest)
    {
        await _questRepository.DeleteQuestAsync(quest);
    }

    public async Task<List<Quest>> GetPresetQuestsAsync()
    {
        // Refactoring: Preset quests should be retrieved from the database 
        // or a configuration file, not hardcoded in the service logic.
        return await Task.FromResult(new List<Quest>());
    }
}