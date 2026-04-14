using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;

using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services.eventServices;
using Events_GSS.Data.Services.reputationService;

using Moq;

using Xunit;

namespace Events_GSS.Tests.Services
{
    public sealed class EventServiceTests : IDisposable
    {
        private const int ExampleEventId = 11;
        private const int ExampleAdminUserId = 22;

        private readonly Mock<IEventRepository> eventRepositoryMock;
        private readonly Mock<IReputationService> reputationServiceMock;

        private readonly EventService eventService;

        private readonly object reputationMessageRecipient;

        public EventServiceTests()
        {
            // Setup
            this.eventRepositoryMock = new Mock<IEventRepository>(MockBehavior.Strict);
            this.reputationServiceMock = new Mock<IReputationService>(MockBehavior.Strict);

            this.eventService = MakeEventService(this.eventRepositoryMock, this.reputationServiceMock);

            this.reputationMessageRecipient = new object();
        }

        public void Dispose()
        {
            // TearDown
            WeakReferenceMessenger.Default.UnregisterAll(this.reputationMessageRecipient);
        }

        [Fact]
        public async Task GetAllPublicActiveEventsAsync_WhenCalled_ReturnsRepositoryResult()
        {
            // Arrange
            var expectedEvents = new List<Event>
            {
                new Event(),
                new Event(),
            };

            this.eventRepositoryMock
                .Setup(repository => repository.GetAllPublicActiveAsync())
                .ReturnsAsync(expectedEvents);

            // Act
            List<Event> actualEvents = await this.eventService.GetAllPublicActiveEventsAsync();

            // Assert
            Assert.Same(expectedEvents, actualEvents);

            this.eventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task GetEventByIdAsync_WhenCalled_ReturnsRepositoryResult()
        {
            // Arrange
            var expectedEvent = new Event();

            this.eventRepositoryMock
                .Setup(repository => repository.GetByIdAsync(ExampleEventId))
                .ReturnsAsync(expectedEvent);

            // Act
            Event? actualEvent = await this.eventService.GetEventByIdAsync(ExampleEventId);

            // Assert
            Assert.Same(expectedEvent, actualEvent);

            this.eventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_ReputationTooLow_ThrowsInvalidOperationException()
        {
            // Arrange
            Event eventEntity = MakeEventWithAdminUserId(ExampleAdminUserId);

            this.reputationServiceMock
                .Setup(service => service.CanCreateEventsAsync(ExampleAdminUserId))
                .ReturnsAsync(false);

            // Act
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.eventService.CreateEventAsync(eventEntity));

            // Assert
            Assert.Equal("Your reputation is too low to create events (below -700 RP).", exception.Message);

            this.eventRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<Event>()),
                Times.Never);

