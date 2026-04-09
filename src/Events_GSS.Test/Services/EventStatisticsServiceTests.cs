
using Events_GSS.Data.Repositories.eventStatisticsRepository;
using Events_GSS.Data.Services.eventStatisticsServices;
using Events_GSS.Data.Models;
using Moq;
using Xunit;

namespace Events_GSS.Test.Services;


public class EventStatisticsServiceTests
{
    [Fact]
    public async Task GetParticipantOverviewAsync_ValidEventId_CallsRepositoryAndReturnsResult()
    {
        //Arrange
        var mockRepository = new Mock<IEventStatisticsRepository>(MockBehavior.Strict);
        int eventId = 1;
        var expected = new ParticipantOverview();
        mockRepository.Setup(repo => repo.GetParticipantOverviewAsync(eventId))
            .ReturnsAsync(expected);
        var service = new EventStatisticsService(mockRepository.Object);

        //Act
        var result = await service.GetParticipantOverviewAsync(eventId);

        //Assert
        Assert.Equal(expected, result);
        mockRepository.Verify(repo => repo.GetParticipantOverviewAsync(eventId), Times.Once);
    }

    [Fact]
    public async Task GetEngagementBreakdownAsync_ValidEventId_CallsRepositoryAndReturnsResult()
    {
        //Arrange
        var mockRepository = new Mock<IEventStatisticsRepository>(MockBehavior.Strict);
        int eventId = 1;
        var expected = new EngagementBreakdown();
        mockRepository.Setup(repo=> repo.GetEngagementBreakdownAsync(eventId))
            .ReturnsAsync(expected);
        var service= new EventStatisticsService(mockRepository.Object);

        //Act
        var result = await service.GetEngagementBreakdownAsync(eventId);

        //Assert
        Assert.Equal(expected, result);
        mockRepository.Verify(repo => repo.GetEngagementBreakdownAsync(eventId), Times.Once);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ValidEventId_CallsRepositoryAndReturnsResult()
    {
        //Arrange
        var mockRepository = new Mock<IEventStatisticsRepository>(MockBehavior.Strict);
        int eventId = 1;
        var expected= new List<LeaderboardEntry>();
        mockRepository.Setup(repo => repo.GetLeaderboardAsync(eventId))
            .ReturnsAsync(expected);
        var service = new EventStatisticsService(mockRepository.Object);

        //Act
        var result = await service.GetLeaderboardAsync(eventId);

        //Assert
        Assert.Equal(expected, result);
        mockRepository.Verify(repo => repo.GetLeaderboardAsync(eventId), Times.Once);

    }

    [Fact]
    public async Task GetQuestAnalyticsAsync_ValidEventId_CallsRepositoryAndReturnsResult()
    {
        //Arrange
        var mockRepository = new Mock<IEventStatisticsRepository>(MockBehavior.Strict);
        int eventId = 1;
        var expected = new List<QuestAnalyticsEntry>();
        mockRepository.Setup(repo => repo.GetQuestAnalyticsAsync(eventId))
            .ReturnsAsync(expected);
        var service = new EventStatisticsService(mockRepository.Object);

        //Act
        var result = await service.GetQuestAnalyticsAsync(eventId);

        //Assert
        Assert.Equal(expected, result);
        mockRepository.Verify(repo => repo.GetQuestAnalyticsAsync(eventId), Times.Once);
    }

    [Fact]
    public async Task GetParticipantOverviewAsync_RepositoryThrows_ThrowsException()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        mockRepo.Setup(r => r.GetParticipantOverviewAsync(eventId))
            .ThrowsAsync(new Exception("repo fail"));

        var service = new EventStatisticsService(mockRepo.Object);

        await Assert.ThrowsAsync<Exception>(() => service.GetParticipantOverviewAsync(eventId));
    }

    [Fact]
    public async Task GetLeaderboardAsync_RepositoryReturnsNull_ReturnsNull()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        mockRepo.Setup(r => r.GetLeaderboardAsync(eventId))
            .ReturnsAsync((List<LeaderboardEntry>)null);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetLeaderboardAsync(eventId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetParticipantOverviewAsync_ZeroParticipants_SetsEngagementRateToZero()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        var overview = new ParticipantOverview { TotalParticipants = 0, ActiveParticipants = 0 };
        mockRepo.Setup(r => r.GetParticipantOverviewAsync(eventId)).ReturnsAsync(overview);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetParticipantOverviewAsync(eventId);

        Assert.Equal(0, result.EngagementRate);
    }

