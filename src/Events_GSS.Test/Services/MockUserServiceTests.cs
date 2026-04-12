

using Events_GSS.Data.Models;
using Events_GSS.Services;
using Events_GSS.Services.Interfaces;

using Xunit;
using Moq;

namespace Events_GSS.Test.Services;

public class MockUserServiceTests
{
    [Fact]
    public void GetCurrentUser_ReturnsCorrectUser()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.NotNull(user);
        Assert.Equal(3, user.UserId);
        Assert.Equal("Carol Popa", user.Name);
    }

    [Fact]
    public void GetUserById_ExistingUser_ReturnsUser()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        // Act
        var user = service.GetUserById(2);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(2, user.UserId);
        Assert.Equal("Bob Ionescu", user.Name);
    }

    [Fact]
    public void GetUserById_NonExistingUser_ReturnsNull()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        // Act
        var user = service.GetUserById(999);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public void GetFriends_ExistingUser_ReturnsFriends()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        // Act
        var friends = service.GetFriends(1);

        // Assert
        Assert.NotNull(friends);
        Assert.Equal(3, friends.Count);
    }

    [Fact]
    public void GetFriends_UserWithNoFriends_ReturnsEmptyList()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        // Act
        var friends = service.GetFriends(999);

        // Assert
        Assert.NotNull(friends);
        Assert.Empty(friends);
    }

    [Fact]
    public void SearchFriends_ByName_ReturnsMatchingFriends()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        // Act
        var results = service.SearchFriends(1, "bob");

        // Assert
        Assert.Single(results);
        Assert.Equal("Bob Ionescu", results[0].Name);
    }

    [Fact]
    public void SearchFriends_EmptyName_ReturnsAllFriends()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        // Act
        var results = service.SearchFriends(1, "");

        // Assert
        Assert.Equal(3, results.Count); 
    }

    [Fact]
    public async Task IsAttending_UserAttending_ReturnsTrue()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        var currentEvent = new Event { EventId = 100 };
        mockAttendedService.Setup(s => s.GetAttendedEventsAsync(3))
            .ReturnsAsync(new List<AttendedEvent> { new AttendedEvent { Event = currentEvent } });

        // Act
        var result = await service.IsAttending(currentEvent);

        // Assert
        Assert.True(result);
        mockAttendedService.Verify(s => s.GetAttendedEventsAsync(3), Times.Once);
    }

    [Fact]
    public async Task IsAttending_UserNotAttending_ReturnsFalse()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        var currentEvent = new Event { EventId = 100 };
        mockAttendedService.Setup(s => s.GetAttendedEventsAsync(3))
            .ReturnsAsync(new List<AttendedEvent>());

        // Act
        var result = await service.IsAttending(currentEvent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_AdminUser_ReturnsTrue()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        var currentEvent = new Event { Admin = new User { UserId = 3 } };
        // Act
        var result = service.IsAdmin(currentEvent);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAdmin_NonAdminUser_ReturnsFalse()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        var currentEvent = new Event { Admin = new User { UserId = 1 } }; 
        // Act
        var result = service.IsAdmin(currentEvent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetUserById_NegativeId_ReturnsNull()
    {
        var service = new MockUserService(Mock.Of<IAttendedEventService>());
        var user = service.GetUserById(-1);
        Assert.Null(user);
    }

    [Fact]
    public void SearchFriends_NullName_ReturnsAllFriends()
    {
        var service = new MockUserService(Mock.Of<IAttendedEventService>());
        var results = service.SearchFriends(1, null);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task IsAttending_ServiceThrowsException_ReturnsFalse()
    {
        // Arrange
        var mockAttendedService = new Mock<IAttendedEventService>(MockBehavior.Strict);
        var service = new MockUserService(mockAttendedService.Object);

        var currentEvent = new Event { EventId = 100 };

        mockAttendedService
            .Setup(s => s.GetAttendedEventsAsync(3))
            .ThrowsAsync(new Exception("Database failure"));

        // Act
        var result = await service.IsAttending(currentEvent);

        // Assert
        Assert.False(result);
        mockAttendedService.Verify(s => s.GetAttendedEventsAsync(3), Times.Once);
    }
}
