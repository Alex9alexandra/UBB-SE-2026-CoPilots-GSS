using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services;

using NSubstitute;

using Xunit;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Events_GSS.Test;

public class QuestServiceTests
{
    private readonly IQuestRepository _mockRepo;
    private readonly QuestService _service;

    public QuestServiceTests()
    {
        _mockRepo = Substitute.For<IQuestRepository>();
        _service = new QuestService(_mockRepo);
    }

    [Fact]
    public async Task AddQuestAsync_Coverage()
    {
        var ev = new Event { EventId = 1 };
        var quest = new Quest { Id = 10, Name = "New Quest" };
        _mockRepo.AddQuestAsync(ev, quest).Returns(Task.FromResult(10));

        var result = await _service.AddQuestAsync(ev, quest);

        Assert.Equal(10, result);
        await _mockRepo.Received(1).AddQuestAsync(ev, quest);
    }

    [Fact]
    public async Task GetQuestsAsync_ShouldReturnList()
    {
        var ev = new Event { EventId = 1 };
        _mockRepo.GetQuestsAsync(ev).Returns(new List<Quest> { new Quest { Id = 1 } });

        var result = await _service.GetQuestsAsync(ev);

        Assert.NotNull(result);
        await _mockRepo.Received(1).GetQuestsAsync(ev);
    }

    [Fact]
    public async Task DeleteQuestAsync_Coverage()
    {
        var quest = new Quest { Id = 1 };

        await _service.DeleteQuestAsync(quest);

        await _mockRepo.Received(1).DeleteQuestAsync(quest);
    }

    [Fact]
    public async Task GetPresetQuestsAsync_Coverage()
    {
        var result = await _service.GetPresetQuestsAsync();

        Assert.Empty(result); 
    }
}