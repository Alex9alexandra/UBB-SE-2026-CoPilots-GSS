using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;

using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.reputationService;
using Events_GSS.Services;

using Moq;

using Xunit;

namespace Events_GSS.Tests.Services
{
    public sealed class AttendedEventServiceTests : IDisposable
    {
        private const int ExampleUserId = 22;
        private const int ExampleFriendId = 33;
        private const int ExampleEventId = 11;

        private readonly Mock<IAttendedEventRepository> attendedEventRepositoryMock;
        private readonly Mock<IReputationService> reputationServiceMock;

        private readonly AttendedEventService attendedEventService;

        private readonly object reputationMessageRecipient;

        public AttendedEventServiceTests()
        {
            // Setup
            this.attendedEventRepositoryMock = new Mock<IAttendedEventRepository>(MockBehavior.Strict);
            this.reputationServiceMock = new Mock<IReputationService>(MockBehavior.Strict);

            this.attendedEventService = MakeAttendedEventService(
                this.attendedEventRepositoryMock,
                this.reputationServiceMock);

            this.reputationMessageRecipient = new object();
        }

        public void Dispose()
        {
            // TearDown
            WeakReferenceMessenger.Default.UnregisterAll(this.reputationMessageRecipient);
        }

        [Fact]
        public async Task GetAttendedEventsAsync_WhenCalled_ReturnsRepositoryResult()
        {
            // Arrange
            var expectedAttendedEvents = new List<AttendedEvent>
            {
                new AttendedEvent(),
                new AttendedEvent(),
            };

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(ExampleUserId))
                .ReturnsAsync(expectedAttendedEvents);

            // Act
            List<AttendedEvent> actualAttendedEvents = await this.attendedEventService.GetAttendedEventsAsync(ExampleUserId);

            // Assert
            Assert.Same(expectedAttendedEvents, actualAttendedEvents);

            this.attendedEventRepositoryMock.VerifyAll();
        }



        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetEventsByArchiveStatusAsync_WhenCalled_ReturnsNonNullResult(bool isArchived)
        {
            // Arrange
            var attendedEvents = new List<AttendedEvent>
            {
                new AttendedEvent { IsArchived = true },
                new AttendedEvent { IsArchived = false },
                new AttendedEvent { IsArchived = true },
                new AttendedEvent { IsArchived = false },
            };

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(ExampleUserId))
                .ReturnsAsync(attendedEvents);

            // Act
            List<AttendedEvent> result = await this.attendedEventService.GetEventsByArchiveStatusAsync(ExampleUserId, isArchived);

            // Assert
            Assert.NotNull(result);

            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetEventsByArchiveStatusAsync_WhenCalled_FiltersByArchiveStatus(bool isArchived)
        {
            // Arrange
            var attendedEvents = new List<AttendedEvent>
            {
                new AttendedEvent { IsArchived = true },
                new AttendedEvent { IsArchived = false },
                new AttendedEvent { IsArchived = true },
                new AttendedEvent { IsArchived = false },
            };

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(ExampleUserId))
                .ReturnsAsync(attendedEvents);

            // Act
            List<AttendedEvent> result = await this.attendedEventService.GetEventsByArchiveStatusAsync(ExampleUserId, isArchived);

            Assert.All(result, attendedEvent => Assert.Equal(isArchived, attendedEvent.IsArchived));

