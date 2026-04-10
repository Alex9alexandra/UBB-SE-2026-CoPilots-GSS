using Events_GSS.Data.Models;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.ViewModelCore;
using Events_GSS.Services.Interfaces;
using Events_GSS.ViewModels;

using NSubstitute;

namespace Events_GSS.Test;

public class QuestUserViewModelTests
{
    [Fact]
    public async Task LoadQuestsAsync_ServiceReturnsQuests_PopulatesObservableCollection()
    {
        var mockQuestService = Substitute.For<IQuestApprovalService>();
        var mockUserService = Substitute.For<IUserService>();
        var currentEvent = new Event { EventId = 1 };
        var currentUser = new User { UserId = 123 };

        mockUserService.GetCurrentUser().Returns(currentUser);
        mockUserService.IsAttending(currentEvent).Returns(Task.FromResult(true));

        var fakeQuests = new List<QuestMemory>
    {
        new QuestMemory
        {
            ForQuest = new Quest { Id = 1, Name = "Quest 1" },
            ProofStatus = QuestMemoryStatus.Incomplete
        }
    };

        mockQuestService.GetQuestsWithStatus(currentEvent, currentUser)
                        .Returns(Task.FromResult(fakeQuests));

        var viewModel = new QuestUserViewModel(currentEvent, mockQuestService, mockUserService);

        await viewModel.LoadQuestsAsync();

        Assert.Single(viewModel.Quests); 
        Assert.Equal("Quest 1", viewModel.Quests[0].Name); 
        Assert.False(viewModel.IsLoading);
        Assert.Contains("1 quest(s) loaded", viewModel.StatusText);
    }

    [Fact]
    public async Task GetQuestsAsync_ReturnsData_WithoutWinUIErrors()
    {
        var mockService = Substitute.For<IQuestApprovalService>();
        var core = new QuestUserCore(mockService);
        var testEvent = new Event { EventId = 1 };
        var testUser = new User { UserId = 123 };

        var fakeData = new List<QuestMemory>
    {
        new QuestMemory { ForQuest = new Quest { Id = 1 }, ProofStatus = QuestMemoryStatus.Incomplete }
    };

        mockService.GetQuestsWithStatus(testEvent, testUser).Returns(fakeData);

        var result = await core.GetQuestsAsync(testEvent, testUser);

        Assert.Single(result);
        Assert.Equal(QuestMemoryStatus.Incomplete, result[0].ProofStatus);
    }
}