using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.Services.notificationServices;

using NSubstitute;

using Xunit;

namespace Events_GSS.Test;

public class QuestApprovalServiceTests
{
    private readonly IQuestMemoryRepository _mockRepo;
    private readonly IQuestService _mockQuestService;
    private readonly IMemoryService _mockMemoryService;
    private readonly INotificationService _mockNotificationService;
    private readonly QuestApprovalService _service;

    public QuestApprovalServiceTests()
    {
        _mockRepo = Substitute.For<IQuestMemoryRepository>();
        _mockQuestService = Substitute.For<IQuestService>();
        _mockMemoryService = Substitute.For<IMemoryService>();
        _mockNotificationService = Substitute.For<INotificationService>();

        _service = new QuestApprovalService(
            _mockRepo,
            _mockQuestService,
            _mockMemoryService,
            _mockNotificationService);
    }

    [Fact]
    public async Task ChangeProofStatusAsync_StatusApproved_UpdatesRepository()
    {
        var proof = CreateSampleQuestMemory(QuestMemoryStatus.Approved);

        await _service.ChangeProofStatusAsync(proof);

        await _mockRepo.Received(1).ChangeProofStatusAsync(proof);
    }

    [Fact]
    public async Task ChangeProofStatusAsync_StatusApproved_SendsCorrectNotification()
    {
        var proof = CreateSampleQuestMemory(QuestMemoryStatus.Approved);

        await _service.ChangeProofStatusAsync(proof);

        await _mockNotificationService.Received(1).NotifyAsync(
            123,
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("approved")));
    }

    [Fact]
    public async Task DeleteSubmissionAsync_RepositoryThrowsException_ThrowsExceptionType()
    {
        var proof = CreateSampleQuestMemory(QuestMemoryStatus.Submitted);
        var user = new User { UserId = 123 };

        _mockRepo.When(x => x.DeleteProofAsync(proof))
                 .Do(x => throw new Exception("DB Error"));

        await Assert.ThrowsAsync<Exception>(() =>
            _service.DeleteSubmissionAsync(proof, user));
    }

    [Fact]
    public async Task DeleteSubmissionAsync_RepositoryThrowsException_ContainsWrappedMessage()
    {
        var proof = CreateSampleQuestMemory(QuestMemoryStatus.Submitted);
        var user = new User { UserId = 123 };

        _mockRepo.When(x => x.DeleteProofAsync(proof))
                 .Do(x => throw new Exception("DB Error"));

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _service.DeleteSubmissionAsync(proof, user));

        Assert.Contains("An error occurred while deleting the submission", exception.Message);
    }

    [Fact]
    public async Task GetQuestsWithStatus_QuestHasNoSubmission_ReturnsCorrectCount()
    {
        var currentEvent = new Event { EventId = 1 };
        var user = new User { UserId = 123 };

        _mockQuestService.GetQuestsAsync(currentEvent)
            .Returns(new List<Quest> { new Quest { Id = 50, Name = "Test" } });

        _mockRepo.GetRawSubmissionsForUser(user)
            .Returns(new List<QuestMemory>());

        var result = await _service.GetQuestsWithStatus(currentEvent, user);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetQuestsWithStatus_QuestHasNoSubmission_SetsStatusToIncomplete()
    {
        var currentEvent = new Event { EventId = 1 };
        var user = new User { UserId = 123 };

        _mockQuestService.GetQuestsAsync(currentEvent)
            .Returns(new List<Quest> { new Quest { Id = 50, Name = "Test" } });

        _mockRepo.GetRawSubmissionsForUser(user)
            .Returns(new List<QuestMemory>());

        var result = await _service.GetQuestsWithStatus(currentEvent, user);

        Assert.Equal(QuestMemoryStatus.Incomplete, result[0].ProofStatus);
    }

    [Fact]
    public async Task GetQuestsWithStatus_QuestHasNoSubmission_MapsCorrectQuestId()
    {
        var currentEvent = new Event { EventId = 1 };
        var user = new User { UserId = 123 };

        _mockQuestService.GetQuestsAsync(currentEvent)
            .Returns(new List<Quest> { new Quest { Id = 50, Name = "Test" } });

        _mockRepo.GetRawSubmissionsForUser(user)
            .Returns(new List<QuestMemory>());

        var result = await _service.GetQuestsWithStatus(currentEvent, user);

        Assert.Equal(50, result[0].ForQuest.Id);
    }

    private QuestMemory CreateSampleQuestMemory(QuestMemoryStatus status)
    {
        return new QuestMemory
        {
            ProofStatus = status,
            ForQuest = new Quest { Name = "Test Quest" },
            Proof = new Memory
            {
                Author = new User { UserId = 123 },
                Event = new Event { EventId = 1 }
            }
        };
    }

    [Fact]
    public async Task DeleteSubmissionAsync_Fails_ShouldHandleException()
    {
        var proof = new QuestMemory
        {
            ForQuest = new Quest { Name = "Test Quest" },
            Proof = new Memory { Author = new User() }
        };
        _mockRepo.When(x => x.DeleteProofAsync(Arg.Any<QuestMemory>()))
                 .Do(x => throw new Exception("Fake Error"));

        await Assert.ThrowsAsync<Exception>(() => _service.DeleteSubmissionAsync(proof, new User()));
    }

    [Fact]
    public async Task GetQuestsWithStatus_QuestHasSubmission_Coverage_Lambda()
    {
        var currentEvent = new Event { EventId = 1 };
        var user = new User { UserId = 123 };
        var quest = new Quest { Id = 50, Name = "Test" };

        _mockQuestService.GetQuestsAsync(currentEvent)
            .Returns(new List<Quest> { quest });
        var existingSubmission = new QuestMemory
        {
            ForQuest = new Quest { Id = 50 },
            ProofStatus = QuestMemoryStatus.Submitted,
            Proof = new Memory { Author = user, Event = currentEvent } 
        };

        _mockRepo.GetRawSubmissionsForUser(user)
            .Returns(new List<QuestMemory> { existingSubmission });

        var result = await _service.GetQuestsWithStatus(currentEvent, user);

        Assert.Single(result);
        Assert.Equal(50, result[0].ForQuest.Id);
    }

    [Fact]
    public async Task ChangeProofStatusAsync_StatusRejected_Coverage()
    {
        var proof = CreateSampleQuestMemory(QuestMemoryStatus.Rejected);

        await _service.ChangeProofStatusAsync(proof);

        await _mockNotificationService.Received(1).NotifyAsync(
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("rejected")));
    }

    [Fact]
    public async Task SubmitProofAsync_Coverage()
    {
        var quest = new Quest { Id = 1 };
        var proof = new Memory
        {
            Author = new User { UserId = 1 },
            Event = new Event { EventId = 1 }
        };
        _mockRepo.AddMemoryAsync(proof).Returns(Task.FromResult(99));

        await _service.SubmitProofAsync(quest, proof);

        Assert.Equal(99, proof.MemoryId);
        await _mockRepo.Received(1).SubmitProofAsync(quest, proof);
    }

    [Fact]
    public async Task GetProofsForQuestAsync_Coverage()
    {
        await _service.GetProofsForQuestAsync(new Quest());
        
        await _mockRepo.Received(1).GetProofsForQuestAsync(Arg.Any<Quest>());
    }

    [Fact]
    public async Task DeleteSubmissionAsync_Success_Coverage()
    {
        var user = new User { UserId = 1 };
        var proof = new QuestMemory
        {
            ForQuest = new Quest { Id = 1, Name = "Quest" },
            Proof = new Memory { Author = user, Event = new Event() },
            ProofStatus = QuestMemoryStatus.Submitted
        };

        _mockRepo.DeleteProofAsync(proof).Returns(Task.CompletedTask);
        _mockMemoryService.DeleteAsync(proof.Proof, user).Returns(Task.CompletedTask);

        await _service.DeleteSubmissionAsync(proof, user);

        await _mockRepo.Received(1).DeleteProofAsync(proof);
        await _mockMemoryService.Received(1).DeleteAsync(proof.Proof, user);
    }
}