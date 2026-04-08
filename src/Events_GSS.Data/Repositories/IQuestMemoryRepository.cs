using Events_GSS.Data.Models;

public interface IQuestMemoryRepository
{
    Task<int> AddMemoryAsync(Memory proofMemory);
    Task SubmitProofAsync(Quest quest, Memory proof);
    Task<List<QuestMemory>> GetRawSubmissionsForUser(User user); 
    Task<List<QuestMemory>> GetProofsForQuestAsync(Quest quest);
    Task ChangeProofStatusAsync(QuestMemory proof);
    Task DeleteProofAsync(QuestMemory proof);
}