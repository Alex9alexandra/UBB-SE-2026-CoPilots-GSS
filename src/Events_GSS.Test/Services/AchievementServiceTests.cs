using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.achievementRepository;
using Events_GSS.Data.Services.achievementServices;

using Moq;

namespace Events_GSS.Test.Services;

public class AchievementServiceTests
{
    private readonly Mock<IAchievementRepository> _mockRepo;

    public AchievementServiceTests()
    {
        _mockRepo = new Mock<IAchievementRepository>(MockBehavior.Strict);
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ReturnsList()
    {
        int userId = 1;
        var expected = new List<Achievement>
            {
                new Achievement { AchievementId = 1, Name = "First Steps" }
            };

        _mockRepo.Setup(r => r.GetAllAchievementsAsync())
                 .ReturnsAsync(expected);

        var service = new AchievementService(_mockRepo.Object);
        var result = await service.GetUserAchievementsAsync(userId);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task CheckAndAwardAchievementsAsync_UnlocksAchievements()
    {
        int userId = 1;

        _mockRepo.Setup(r => r.GetAttendedEventsCountAsync(userId)).ReturnsAsync(10);
        _mockRepo.Setup(r => r.GetCreatedEventsCountAsync(userId)).ReturnsAsync(3);
        _mockRepo.Setup(r => r.GetApprovedQuestsCountAsync(userId)).ReturnsAsync(50);
        _mockRepo.Setup(r => r.GetMemoriesWithPhotosCountAsync(userId)).ReturnsAsync(60);
        _mockRepo.Setup(r => r.GetMessagesCountAsync(userId)).ReturnsAsync(120);
        _mockRepo.Setup(r => r.HasPerfectEventAsync(userId)).ReturnsAsync(true);

        var achievements = new List<Achievement>
            {
                new Achievement { AchievementId = 1, Name = "First Steps" },
                new Achievement { AchievementId = 2, Name = "Proper Host" }
            };

        _mockRepo.Setup(r => r.GetAllAchievementsAsync()).ReturnsAsync(achievements);
        _mockRepo.Setup(r => r.IsAlreadyUnlockedAsync(userId, It.IsAny<int>())).ReturnsAsync(false);
        _mockRepo.Setup(r => r.UnlockAchievementAsync(userId, It.IsAny<int>())).Returns(Task.CompletedTask);

        var service = new AchievementService(_mockRepo.Object);

        await service.CheckAndAwardAchievementsAsync(userId);

        _mockRepo.Verify(r => r.UnlockAchievementAsync(userId, 1), Times.Once);
        _mockRepo.Verify(r => r.UnlockAchievementAsync(userId, 2), Times.Once);
    }

    [Fact]
    public async Task CheckAndAwardAchievementsAsync_SkipsAlreadyUnlocked()
    {
        int userId = 1;

        var achievements = new List<Achievement>
            {
                new Achievement { AchievementId = 1, Name = "First Steps" }
            };

        _mockRepo.Setup(r => r.GetAttendedEventsCountAsync(userId)).ReturnsAsync(1);
        _mockRepo.Setup(r => r.GetCreatedEventsCountAsync(userId)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.GetApprovedQuestsCountAsync(userId)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.GetMemoriesWithPhotosCountAsync(userId)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.GetMessagesCountAsync(userId)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.HasPerfectEventAsync(userId)).ReturnsAsync(false);

        _mockRepo.Setup(r => r.GetAllAchievementsAsync()).ReturnsAsync(achievements);
        _mockRepo.Setup(r => r.IsAlreadyUnlockedAsync(userId, 1)).ReturnsAsync(true);

        var service = new AchievementService(_mockRepo.Object);

        await service.CheckAndAwardAchievementsAsync(userId);

        _mockRepo.Verify(r => r.UnlockAchievementAsync(userId, It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndAwardAchievementsAsync_EmptyAchievementList_DoesNothing()
    {
        int userId = 1;

        _mockRepo.Setup(r => r.GetAttendedEventsCountAsync(userId)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.GetCreatedEventsCountAsync(userId)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.GetApprovedQuestsCountAsync(userId)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.GetMemoriesWithPhotosCountAsync(userId)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.GetMessagesCountAsync(userId)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.HasPerfectEventAsync(userId)).ReturnsAsync(false);

        _mockRepo.Setup(r => r.GetAllAchievementsAsync()).ReturnsAsync(new List<Achievement>());

        var service = new AchievementService(_mockRepo.Object);

        await service.CheckAndAwardAchievementsAsync(userId);

        _mockRepo.Verify(r => r.UnlockAchievementAsync(userId, It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData("Distinguished Gentleperson", 10, true)]
    [InlineData("Quest Solver", 25, true)]
    [InlineData("Quest Master", 75, true)]
    [InlineData("Quest Champion", 150, true)]
    [InlineData("Memory Keeper", 50, true)]
    [InlineData("Social Butterfly", 100, true)]
    [InlineData("Event Veteran", 10, true)]
    [InlineData("Perfectionist", 0, true)]
    [InlineData("Unknown Achievement", 0, false)]
    public void IsConditionMet_AllTitles_ReturnsCorrectResult(string title, int value, bool expected)
    {
        var service = new AchievementService(_mockRepo.Object);

        bool result;
        if (title == "Distinguished Gentleperson")
            result = service.GetType()
                            .GetMethod("IsConditionMet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .Invoke(service, new object[] { title, 0, value, 0, 0, 0, false }) as bool? ?? false;
        else if (title == "Quest Solver" || title == "Quest Master" || title == "Quest Champion")
            result = (bool)service.GetType()
                            .GetMethod("IsConditionMet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .Invoke(service, new object[] { title, 0, 0, value, 0, 0, false });
        else if (title == "Memory Keeper")
            result = (bool)service.GetType()
                            .GetMethod("IsConditionMet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .Invoke(service, new object[] { title, 0, 0, 0, value, 0, false });
        else if (title == "Social Butterfly")
            result = (bool)service.GetType()
                            .GetMethod("IsConditionMet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .Invoke(service, new object[] { title, 0, 0, 0, 0, value, false });
        else if (title == "Event Veteran")
            result = (bool)service.GetType()
                            .GetMethod("IsConditionMet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .Invoke(service, new object[] { title, value, 0, 0, 0, 0, false });
        else if (title == "Perfectionist")
            result = (bool)service.GetType()
                            .GetMethod("IsConditionMet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .Invoke(service, new object[] { title, 0, 0, 0, 0, 0, true });
        else
            result = (bool)service.GetType()
                            .GetMethod("IsConditionMet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .Invoke(service, new object[] { title, 0, 0, 0, 0, 0, false });

        Assert.Equal(expected, result);
    }
}
