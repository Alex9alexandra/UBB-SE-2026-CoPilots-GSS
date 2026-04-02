using System;
using System.Collections.Generic;
using System.Security.Cryptography.Pkcs;
using System.Text;

using CommunityToolkit.Mvvm.Messaging;

using Events_GSS;
using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.Interfaces;

using static System.Collections.Specialized.BitVector32;

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

        WeakReferenceMessenger.Default.Send(
            new ReputationMessage(proof.Author.UserId, ReputationAction.QuestSubmitted, proof.Event.EventId));

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

        ReputationAction action = ReputationAction.QuestSubmitted;//default value, should not be used

        if (proof.ProofStatus == QuestMemoryStatus.Approved) action = ReputationAction.QuestApproved;
        else if (proof.ProofStatus == QuestMemoryStatus.Rejected) action = ReputationAction.QuestDenied;
        WeakReferenceMessenger.Default.Send(
            new ReputationMessage(proof.Proof.Author.UserId, action, proof.Proof.Event.EventId));
    }

    public async Task DeleteSubmissionAsync(QuestMemory proof,User user)
    {
        try
        {
            await _approvalRepository.DeleteProofAsync(proof);
            await _memoryService.DeleteAsync(proof.Proof, user);
            
        }
        catch (Exception exc)
        {
            throw new Exception("Deleting Submission exception: "+exc.ToString());
        }
    }
}
