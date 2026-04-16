

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.notificationRepository;
using Events_GSS.Data.Services.notificationServices;

using Xunit;
using Moq;

namespace Events_GSS.Test.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task NotifyAsync_ValidParameters_CallsRepositoryOnce()
    {
        // Arrange
        var mockRepository = new Mock<INotificationRepository>(MockBehavior.Strict);
        int userId = 1;
        string title = "Test Title";
        string description = "Test Description";

        mockRepository.Setup(repo => repo.AddAsync(
            userId,
            title,
            description,
            It.IsAny<DateTime>()
        )).Returns(Task.CompletedTask);

        var service = new NotificationService(mockRepository.Object);

        // Act
        await service.NotifyAsync(userId, title, description);

        // Assert
        mockRepository.Verify(repo => repo.AddAsync(
            userId,
            title,
            description,
            It.IsAny<DateTime>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetNotificationsAsync_ValidUserId_ReturnsNotifications()
    {
        // Arrange
        var mockRepository = new Mock<INotificationRepository>(MockBehavior.Strict);
        int userId = 1;
        var expectedNotifications = new List<Notification>
            {
                new Notification { Title = "Title1", Description = "Desc1" },
                new Notification { Title = "Title2", Description = "Desc2" }
            };

        mockRepository.Setup(repo => repo.GetByUserIdAsync(userId))
            .ReturnsAsync(expectedNotifications);

        var service = new NotificationService(mockRepository.Object);

        // Act
        var result = await service.GetNotificationsAsync(userId);

        // Assert
        Assert.Equal(expectedNotifications, result);
        mockRepository.Verify(repo => repo.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ValidNotificationId_CallsRepositoryOnce()
    {
        // Arrange
        var mockRepository = new Mock<INotificationRepository>(MockBehavior.Strict);
        int notificationId = 1;

        mockRepository.Setup(repo => repo.DeleteAsync(notificationId))
            .Returns(Task.CompletedTask);

        var service = new NotificationService(mockRepository.Object);

        // Act
        await service.DeleteAsync(notificationId);

        // Assert
        mockRepository.Verify(repo => repo.DeleteAsync(notificationId), Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_RepositoryThrows_DoesNotSwallow()
    {
        // Arrange
        var mockRepo = new Mock<INotificationRepository>();
        mockRepo.Setup(r => r.AddAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("fail"));

        var service = new NotificationService(mockRepo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.NotifyAsync(1, "Title", "Desc"));
    }

    [Fact]
    public async Task GetNotificationsAsync_RepositoryReturnsEmptyList_ResultIsNotNull()
    {
        // Arrange
        var mockRepo = new Mock<INotificationRepository>();
        mockRepo.Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync(new List<Notification>());

        var service = new NotificationService(mockRepo.Object);

        // Act
        var result = await service.GetNotificationsAsync(1);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetNotificationsAsync_RepositoryReturnsEmptyList_ResultIsEmpty()
    {
        // Arrange
        var mockRepo = new Mock<INotificationRepository>();
        mockRepo.Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync(new List<Notification>());

        var service = new NotificationService(mockRepo.Object);

        // Act
        var result = await service.GetNotificationsAsync(1);

        // Assert
        Assert.Empty(result);
    }
}
