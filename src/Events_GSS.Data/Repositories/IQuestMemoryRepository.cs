using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories;

public interface IQuestMemoryRepository
{
    //FOR USER
    //keep in sync with IMemoryRepository.AddAsync
    public Task<int> AddMemoryAsync(Memory proofMemory);    
    public Task SubmitProofAsync(Quest quest, Memory proof);
    public Task<List<QuestMemory>> GetSubmissionsStatusForUser(List<Quest> quests, User user);


    //FOR ADMIN
    public Task<List<QuestMemory>> GetProofsForQuestAsync(Quest quest);
    public Task ChangeProofStatusAsync(QuestMemory proof);


    //FOR BOTH
    public Task DeleteProofAsync(QuestMemory proof);
}