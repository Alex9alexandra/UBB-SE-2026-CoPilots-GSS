using System;
using System.Collections.Generic;
using System.Security.Cryptography.Pkcs;
using System.Text;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS;

namespace Events_GSS.Data.Services;

public class QuestApprovalService: IQuestApprovalService
{

    private readonly IQuestMemoryRepository _approvalRepository;
    private readonly IQuestService _questService;
    private readonly IMemoryService _memoryService;

    public QuestApprovalService(IQuestMemoryRepository repository,IQuestService questService, IMemoryService memoryService)
    {
        _approvalRepository = repository;
        _questService = questService;
        _memoryService = memoryService;
    }

    public async Task SubmitProofAsync(Quest quest, Memory proof)
    {
        //chack prereq?
        int memoryId = await _approvalRepository.AddMemoryAsync(proof);
        proof.MemoryId = memoryId;
        await _approvalRepository.SubmitProofAsync(quest, proof);
        return;
    }

    public async Task<List<QuestMemory>> GetQuestsWithStatus(Event currentEvent, User user)
    {
        List<Quest> quests = await _questService.GetQuestsAsync(currentEvent);
        List<QuestMemory> submittedQuests = await _approvalRepository.GetSubmissionsStatusForUser(quests, user);
        
        
        
        List<QuestMemory> result = new List<QuestMemory>();
        result.AddRange(submittedQuests);
        result.AddRange(
            quests.Where(q =>
            {
                 return !submittedQuests.Exists(p=> p.ForQuest.Id == q.Id);
            })
            .Select(q=>new QuestMemory
            {
                ForQuest = q,
                Proof=null,
                ProofStatus = QuestMemoryStatus.Incomplete
            })
            );
        return result;
    }



    /// <summary>
    /// Returns all submitted, not approved, nor denied, proofs for a given quest.
    /// </summary>
    /// <param name="quest"></param>
    /// <returns>List of quest proofs</returns>
    public async Task<List<QuestMemory>> GetProofsForQuestAsync(Quest quest)
    {
        return await _approvalRepository.GetProofsForQuestAsync(quest);
    }

    /// <summary>
    /// Updates the quest proof with the new status
    /// </summary>
    /// <param name="proof"></param>
    /// <returns></returns>
    public async Task ChangeProofStatusAsync(QuestMemory proof)
    {
        await _approvalRepository.ChangeProofStatusAsync(proof);
    }

    public async Task DeleteSubmissionAsync(QuestMemory proof,User user)
    {
        try
        {
            //proof.Proof does not exist in current context.
            await _approvalRepository.DeleteProofAsync(proof);
            await _memoryService.DeleteAsync(proof.Proof, user);
            
        }
        catch (Exception exc)
        {
            throw new Exception("Deleting Submission exception: "+exc.ToString());
        }
    }
}
