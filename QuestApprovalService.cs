using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;

using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.Services.notificationServices;

namespace Events_GSS.Data.Services;

public class QuestApprovalService : IQuestApprovalService
{
    private readonly IQuestMemoryRepository _approvalRepository;
    private readonly IQuestService _questService;
    private readonly IMemoryService _memoryService;
    private readonly INotificationService _notificationService;

    public QuestApprovalService(
        IQuestMemoryRepository repository,
        IQuestService questService,
        IMemoryService memoryService,
        INotificationService notificationService)
    {
        _approvalRepository = repository;
        _questService = questService;
        _memoryService = memoryService;
        _notificationService = notificationService;
    }

    public async Task SubmitProofAsync(Quest quest, Memory proof)
    {
        int memoryId = await _approvalRepository.AddMemoryAsync(proof);
        proof.MemoryId = memoryId;
        await _approvalRepository.SubmitProofAsync(quest, proof);

        WeakReferenceMessenger.Default.Send(
            new ReputationMessage(proof.Author.UserId, ReputationAction.QuestSubmitted, proof.Event.EventId));
    }

    public async Task<List<QuestMemory>> GetQuestsWithStatus(Event currentEvent, User user)
    {
        List<Quest> allQuests = await _questService.GetQuestsAsync(currentEvent);

        List<QuestMemory> rawSubmissions = await _approvalRepository.GetRawSubmissionsForUser(user);

        List<QuestMemory> finalStatusList = new List<QuestMemory>();

        foreach (var quest in allQuests)
        {
            var submission = rawSubmissions.FirstOrDefault(s => s.ForQuest.Id == quest.Id);

            if (submission != null)
            {
                submission.ForQuest = quest; 
                finalStatusList.Add(submission);
            }
            else
            {
                finalStatusList.Add(new QuestMemory
                {
                    ForQuest = quest,
                    Proof = null,
                    ProofStatus = QuestMemoryStatus.Incomplete
                });
            }
        }

        return finalStatusList;
    }

    public async Task<List<QuestMemory>> GetProofsForQuestAsync(Quest quest)
    {
        return await _approvalRepository.GetProofsForQuestAsync(quest);
    }

    public async Task ChangeProofStatusAsync(QuestMemory proof)
    {
        await _approvalRepository.ChangeProofStatusAsync(proof);

        ReputationAction action = ReputationAction.QuestSubmitted;

        if (proof.ProofStatus == QuestMemoryStatus.Approved)
        {
            action = ReputationAction.QuestApproved;
        }
        else if (proof.ProofStatus == QuestMemoryStatus.Rejected)
        {
            action = ReputationAction.QuestDenied;
        }

        WeakReferenceMessenger.Default.Send(
            new ReputationMessage(proof.Proof.Author.UserId, action, proof.Proof.Event.EventId));

        string statusMessage = proof.ProofStatus.ToString().ToLower();
        await _notificationService.NotifyAsync(
            proof.Proof.Author.UserId,
            "Quest results back!",
            $"Your submission for the quest '{proof.ForQuest.Name}' has been {statusMessage}."
            );
    }

    public async Task DeleteSubmissionAsync(QuestMemory proof, User user)
    {
        try
        {
            await _approvalRepository.DeleteProofAsync(proof);
            await _memoryService.DeleteAsync(proof.Proof, user);
        }
        catch (Exception exception)
        {
            throw new Exception("An error occurred while deleting the submission: " + exception.Message);
        }
    }
}