            this.attendedEventRepositoryMock.VerifyAll();
        }

        
        [Fact]
        public async Task AttendEventAsync_ReputationTooLow_ThrowsInvalidOperationException()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(false);

            // Act
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId));

            // Assert
            Assert.Equal("Your reputation is too low to attend events (at -1000 RP).", exception.Message);

            this.attendedEventRepositoryMock.Verify(
                repository => repository.GetAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);

            this.attendedEventRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<AttendedEvent>()),
                Times.Never);

            this.reputationServiceMock.VerifyAll();
        }

        [Fact]
        public async Task AttendEventAsync_UserAlreadyEnrolled_DoesNotAddAttendedEvent()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync(new AttendedEvent());

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert
            this.attendedEventRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<AttendedEvent>()),
                Times.Never);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_CreatesAttendedEvent()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            AttendedEvent? capturedAttendedEvent = null;

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Callback<AttendedEvent>(attendedEvent => capturedAttendedEvent = attendedEvent)
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert
            Assert.NotNull(capturedAttendedEvent);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_SetsEvent()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            AttendedEvent? capturedAttendedEvent = null;

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Callback<AttendedEvent>(attendedEvent => capturedAttendedEvent = attendedEvent)
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert
            Assert.NotNull(capturedAttendedEvent!.Event);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_SetsUser()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            AttendedEvent? capturedAttendedEvent = null;

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Callback<AttendedEvent>(attendedEvent => capturedAttendedEvent = attendedEvent)
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert
            Assert.NotNull(capturedAttendedEvent!.User);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_SetsCorrectEventId()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            AttendedEvent? capturedAttendedEvent = null;

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Callback<AttendedEvent>(attendedEvent => capturedAttendedEvent = attendedEvent)
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert

            Assert.Equal(ExampleEventId, capturedAttendedEvent.Event.EventId);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_SetsCorrectUserId()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            AttendedEvent? capturedAttendedEvent = null;

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Callback<AttendedEvent>(attendedEvent => capturedAttendedEvent = attendedEvent)
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert

            Assert.Equal(ExampleUserId, capturedAttendedEvent.User.UserId);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        //from here 
        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_SetsDefaultFlagsToFalse()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            AttendedEvent? capturedAttendedEvent = null;

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Callback<AttendedEvent>(attendedEvent => capturedAttendedEvent = attendedEvent)
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert
            Assert.NotNull(capturedAttendedEvent);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_SetsDefaultFlagsToFalse2()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            AttendedEvent? capturedAttendedEvent = null;

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Callback<AttendedEvent>(attendedEvent => capturedAttendedEvent = attendedEvent)
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert
            Assert.False(capturedAttendedEvent!.IsArchived);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_SetsDefaultFlagsToFalse3()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            AttendedEvent? capturedAttendedEvent = null;

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Callback<AttendedEvent>(attendedEvent => capturedAttendedEvent = attendedEvent)
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert
            Assert.False(capturedAttendedEvent!.IsFavourite);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        //till here

        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_SetsEnrollmentDateNearNow()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            AttendedEvent? capturedAttendedEvent = null;

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Callback<AttendedEvent>(attendedEvent => capturedAttendedEvent = attendedEvent)
                .Returns(Task.CompletedTask);

            DateTime utcNowBeforeCall = DateTime.UtcNow;

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            DateTime utcNowAfterCall = DateTime.UtcNow;

            // Assert
            Assert.NotNull(capturedAttendedEvent);
            Assert.InRange(capturedAttendedEvent!.EnrollmentDate, utcNowBeforeCall, utcNowAfterCall);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task AttendEventAsync_UserNotEnrolled_SendsReputationMessage()
        {
            // Arrange
            this.reputationServiceMock
                .Setup(service => service.CanAttendEventsAsync(ExampleUserId))
                .ReturnsAsync(true);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            this.attendedEventRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<AttendedEvent>()))
                .Returns(Task.CompletedTask);

            ReputationMessage? capturedMessage = null;

            WeakReferenceMessenger.Default.Register<ReputationMessage>(
                this.reputationMessageRecipient,
                (recipient, message) => capturedMessage = message);

            // Act
            await this.attendedEventService.AttendEventAsync(ExampleEventId, ExampleUserId);

            // Assert
            Assert.NotNull(capturedMessage);
            Assert.Equal(ExampleUserId, capturedMessage!.UserId);
            Assert.Equal(ExampleEventId, capturedMessage.EventId);
            Assert.Equal(ReputationAction.EventAttended, capturedMessage.Value);

            this.reputationServiceMock.VerifyAll();
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task GetAsync_WhenRepositoryReturnsNull_ReturnsNull()
        {
            // Arrange
            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync((AttendedEvent?)null);

            // Act
            AttendedEvent? result = await this.attendedEventService.GetAsync(ExampleEventId, ExampleUserId);

            // Assert
            Assert.Null(result);

            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task GetAsync_WhenRepositoryReturnsEntity_ReturnsEntity()
        {
            // Arrange
            var expectedAttendedEvent = new AttendedEvent();

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetAsync(ExampleEventId, ExampleUserId))
                .ReturnsAsync(expectedAttendedEvent);

            // Act
            AttendedEvent? result = await this.attendedEventService.GetAsync(ExampleEventId, ExampleUserId);

            // Assert
            Assert.Same(expectedAttendedEvent, result);

            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task LeaveEventAsync_WhenCalled_DeletesEnrollment()
        {
            // Arrange
            this.attendedEventRepositoryMock
                .Setup(repository => repository.DeleteAsync(ExampleEventId, ExampleUserId))
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.LeaveEventAsync(ExampleEventId, ExampleUserId);

            // Assert
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetArchivedAsync_WhenCalled_UpdatesIsArchivedFlag(bool isArchived)
        {
            // Arrange
            this.attendedEventRepositoryMock
                .Setup(repository => repository.UpdateIsArchivedAsync(ExampleEventId, ExampleUserId, isArchived))
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.SetArchivedAsync(ExampleEventId, ExampleUserId, isArchived);

            // Assert
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetFavouriteAsync_WhenCalled_UpdatesIsFavouriteFlag(bool isFavourite)
        {
            // Arrange
            this.attendedEventRepositoryMock
                .Setup(repository => repository.UpdateIsFavouriteAsync(ExampleEventId, ExampleUserId, isFavourite))
                .Returns(Task.CompletedTask);

            // Act
            await this.attendedEventService.SetFavouriteAsync(ExampleEventId, ExampleUserId, isFavourite);

            // Assert
            this.attendedEventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task GetCommonEventsAsync_WhenCalled_ReturnsRepositoryResult()
        {
            // Arrange
            var expectedCommonEvents = new List<AttendedEvent>
            {
                new AttendedEvent(),
                new AttendedEvent(),
            };

            this.attendedEventRepositoryMock
                .Setup(repository => repository.GetCommonEventsAsync(ExampleUserId, ExampleFriendId))
                .ReturnsAsync(expectedCommonEvents);

            // Act
            List<AttendedEvent> actualCommonEvents = await this.attendedEventService.GetCommonEventsAsync(
                ExampleUserId,
                ExampleFriendId);

            // Assert
            Assert.Same(expectedCommonEvents, actualCommonEvents);

            this.attendedEventRepositoryMock.VerifyAll();
        }

        private static AttendedEventService MakeAttendedEventService(
            Mock<IAttendedEventRepository> attendedEventRepositoryMock,
            Mock<IReputationService> reputationServiceMock)
        {
            return new AttendedEventService(
                attendedEventRepositoryMock.Object,
                reputationServiceMock.Object);
        }
    }
}