    [Fact]
    public async Task GetEngagementBreakdownAsync_ZeroSubmissions_SetsRatesToZero()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        var breakdown = new EngagementBreakdown { TotalQuestSubmissions = 0, ApprovedQuests = 0, DeniedQuests = 0 };
        mockRepo.Setup(r => r.GetEngagementBreakdownAsync(eventId)).ReturnsAsync(breakdown);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetEngagementBreakdownAsync(eventId);

        Assert.Equal(0, result.ApprovedQuestsRate);
        Assert.Equal(0, result.DeniedQuestsRate);
    }

    [Fact]
    public async Task GetEngagementBreakdownAsync_NonZeroSubmissions_CalculatesRatesCorrectly()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        var breakdown = new EngagementBreakdown { TotalQuestSubmissions = 10, ApprovedQuests = 7 };
        mockRepo.Setup(r => r.GetEngagementBreakdownAsync(eventId)).ReturnsAsync(breakdown);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetEngagementBreakdownAsync(eventId);

        Assert.Equal(70, result.ApprovedQuestsRate);
        Assert.Equal(30, result.DeniedQuestsRate);
    }

    [Fact]
    public async Task GetEngagementBreakdownAsync_RepositoryReturnsNull_ReturnsNull()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        mockRepo.Setup(r => r.GetEngagementBreakdownAsync(eventId)).ReturnsAsync((EngagementBreakdown)null);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetEngagementBreakdownAsync(eventId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuestAnalyticsAsync_RepositoryReturnsNull_ReturnsNull()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        mockRepo.Setup(r => r.GetQuestAnalyticsAsync(eventId)).ReturnsAsync((List<QuestAnalyticsEntry>)null);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetQuestAnalyticsAsync(eventId);
        Assert.Null(result);
    }
    [Fact]
    public async Task GetParticipantOverviewAsync_NonZeroParticipants_CalculatesEngagementRate()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        var overview = new ParticipantOverview { TotalParticipants = 10, ActiveParticipants = 3 };
        mockRepo.Setup(r => r.GetParticipantOverviewAsync(eventId)).ReturnsAsync(overview);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetParticipantOverviewAsync(eventId);

        Assert.Equal(30, result.EngagementRate); // 3 / 10 * 100
    }

    [Fact]
    public async Task GetEngagementBreakdownAsync_AllApprovedCalculatesCorrectly()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        var breakdown = new EngagementBreakdown { TotalQuestSubmissions = 5, ApprovedQuests = 5 };
        mockRepo.Setup(r => r.GetEngagementBreakdownAsync(eventId)).ReturnsAsync(breakdown);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetEngagementBreakdownAsync(eventId);

        Assert.Equal(100, result.ApprovedQuestsRate);
        Assert.Equal(0, result.DeniedQuestsRate);
    }

    [Fact]
    public async Task GetEngagementBreakdownAsync_AllDeniedCalculatesCorrectly()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        var breakdown = new EngagementBreakdown { TotalQuestSubmissions = 5, ApprovedQuests = 0 };
        mockRepo.Setup(r => r.GetEngagementBreakdownAsync(eventId)).ReturnsAsync(breakdown);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetEngagementBreakdownAsync(eventId);

        Assert.Equal(0, result.ApprovedQuestsRate);
        Assert.Equal(100, result.DeniedQuestsRate);
    }

    [Fact]
    public async Task GetQuestAnalyticsAsync_EmptyList_ReturnsEmpty()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        var expected = new List<QuestAnalyticsEntry>();
        mockRepo.Setup(r => r.GetQuestAnalyticsAsync(eventId)).ReturnsAsync(expected);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetQuestAnalyticsAsync(eventId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLeaderboardAsync_EmptyList_ReturnsEmpty()
    {
        var mockRepo = new Mock<IEventStatisticsRepository>();
        int eventId = 1;
        var expected = new List<LeaderboardEntry>();
        mockRepo.Setup(r => r.GetLeaderboardAsync(eventId)).ReturnsAsync(expected);

        var service = new EventStatisticsService(mockRepo.Object);

        var result = await service.GetLeaderboardAsync(eventId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }


}
