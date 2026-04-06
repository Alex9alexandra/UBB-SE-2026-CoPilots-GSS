using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.Interfaces;

public interface IQuestApprovalService
{
    Task SubmitProofAsync(Quest quest, Memory proof);
    /// <summary>
    /// Return the quest details and the completion status for the user who asks for them.
    /// </summary>
    /// <param name="currentEvent"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    Task<List<QuestMemory>> GetQuestsWithStatus(Event currentEvent, User user);


    
    /// <summary>
    /// Returns all submitted, not approved, nor denied, proofs for a given quest.
    /// </summary>
    /// <param name="quest"></param>
    /// <returns>List of quest proofs</returns>
    Task<List<QuestMemory>> GetProofsForQuestAsync(Quest quest);

    /// <summary>
    /// Updates the quest proof with the new status
    /// </summary>
    /// <param name="proof"></param>
    /// <returns></returns>
    Task ChangeProofStatusAsync(QuestMemory proof);

    Task DeleteSubmissionAsync(QuestMemory proof,User user);
}