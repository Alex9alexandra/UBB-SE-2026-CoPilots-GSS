

using System.Reflection;

using CommunityToolkit.Mvvm.Messaging;

using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Repositories.reputationRepository;
using Events_GSS.Data.Services.achievementServices;
using Events_GSS.Data.Services.reputationService;
using Events_GSS.Services.Interfaces;

using Moq;

using Xunit;

namespace Events_GSS.Test.Services;

public class ReputationServiceTests
{
    [Fact]
    public async Task GetReputationPointsAsync_ValidUserId_ReturnsPoints()
    {
        var mockRepo = new Mock<IReputationRepository>(MockBehavior.Strict);
        int userId = 1;
        int expectedPoints = 123;

        mockRepo.Setup(r => r.GetReputationPointsAsync(userId))
            .ReturnsAsync(expectedPoints);

        var service = new ReputationService(
            mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>()
        );

        var result = await service.GetReputationPointsAsync(userId);

        Assert.Equal(expectedPoints, result);
        mockRepo.Verify(r => r.GetReputationPointsAsync(userId), Times.Once);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-600, false)]
    public async Task CanPostMessagesAsync_CheckThreshold(int points, bool expected)
    {
        int userId = 1;
        var mockRepo = new Mock<IReputationRepository>(MockBehavior.Strict);
        mockRepo.Setup(r => r.GetReputationPointsAsync(userId))
            .ReturnsAsync(points);

        var service = new ReputationService(
            mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>()
        );

        var result = await service.CanPostMessagesAsync(userId);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleEventAttendedAsync_AttendeeCount10_UpdatesAdminReputation()
    {
        int eventId = 1;
        int adminId = 42;

        var mockAttendedRepo = new Mock<IAttendedEventRepository>(MockBehavior.Strict);
        var mockEventRepo = new Mock<IEventRepository>(MockBehavior.Strict);
        var mockRepo = new Mock<IReputationRepository>(MockBehavior.Strict);
        var mockAchievementService = new Mock<IAchievementService>(MockBehavior.Strict);

        mockAttendedRepo.Setup(r => r.GetAttendeeCountAsync(eventId)).ReturnsAsync(10);
        mockEventRepo.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(new Event { Admin = new User { UserId = adminId } });
        mockRepo.Setup(r => r.GetReputationPointsAsync(adminId)).ReturnsAsync(0);
        mockRepo.Setup(r => r.SetReputationAsync(adminId, It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var service = new ReputationService(
            mockRepo.Object,
            mockAttendedRepo.Object,
            mockEventRepo.Object,
            mockAchievementService.Object
        );

        var method = service.GetType()
            .GetMethod("HandleEventAttendedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method.Invoke(service, new object[] { eventId });

        mockRepo.Verify(r => r.SetReputationAsync(adminId, 20, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetTierAsync_ReturnsTier()
    {
        var mockRepo = new Mock<IReputationRepository>(MockBehavior.Strict);
        mockRepo.Setup(r => r.GetTierAsync(1)).ReturnsAsync("Gold");

        var service = new ReputationService(
            mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>()
        );

        var result = await service.GetTierAsync(1);

        Assert.Equal("Gold", result);
        mockRepo.Verify(r => r.GetTierAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetAchievementsAsync_ReturnsAchievements()
    {
        var mockAchievementService = new Mock<IAchievementService>(MockBehavior.Strict);
        var expected = new List<Achievement>();
        mockAchievementService.Setup(a => a.GetUserAchievementsAsync(1)).ReturnsAsync(expected);

        var service = new ReputationService(
            Mock.Of<IReputationRepository>(),
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            mockAchievementService.Object
        );

        var result = await service.GetAchievementsAsync(1);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleReputationChangeAsync_EventCreated_UpdatesReputation()
    {
        WeakReferenceMessenger.Default.Reset();
        var mockRepo = new Mock<IReputationRepository>(MockBehavior.Strict);
        var mockAchievementService = new Mock<IAchievementService>(MockBehavior.Strict);

        mockRepo.Setup(r => r.GetReputationPointsAsync(1)).ReturnsAsync(0);
        mockRepo.Setup(r => r.SetReputationAsync(1, 5, It.IsAny<string>())).Returns(Task.CompletedTask);
        mockAchievementService.Setup(a => a.CheckAndAwardAchievementsAsync(1)).Returns(Task.CompletedTask);

        var service = new ReputationService(
            mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            mockAchievementService.Object
        );

        WeakReferenceMessenger.Default.Send(new ReputationMessage(1, ReputationAction.EventCreated, null));

        await Task.Delay(100);

        mockRepo.Verify(r => r.SetReputationAsync(1, 5, It.IsAny<string>()), Times.Once);
        mockAchievementService.Verify(a => a.CheckAndAwardAchievementsAsync(1), Times.Once);
    }

    [Fact]
    public async Task HandleEventAttendedAsync_AdminNullOrAttendeesNot10_DoesNothing()
    {
        int eventId = 1;

        var mockAttendedRepo = new Mock<IAttendedEventRepository>(MockBehavior.Strict);
        var mockEventRepo = new Mock<IEventRepository>(MockBehavior.Strict);
        var mockRepo = new Mock<IReputationRepository>(MockBehavior.Strict);
        var mockAchievementService = new Mock<IAchievementService>(MockBehavior.Strict);

        // Attendees != 10
        mockAttendedRepo.Setup(r => r.GetAttendeeCountAsync(eventId)).ReturnsAsync(5);

        var service = new ReputationService(mockRepo.Object, mockAttendedRepo.Object, mockEventRepo.Object, mockAchievementService.Object);

        var method = service.GetType().GetMethod("HandleEventAttendedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(service, new object[] { eventId });

        mockRepo.Verify(r => r.SetReputationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CanPostMemoriesAsync_ReputationAboveThreshold_ReturnsTrue()
    {
        var userId = 1;
        var mockRepo = new Mock<IReputationRepository>(MockBehavior.Strict);
        mockRepo.Setup(r => r.GetReputationPointsAsync(userId)).ReturnsAsync(-299);

        var service = new ReputationService(
            mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());

        var canPost = await service.CanPostMemoriesAsync(userId);
        Assert.True(canPost);

    }

    [Fact]
    public async Task CanPostMemoriesAsync_ReputationBelowThreshold_ReturnsFalse()
    {
        var userId = 1;
        var mockRepo = new Mock<IReputationRepository>(MockBehavior.Strict);
        mockRepo.Setup(r => r.GetReputationPointsAsync(userId)).ReturnsAsync(-299);

        var service = new ReputationService(
            mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());


        mockRepo.Setup(r => r.GetReputationPointsAsync(userId)).ReturnsAsync(-301);
        var canPost = await service.CanPostMemoriesAsync(userId);
        Assert.False(canPost);
    }

    [Fact]
    public async Task CanCreateEventsAsync_ReputationAboveThreshold_ReturnsTrue()
    {
        var userId = 1;
        var mockRepo = new Mock<IReputationRepository>();
        mockRepo.Setup(r => r.GetReputationPointsAsync(userId)).ReturnsAsync(-699);

        var service = new ReputationService(mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());

        var canCreate = await service.CanCreateEventsAsync(userId);
        Assert.True(canCreate);
    }

    [Fact]
    public async Task CanCreateEventsAsync_ReputationBelowThreshold_ReturnsFalse()
    {
        var userId = 1;
        var mockRepo = new Mock<IReputationRepository>();
        mockRepo.Setup(r => r.GetReputationPointsAsync(userId)).ReturnsAsync(-699);

        var service = new ReputationService(mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());


        mockRepo.Setup(r => r.GetReputationPointsAsync(userId)).ReturnsAsync(-701);
        var canCreate = await service.CanCreateEventsAsync(userId);
        Assert.False(canCreate);
    }

    [Fact]
    public async Task CanAttendEventsAsync_ReputationAboveThreshold_ReturnsTrue()
    {
        var userId = 1;
        var mockRepo = new Mock<IReputationRepository>();
        mockRepo.Setup(r => r.GetReputationPointsAsync(userId)).ReturnsAsync(-999);

        var service = new ReputationService(mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());

        var canAttend = await service.CanAttendEventsAsync(userId);
        Assert.True(canAttend);
    }

    [Fact]
    public async Task CanAttendEventsAsync_ReputationBelowThreshold_ReturnsFalse()
    {
        var userId = 1;
        var mockRepo = new Mock<IReputationRepository>();
        mockRepo.Setup(r => r.GetReputationPointsAsync(userId)).ReturnsAsync(-999);

        var service = new ReputationService(mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());


        mockRepo.Setup(r => r.GetReputationPointsAsync(userId)).ReturnsAsync(-1001);
        var canAttend = await service.CanAttendEventsAsync(userId);
        Assert.False(canAttend);
    }

    [Fact]
    public async Task CalculateTier_Score0_ReturnsNewcomer()
    {
        var service = new ReputationService(
            Mock.Of<IReputationRepository>(),
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());

        var method = service.GetType().GetMethod("CalculateTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.Equal("Newcomer", method.Invoke(service, new object[] { 0 }));
    }

    [Fact]
    public async Task CalculateTier_Score50_ReturnsContributor()
    {
        var service = new ReputationService(
            Mock.Of<IReputationRepository>(),
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());

        var method = service.GetType().GetMethod("CalculateTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.Equal("Contributor", method.Invoke(service, new object[] { 50 }));
    }

    [Fact]
    public async Task CalculateTier_Score200_ReturnsOrganizer()
    {
        var service = new ReputationService(
            Mock.Of<IReputationRepository>(),
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());

        var method = service.GetType().GetMethod("CalculateTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.Equal("Organizer", method.Invoke(service, new object[] { 200 }));
    }

    [Fact]
    public async Task CalculateTier_Score500_ReturnsCommunityLeader()
    {
        var service = new ReputationService(
            Mock.Of<IReputationRepository>(),
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());

        var method = service.GetType().GetMethod("CalculateTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.Equal("Community Leader", method.Invoke(service, new object[] { 500 }));
    }

    [Fact]
    public async Task CalculateTier_Score1000_ReturnsEventMaster()
    {
        var service = new ReputationService(
            Mock.Of<IReputationRepository>(),
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            Mock.Of<IAchievementService>());

        var method = service.GetType().GetMethod("CalculateTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.Equal("Event Master", method.Invoke(service, new object[] { 1000 }));
    }

    [Fact]
    public async Task HandleReputationChangeAsync_AllActions_CallsSetReputationAndAchievements()
    {
        WeakReferenceMessenger.Default.Reset();
        var mockRepo = new Mock<IReputationRepository>();
        var mockAchievementService = new Mock<IAchievementService>();

        mockRepo.Setup(r => r.GetReputationPointsAsync(1)).ReturnsAsync(0);
        mockRepo.Setup(r => r.SetReputationAsync(1, It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        mockAchievementService.Setup(a => a.CheckAndAwardAchievementsAsync(1)).Returns(Task.CompletedTask);

        var service = new ReputationService(mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            mockAchievementService.Object);

        foreach (ReputationAction action in Enum.GetValues(typeof(ReputationAction)))
        {
            if (action == ReputationAction.EventAttended) continue;
            WeakReferenceMessenger.Default.Send(new ReputationMessage(1, action, null));
            await Task.Delay(50);
        }

        mockRepo.Verify(r => r.SetReputationAsync(1, It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(9));
        mockAchievementService.Verify(a => a.CheckAndAwardAchievementsAsync(1), Times.Exactly(9));
    }

    [Fact]
    public async Task HandleEventAttendedAsync_AdminNull_DoesNothing()
    {
        int eventId = 1;
        var mockAttendedRepo = new Mock<IAttendedEventRepository>();
        var mockEventRepo = new Mock<IEventRepository>();
        var mockRepo = new Mock<IReputationRepository>();
        var mockAchievementService = new Mock<IAchievementService>();

        mockAttendedRepo.Setup(r => r.GetAttendeeCountAsync(eventId)).ReturnsAsync(10);
        mockEventRepo.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(new Event { Admin = null });

        var service = new ReputationService(mockRepo.Object, mockAttendedRepo.Object, mockEventRepo.Object, mockAchievementService.Object);
        var method = service.GetType().GetMethod("HandleEventAttendedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method.Invoke(service, new object[] { eventId });

        mockRepo.Verify(r => r.SetReputationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleReputationChangeAsync_MinReputation_NotBelowMin()
    {
        WeakReferenceMessenger.Default.Reset();

        var mockRepo = new Mock<IReputationRepository>();
        var mockAchievementService = new Mock<IAchievementService>();

        // Current reputation is near min, delta would drop below
        mockRepo.Setup(r => r.GetReputationPointsAsync(1)).ReturnsAsync(-999);
        mockRepo.Setup(r => r.SetReputationAsync(1, It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        mockAchievementService.Setup(a => a.CheckAndAwardAchievementsAsync(1)).Returns(Task.CompletedTask);

        var service = new ReputationService(mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            mockAchievementService.Object);

        WeakReferenceMessenger.Default.Send(new ReputationMessage(1, ReputationAction.EventCancelled, null));
        await Task.Delay(50);

        // Should clamp to MinReputation
        mockRepo.Verify(r => r.SetReputationAsync(1, ReputationService.ReputationConstants.MinReputation, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleReputationChangeAsync_UnknownAction_DoesNothing()
    {
        WeakReferenceMessenger.Default.Reset();

        var mockRepo = new Mock<IReputationRepository>();
        var mockAchievementService = new Mock<IAchievementService>();

        var service = new ReputationService(mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            mockAchievementService.Object);

        var method = typeof(ReputationService)
        .GetMethod("HandleReputationChangeAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Send a fake action not in the map
        var unknownAction = (ReputationAction)999;
        var message = new ReputationMessage(1, unknownAction, null);

        await (Task)method.Invoke(service, new object[] { message });

        mockRepo.Verify(r => r.SetReputationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        mockAchievementService.Verify(a => a.CheckAndAwardAchievementsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAttendedAsync_AttendeeCountNot10_DoesNotUpdate()
    {
        int eventId = 1;
        var mockAttendedRepo = new Mock<IAttendedEventRepository>();
        var mockEventRepo = new Mock<IEventRepository>();
        var mockRepo = new Mock<IReputationRepository>();
        var mockAchievementService = new Mock<IAchievementService>();

        mockAttendedRepo.Setup(r => r.GetAttendeeCountAsync(eventId)).ReturnsAsync(9);

        var service = new ReputationService(mockRepo.Object, mockAttendedRepo.Object, mockEventRepo.Object, mockAchievementService.Object);
        var method = service.GetType().GetMethod("HandleEventAttendedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method.Invoke(service, new object[] { eventId });

        mockRepo.Verify(r => r.SetReputationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleReputationChangeAsync_ExceptionHandled()
    {
        WeakReferenceMessenger.Default.Reset();
        var mockRepo = new Mock<IReputationRepository>();
        var mockAchievement = new Mock<IAchievementService>();

        mockRepo.Setup(r => r.GetReputationPointsAsync(1)).ThrowsAsync(new Exception("fail"));

        var service = new ReputationService(mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            mockAchievement.Object);

        WeakReferenceMessenger.Default.Send(new ReputationMessage(1, ReputationAction.EventCreated, null));
        await Task.Delay(50);

    }

    [Fact]
    public async Task HandleEventAttendedAsync_EventNull_DoesNothing()
    {
        int eventId = 1;
        var mockAttendedRepo = new Mock<IAttendedEventRepository>();
        var mockEventRepo = new Mock<IEventRepository>();
        var mockRepo = new Mock<IReputationRepository>();
        var mockAchievementService = new Mock<IAchievementService>();

        mockAttendedRepo.Setup(r => r.GetAttendeeCountAsync(eventId)).ReturnsAsync(10);
        mockEventRepo.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Event?)null);

        var service = new ReputationService(mockRepo.Object, mockAttendedRepo.Object, mockEventRepo.Object, mockAchievementService.Object);
        var method = service.GetType().GetMethod("HandleEventAttendedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method.Invoke(service, new object[] { eventId });

        mockRepo.Verify(r => r.SetReputationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleReputationChangeAsync_EventAttended_NullEventId_DoesNothing()
    {
        WeakReferenceMessenger.Default.Reset();

        var mockRepo = new Mock<IReputationRepository>();
        var mockAchievementService = new Mock<IAchievementService>();

        mockRepo.Setup(r => r.SetReputationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

        var service = new ReputationService(
            mockRepo.Object,
            Mock.Of<IAttendedEventRepository>(),
            Mock.Of<IEventRepository>(),
            mockAchievementService.Object);

        var message = new ReputationMessage(1, ReputationAction.EventAttended, null);
        WeakReferenceMessenger.Default.Send(message);

        await Task.Delay(100); 

        mockRepo.Verify(r => r.SetReputationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleReputationChangeAsync_EventAttended_ValidEventId_UpdatesAdminReputationAndAchievements()
    {
        // Reset the messenger to avoid interference
        WeakReferenceMessenger.Default.Reset();

        int userId = 1;
        int adminId = 42;
        int eventId = 100;

        // Mock repositories and services
        var mockRepo = new Mock<IReputationRepository>(MockBehavior.Strict);
        var mockAttendedRepo = new Mock<IAttendedEventRepository>(MockBehavior.Strict);
        var mockEventRepo = new Mock<IEventRepository>(MockBehavior.Strict);
        var mockAchievementService = new Mock<IAchievementService>(MockBehavior.Strict);

        // Event has exactly 10 attendees
        mockAttendedRepo.Setup(r => r.GetAttendeeCountAsync(eventId)).ReturnsAsync(10);
        // Event has an admin
        mockEventRepo.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(new Event { Admin = new User { UserId = adminId } });
        // Admin current reputation
        mockRepo.Setup(r => r.GetReputationPointsAsync(adminId)).ReturnsAsync(0);
        // Expect admin reputation to be updated
        mockRepo.Setup(r => r.SetReputationAsync(adminId, ReputationService.ReputationDeltas.EventAttendedAdminBonus, It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        // Achievement check
        mockAchievementService.Setup(a => a.CheckAndAwardAchievementsAsync(userId)).Returns(Task.CompletedTask);

        var service = new ReputationService(
            mockRepo.Object,
            mockAttendedRepo.Object,
            mockEventRepo.Object,
            mockAchievementService.Object
        );

        // Send the ReputationMessage
        var message = new ReputationMessage(userId, ReputationAction.EventAttended, eventId);
        WeakReferenceMessenger.Default.Send(message);

        // Small delay to allow async handler to execute
        await Task.Delay(50);

        // Verify expected calls
        mockRepo.Verify(r => r.SetReputationAsync(adminId, ReputationService.ReputationDeltas.EventAttendedAdminBonus, It.IsAny<string>()), Times.Once);
        mockAchievementService.Verify(a => a.CheckAndAwardAchievementsAsync(userId), Times.Once);
    }
}