            this.reputationServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_ReputationHighEnough_ReturnsRepositoryEventId()
        {
            // Arrange
            Event eventEntity = MakeEventWithAdminUserId(ExampleAdminUserId);

            this.reputationServiceMock
                .Setup(service => service.CanCreateEventsAsync(ExampleAdminUserId))
                .ReturnsAsync(true);

            this.eventRepositoryMock
                .Setup(repository => repository.AddAsync(eventEntity))
                .ReturnsAsync(ExampleEventId);

            // Act
            int createdEventId = await this.eventService.CreateEventAsync(eventEntity);

            // Assert
            Assert.Equal(ExampleEventId, createdEventId);

            this.reputationServiceMock.VerifyAll();
            this.eventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_ReputationHighEnough_SendsReputationMessage()
        {
            // Arrange
            Event eventEntity = MakeEventWithAdminUserId(ExampleAdminUserId);

            this.reputationServiceMock
                .Setup(service => service.CanCreateEventsAsync(ExampleAdminUserId))
                .ReturnsAsync(true);

            this.eventRepositoryMock
                .Setup(repository => repository.AddAsync(eventEntity))
                .ReturnsAsync(ExampleEventId);

            ReputationMessage? capturedReputationMessage = null;

            WeakReferenceMessenger.Default.Register<ReputationMessage>(
                this.reputationMessageRecipient,
                (recipient, message) => capturedReputationMessage = message);

            var expectedReputationMessage = new ReputationMessage(ExampleAdminUserId, ReputationAction.EventCreated, null);

            // Act
            await this.eventService.CreateEventAsync(eventEntity);

            // Assert
            Assert.Equivalent(expectedReputationMessage,capturedReputationMessage);

            this.reputationServiceMock.VerifyAll();
            this.eventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task UpdateEventAsync_WhenCalled_UpdatesEvent()
        {
            // Arrange
            var eventEntity = new Event();

            this.eventRepositoryMock
                .Setup(repository => repository.UpdateAsync(eventEntity))
                .Returns(Task.CompletedTask);

            // Act
            await this.eventService.UpdateEventAsync(eventEntity);

            // Assert
            this.eventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task DeleteEventAsync_WhenCalled_DeletesEvent()
        {
            // Arrange
            this.eventRepositoryMock
                .Setup(repository => repository.GetByIdAsync(ExampleEventId))
                .ReturnsAsync((Event?)null);

            this.eventRepositoryMock
                .Setup(repository => repository.DeleteAsync(ExampleEventId))
                .Returns(Task.CompletedTask);

            // Act
            await this.eventService.DeleteEventAsync(ExampleEventId);

            // Assert
            this.eventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task DeleteEventAsync_WhenAdminExists_SendsReputationMessage()
        {
            // Arrange
            Event eventEntity = MakeEventWithAdminUserId(ExampleAdminUserId);

            this.eventRepositoryMock
                .Setup(repository => repository.GetByIdAsync(ExampleEventId))
                .ReturnsAsync(eventEntity);

            this.eventRepositoryMock
                .Setup(repository => repository.DeleteAsync(ExampleEventId))
                .Returns(Task.CompletedTask);

            ReputationMessage? capturedReputationMessage = null;

            WeakReferenceMessenger.Default.Register<ReputationMessage>(
                this.reputationMessageRecipient,
                (recipient, message) => capturedReputationMessage = message);

            var expectedReputationMessage=new ReputationMessage(ExampleAdminUserId, ReputationAction.EventCancelled, null);

            // Act
            await this.eventService.DeleteEventAsync(ExampleEventId);

            // Assert
            Assert.Equivalent(expectedReputationMessage, capturedReputationMessage);

            this.eventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task DeleteEventAsync_WhenAdminIsNull_DoesNotSendReputationMessage()
        {
            // Arrange
            var eventEntity = new Event { Admin = null };

            this.eventRepositoryMock
                .Setup(repository => repository.GetByIdAsync(ExampleEventId))
                .ReturnsAsync(eventEntity);

            this.eventRepositoryMock
                .Setup(repository => repository.DeleteAsync(ExampleEventId))
                .Returns(Task.CompletedTask);

            ReputationMessage? capturedMessage = null;

            WeakReferenceMessenger.Default.Register<ReputationMessage>(
                this.reputationMessageRecipient,
                (recipient, message) => capturedMessage = message);

            // Act
            await this.eventService.DeleteEventAsync(ExampleEventId);

            // Assert
            Assert.Null(capturedMessage);

            this.eventRepositoryMock.VerifyAll();
        }

        [Theory]
        [InlineData("Music", 2)]
        [InlineData("Sports", 1)]
        [InlineData("Unknown", 0)]
        public async Task FilterByCategoryAsync_WhenCalled_ReturnsMatchingEvents(string category, int expectedCount)
        {
            // Arrange
            var events = new List<Event>
            {
                MakeEventWithCategoryTitle("Music"),
                MakeEventWithCategoryTitle("Music"),
                MakeEventWithCategoryTitle("Sports"),
                new Event { Category = null },
            };

            this.eventRepositoryMock
                .Setup(repository => repository.GetAllPublicActiveAsync())
                .ReturnsAsync(events);

            // Act
            List<Event> result = await this.eventService.FilterByCategoryAsync(category);

            // Assert
            Assert.Equal(expectedCount, result.Count);

            this.eventRepositoryMock.VerifyAll();
        }

        [Theory]
        [InlineData("Cluj", 2)]
        [InlineData("Bucharest", 1)]
        [InlineData("Nope", 0)]
        public async Task FilterByLocationAsync_WhenCalled_ReturnsEventsWithNameContainingLocation(string location, int expectedCount)
        {
            // Arrange
            var events = new List<Event>
            {
                new Event { Name = "Cluj - Concert" },
                new Event { Name = "Conference in Cluj" },
                new Event { Name = "Bucharest Meetup" },
            };

            this.eventRepositoryMock
                .Setup(repository => repository.GetAllPublicActiveAsync())
                .ReturnsAsync(events);

            // Act
            List<Event> result = await this.eventService.FilterByLocationAsync(location);

            // Assert
            Assert.Equal(expectedCount, result.Count);

            this.eventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task FilterByDateAsync_WhenCalled_ReturnsEventsOnSameDate()
        {
            // Arrange
            DateTime targetDate = new DateTime(2026, 4, 8);

            var events = new List<Event>
            {
                new Event { StartDateTime = new DateTime(2026, 4, 8, 10, 0, 0) },
                new Event { StartDateTime = new DateTime(2026, 4, 8, 23, 59, 0) },
                new Event { StartDateTime = new DateTime(2026, 4, 9, 0, 0, 0) },
            };

            this.eventRepositoryMock
                .Setup(repository => repository.GetAllPublicActiveAsync())
                .ReturnsAsync(events);

            // Act
            List<Event> result = await this.eventService.FilterByDateAsync(targetDate);

            // Assert
            Assert.Equal(2, result.Count);

            this.eventRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task FilterByDateRangeAsync_WhenCalled_ReturnsEventsWithinRangeInclusive()
        {
            // Arrange
            DateTime fromDate = new DateTime(2026, 4, 1);
            DateTime toDate = new DateTime(2026, 4, 3);

            var events = new List<Event>
            {
                new Event { StartDateTime = new DateTime(2026, 3, 31, 12, 0, 0) },
                new Event { StartDateTime = new DateTime(2026, 4, 1, 0, 0, 0) },
                new Event { StartDateTime = new DateTime(2026, 4, 2, 18, 0, 0) },
                new Event { StartDateTime = new DateTime(2026, 4, 3, 23, 59, 59) },
                new Event { StartDateTime = new DateTime(2026, 4, 4, 0, 0, 0) },
            };

            this.eventRepositoryMock
                .Setup(repository => repository.GetAllPublicActiveAsync())
                .ReturnsAsync(events);

            // Act
            List<Event> result = await this.eventService.FilterByDateRangeAsync(fromDate, toDate);

            // Assert
            Assert.Equal(3, result.Count);

            this.eventRepositoryMock.VerifyAll();
        }

        [Theory]
        [InlineData("concert", 2)]
        [InlineData("MEETUP", 1)]
        [InlineData("missing", 0)]
        public async Task SearchByTitleAsync_WhenCalled_ReturnsEventsWithNameContainingTitle(string title, int expectedCount)
        {
            // Arrange
            var events = new List<Event>
            {
                new Event { Name = "Big Concert Tonight" },
                new Event { Name = "concert rehearsal" },
                new Event { Name = "Local Meetup" },
            };

            this.eventRepositoryMock
                .Setup(repository => repository.GetAllPublicActiveAsync())
                .ReturnsAsync(events);

            // Act
            List<Event> result = await this.eventService.SearchByTitleAsync(title);

            // Assert
            Assert.Equal(expectedCount, result.Count);

            this.eventRepositoryMock.VerifyAll();
        }

        private static EventService MakeEventService(
            Mock<IEventRepository> eventRepositoryMock,
            Mock<IReputationService> reputationServiceMock)
        {
            return new EventService(eventRepositoryMock.Object, reputationServiceMock.Object);
        }

        private static Event MakeEventWithAdminUserId(int adminUserId)
        {
            return new Event
            {
                Admin = new User { UserId = adminUserId },
                Name = "Example Event",
                StartDateTime = new DateTime(2026, 4, 8),
            };
        }

        private static Event MakeEventWithCategoryTitle(string categoryTitle)
        {
            return new Event
            {
                Category = new Category { Title = categoryTitle },
                Name = "Example Event",
                StartDateTime = new DateTime(2026, 4, 8),
            };
        }
    }
}