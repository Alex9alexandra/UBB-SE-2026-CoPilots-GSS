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
    public async Task ChangeProofStatusAsync_StatusApproved_SendsNotificationAndUpdatesRepo()
    {
        var proof = CreateSampleQuestMemory(QuestMemoryStatus.Approved);

        await _service.ChangeProofStatusAsync(proof);

        await _mockRepo.Received(1).ChangeProofStatusAsync(proof);

        await _mockNotificationService.Received(1).NotifyAsync(
            123,
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("approved")));
    }

    [Fact]
    public async Task DeleteSubmissionAsync_RepositoryThrowsException_ThrowsWrappedException()
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
    public async Task GetQuestsWithStatus_QuestHasNoSubmission_ReturnsIncompleteStatus()
    {
        var currentEvent = new Event { EventId = 1 };
        var user = new User { UserId = 123 };

        _mockQuestService.GetQuestsAsync(currentEvent)
            .Returns(new List<Quest> { new Quest { Id = 50, Name = "Test" } });

        _mockRepo.GetRawSubmissionsForUser(user)
            .Returns(new List<QuestMemory>());

        var result = await _service.GetQuestsWithStatus(currentEvent, user);

        Assert.Single(result);
        Assert.Equal(QuestMemoryStatus.Incomplete, result[0].ProofStatus);
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
}