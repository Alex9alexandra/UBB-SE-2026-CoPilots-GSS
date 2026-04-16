using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.announcementServices;
using Events_GSS.Data.Services.ViewModelCore;
using Events_GSS.Services.Interfaces;

using Moq;

using Xunit;

namespace Events_GSS.Tests.ViewModels
{
    public sealed class AttendedEventViewModelCoreTests
    {
        private const int CurrentUserId = 1;
        private const int FriendUserId = 2;

        private const int EventId1 = 101;
        private const int EventId2 = 102;
        private const int EventId3 = 103;

        private const int CategoryIdMusic = 11;
        private const int CategoryIdSports = 22;

        private readonly Mock<IAttendedEventService> attendedEventServiceMock;
        private readonly Mock<IUserService> userServiceMock;
        private readonly Mock<IAnnouncementService> announcementServiceMock;

        private readonly AttendedEventViewModelCore _viewModelCore;

        public AttendedEventViewModelCoreTests()
        {
            // Setup
            this.attendedEventServiceMock = new Mock<IAttendedEventService>(MockBehavior.Strict);
            this.userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
            this.announcementServiceMock = new Mock<IAnnouncementService>(MockBehavior.Strict);

            this._viewModelCore = MakeCore(
                this.attendedEventServiceMock,
                this.userServiceMock,
                this.announcementServiceMock);
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_LoadsAndComputesAllDerivedState()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(
                    userId: CurrentUserId,
                    eventId: EventId1,
                    name: "Alpha",
                    category: music,
                    start: new DateTime(2026, 1, 1),
                    end: new DateTime(2026, 1, 2),
                    isArchived: false,
                    isFavourite: true),

                MakeAttendedEvent(
                    userId: CurrentUserId,
                    eventId: EventId2,
                    name: "Beta",
                    category: sports,
                    start: new DateTime(2026, 2, 1),
                    end: new DateTime(2026, 2, 2),
                    isArchived: true,
                    isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(
                    userId: CurrentUserId,
                    eventId: EventId3,
                    name: "Gamma",
                    category: null,
                    start: new DateTime(2026, 3, 1),
                    end: new DateTime(2026, 3, 2),
                    isArchived: false,
                    isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            Assert.False(this._viewModelCore.IsLoading);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_SetsErrorMessageNull()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            Assert.Null(this._viewModelCore.ErrorMessage);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_SetsCurrentUser()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            Assert.Same(user, this._viewModelCore.CurrentUser);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_ComputesAvailableCategories()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Categories deduped by CategoryId and excluding nulls (music, sports)
            Assert.Equal(2, this._viewModelCore.AvailableCategories.Select(c => c.CategoryId).Distinct().Count());

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_ComputesAvailableCategories_ContainsMusic()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Categories deduped by CategoryId and excluding nulls (music, sports)
            Assert.Contains(this._viewModelCore.AvailableCategories, c => c.CategoryId == CategoryIdMusic);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_ComputesAvailableCategories_ContainsSports()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Categories deduped by CategoryId and excluding nulls (music, sports)
            Assert.Contains(this._viewModelCore.AvailableCategories, c => c.CategoryId == CategoryIdSports);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_SetsFilteredFriends()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            Assert.Same(friends, this._viewModelCore.FilteredFriends);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_AppliesUnreadAnnouncementCountForEvent1()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Unread counts were applied to each attended event instance
            Assert.Equal(7, events.Single(e => e.Event.EventId == EventId1).UnreadAnnouncementCount);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_AppliesUnreadAnnouncementCountForEvent2DefaultsToZero()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Unread counts were applied to each attended event instance
            Assert.Equal(0, events.Single(e => e.Event.EventId == EventId2).UnreadAnnouncementCount);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_AppliesUnreadAnnouncementCountForEvent3()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Unread counts were applied to each attended event instance
            Assert.Equal(1, events.Single(e => e.Event.EventId == EventId3).UnreadAnnouncementCount);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_ComputesActiveEventsCount()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Active = not archived (EventId1, EventId3)
            Assert.Equal(2, this._viewModelCore.AttendedEvents.Count);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_DoesNotIncludeArchivedEventInActiveList()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Active = not archived (EventId1, EventId3)
            Assert.DoesNotContain(this._viewModelCore.AttendedEvents, ae => ae.Event.EventId == EventId2);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_ComputesArchivedEventsCount()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Archived = archived (EventId2)
            Assert.Single(this._viewModelCore.ArchivedEvents);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_ComputesArchivedEventId()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Archived = archived (EventId2)
            Assert.Equal(EventId2, this._viewModelCore.ArchivedEvents[0].Event.EventId);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_ComputesFavouriteEventsCount()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // FavouriteEvents = favourite && not archived (EventId1)
            Assert.Single(this._viewModelCore.FavouriteEvents);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_ComputesFavouriteEventId()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", music,  new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta",  sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true,  isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null,   new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // FavouriteEvents = favourite && not archived (EventId1)
            Assert.Equal(EventId1, this._viewModelCore.FavouriteEvents[0].Event.EventId);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenCalled_RaisesStateChangedAtLeastThreeTimes()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(
                    userId: CurrentUserId,
                    eventId: EventId1,
                    name: "Alpha",
                    category: music,
                    start: new DateTime(2026, 1, 1),
                    end: new DateTime(2026, 1, 2),
                    isArchived: false,
                    isFavourite: true),

                MakeAttendedEvent(
                    userId: CurrentUserId,
                    eventId: EventId2,
                    name: "Beta",
                    category: sports,
                    start: new DateTime(2026, 2, 1),
                    end: new DateTime(2026, 2, 2),
                    isArchived: true,
                    isFavourite: false),

                // Category is null -> should not appear in AvailableCategories
                MakeAttendedEvent(
                    userId: CurrentUserId,
                    eventId: EventId3,
                    name: "Gamma",
                    category: null,
                    start: new DateTime(2026, 3, 1),
                    end: new DateTime(2026, 3, 2),
                    isArchived: false,
                    isFavourite: false),
            };

            var friends = new List<User>
            {
                new User { UserId = FriendUserId },
            };

            var unreadCounts = new Dictionary<int, int>
            {
                { EventId1, 7 },
                // EventId2 intentionally missing => should become 0
                { EventId3, 1 },
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(friends);

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(unreadCounts);

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            // Load triggers StateChanged at least: start + ApplyFiltersAndSort + finally
            Assert.True(stateChangedCount >= 3);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadAsync_WhenUserServiceThrows_SetsErrorMessageAndStopsLoading()
        {
            // Arrange
            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Throws(new Exception("boom"));

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            Assert.False(this._viewModelCore.IsLoading);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyNoOtherCalls();
            this.announcementServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LoadAsync_WhenUserServiceThrows_SetsErrorMessage()
        {
            // Arrange
            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Throws(new Exception("boom"));

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            Assert.NotNull(this._viewModelCore.ErrorMessage);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyNoOtherCalls();
            this.announcementServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LoadAsync_WhenUserServiceThrows_SetsErrorMessageContent()
        {
            // Arrange
            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Throws(new Exception("boom"));

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            Assert.Contains("Failed to load events: boom", this._viewModelCore.ErrorMessage);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyNoOtherCalls();
            this.announcementServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LoadAsync_WhenUserServiceThrows_RaisesStateChangedAtLeastTwoTimes()
        {
            // Arrange
            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Throws(new Exception("boom"));

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            Assert.True(stateChangedCount >= 2);

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyNoOtherCalls();
            this.announcementServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LoadCommonEventsAsync_WhenCurrentUserNotLoaded_ThrowsInvalidOperationException()
        {
            // Arrange
            var friend = new User { UserId = FriendUserId };

            // Act + Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => this._viewModelCore.LoadCommonEventsAsync(friend));

            Assert.Equal("CurrentUser is not loaded. Call LoadAsync first.", exception.Message);

            this.attendedEventServiceMock.VerifyNoOtherCalls();
            this.userServiceMock.VerifyNoOtherCalls();
            this.announcementServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LoadCommonEventsAsync_WhenCalledAfterLoad_SetsCommonEventsAndRaisesStateChanged()
        {
            // Arrange
            await LoadHappyPathAsync();

            var friend = new User { UserId = FriendUserId };
            var common = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "X", null, DateTime.UtcNow, DateTime.UtcNow, false, false),
            };

            this.attendedEventServiceMock
                .Setup(service => service.GetCommonEventsAsync(CurrentUserId, FriendUserId))
                .ReturnsAsync(common);

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.LoadCommonEventsAsync(friend);

            // Assert
            Assert.Same(common, this._viewModelCore.CommonEvents);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadCommonEventsAsync_WhenCalledAfterLoad_RaisesStateChangedAtLeastOnce()
        {
            // Arrange
            await LoadHappyPathAsync();

            var friend = new User { UserId = FriendUserId };
            var common = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "X", null, DateTime.UtcNow, DateTime.UtcNow, false, false),
            };

            this.attendedEventServiceMock
                .Setup(service => service.GetCommonEventsAsync(CurrentUserId, FriendUserId))
                .ReturnsAsync(common);

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.LoadCommonEventsAsync(friend);

            // Assert
            Assert.True(stateChangedCount >= 1);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetSearchQuery_WhenNull_TreatsAsEmptyAndDoesNotFilterOutItems()
        {
            // Arrange
            await LoadHappyPathAsync();

            // Act
            this._viewModelCore.SetSearchQuery(null!);

            // Assert
            Assert.Equal(string.Empty, this._viewModelCore.SearchQuery);
        }

        [Fact]
        public async Task SetSearchQuery_WhenNull_DoesNotFilterOutItems()
        {
            // Arrange
            await LoadHappyPathAsync();

            // Act
            this._viewModelCore.SetSearchQuery(null!);

            // Assert
            // From LoadHappyPathAsync we have 2 active events (not archived)
            Assert.Equal(2, this._viewModelCore.AttendedEvents.Count);
        }

        [Fact]
        public async Task SetSearchQuery_WhenNonEmpty_FiltersByEventNameCaseInsensitive()
        {
            // Arrange
            await LoadHappyPathAsync();

            // Act
            this._viewModelCore.SetSearchQuery("alpha");

            // Assert
            Assert.Single(this._viewModelCore.AttendedEvents);
        }

        [Fact]
        public async Task SetSearchQuery_WhenNonEmpty_FiltersByEventNameCaseInsensitive_MatchesEventName()
        {
            // Arrange
            await LoadHappyPathAsync();

            // Act
            this._viewModelCore.SetSearchQuery("alpha");

            // Assert
            Assert.Equal("Alpha", this._viewModelCore.AttendedEvents[0].Event.Name);
        }

        [Fact]
        public async Task SetSelectedCategory_WhenSet_FiltersByCategoryId()
        {
            // Arrange
            var (music, sports) = await LoadHappyPathWithCategoriesAsync();

            // Act
            this._viewModelCore.SetSelectedCategory(sports);

            // Assert
            Assert.Single(this._viewModelCore.AttendedEvents);
        }

        [Fact]
        public async Task SetSelectedCategory_WhenSet_FiltersByCategoryId_MatchesEventName()
        {
            // Arrange
            var (music, sports) = await LoadHappyPathWithCategoriesAsync();

            // Act
            this._viewModelCore.SetSelectedCategory(sports);

            // Assert
            Assert.Equal("BetaActive", this._viewModelCore.AttendedEvents[0].Event.Name);
        }

        [Fact]
        public async Task ClearFilters_WhenCalled_ResetsSearchCategoryAndSortToDefault()
        {
            // Arrange
            var (music, _) = await LoadHappyPathWithCategoriesAsync();
            this._viewModelCore.SetSearchQuery("Alpha");
            this._viewModelCore.SetSelectedCategory(music);
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.TitleDescending);

            // Act
            this._viewModelCore.ClearFilters();

            // Assert
            Assert.Equal(string.Empty, this._viewModelCore.SearchQuery);
        }

        [Fact]
        public async Task ClearFilters_WhenCalled_ResetsSelectedCategory()
        {
            // Arrange
            var (music, _) = await LoadHappyPathWithCategoriesAsync();
            this._viewModelCore.SetSearchQuery("Alpha");
            this._viewModelCore.SetSelectedCategory(music);
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.TitleDescending);

            // Act
            this._viewModelCore.ClearFilters();

            // Assert
            Assert.Null(this._viewModelCore.SelectedCategory);
        }

        [Fact]
        public async Task ClearFilters_WhenCalled_ResetsSelectedSortToDefault()
        {
            // Arrange
            var (music, _) = await LoadHappyPathWithCategoriesAsync();
            this._viewModelCore.SetSearchQuery("Alpha");
            this._viewModelCore.SetSelectedCategory(music);
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.TitleDescending);

            // Act
            this._viewModelCore.ClearFilters();

            // Assert
            Assert.Equal(AttendedEventViewModelCore.SortOption.Default, this._viewModelCore.SelectedSort);
        }

        [Fact]
        public async Task ClearFilters_WhenCalled_ResetsActiveCount()
        {
            // Arrange
            var (music, _) = await LoadHappyPathWithCategoriesAsync();
            this._viewModelCore.SetSearchQuery("Alpha");
            this._viewModelCore.SetSelectedCategory(music);
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.TitleDescending);

            // Act
            this._viewModelCore.ClearFilters();

            // Assert
            // Back to full active set (2 active events in that setup)
            Assert.Equal(2, this._viewModelCore.AttendedEvents.Count);
        }

        [Fact]
        public async Task SetSelectedSort_WhenTitleAscending_SortsByNameAscending()
        {
            // Arrange
            await LoadHappyPathAsync(); // active names are Alpha and Gamma

            // Act
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.TitleAscending);

            // Assert
            Assert.Equal(new[] { "Alpha", "Gamma" }, this._viewModelCore.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenTitleDescending_SortsByNameDescending()
        {
            // Arrange
            await LoadHappyPathAsync();

            // Act
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.TitleDescending);

            // Assert
            Assert.Equal(new[] { "Gamma", "Alpha" }, this._viewModelCore.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenCategoryAscending_SortsByCategoryTitle()
        {
            // Arrange
            var (music, sports) = await LoadHappyPathWithCategoriesAsync();
            _ = music;
            _ = sports;

            // Act
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.CategoryAscending);

            // Assert
            // "Music" then "Sports"
            Assert.Equal(new[] { "AlphaActive", "BetaActive" }, this._viewModelCore.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenCategoryDescending_SortsByCategoryTitleDescending()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync();

            // Act
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.CategoryDescending);

            // Assert
            Assert.Equal(new[] { "BetaActive", "AlphaActive" }, this._viewModelCore.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenStartDateAscending_SortsByStartDate()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync(); // AlphaActive has earlier start than BetaActive

            // Act
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.StartDateAscending);

            // Assert
            Assert.Equal(new[] { "AlphaActive", "BetaActive" }, this._viewModelCore.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenStartDateDescending_SortsByStartDateDescending()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync();

            // Act
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.StartDateDescending);

            // Assert
            Assert.Equal(new[] { "BetaActive", "AlphaActive" }, this._viewModelCore.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenEndDateAscending_SortsByEndDate()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync();

            // Act
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.EndDateAscending);

            // Assert
            Assert.Equal(new[] { "AlphaActive", "BetaActive" }, this._viewModelCore.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenEndDateDescending_SortsByEndDateDescending()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync();

            // Act
            this._viewModelCore.SetSelectedSort(AttendedEventViewModelCore.SortOption.EndDateDescending);

            // Assert
            Assert.Equal(new[] { "BetaActive", "AlphaActive" }, this._viewModelCore.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task LeaveAsync_WhenServiceSucceeds_RemovesEventFromLists()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var toLeave = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.LeaveEventAsync(EventId3, CurrentUserId))
                .Returns(Task.CompletedTask);

            // Act
            await this._viewModelCore.LeaveAsync(toLeave);

            // Assert
            Assert.DoesNotContain(this._viewModelCore.AttendedEvents, ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LeaveAsync_WhenServiceSucceeds_SetsErrorMessageNull()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var toLeave = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.LeaveEventAsync(EventId3, CurrentUserId))
                .Returns(Task.CompletedTask);

            // Act
            await this._viewModelCore.LeaveAsync(toLeave);

            // Assert
            Assert.Null(this._viewModelCore.ErrorMessage);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LeaveAsync_WhenServiceThrows_SetsErrorMessage()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var toLeave = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.LeaveEventAsync(EventId3, CurrentUserId))
                .ThrowsAsync(new Exception("nope"));

            // Act
            await this._viewModelCore.LeaveAsync(toLeave);

            // Assert
            Assert.NotNull(this._viewModelCore.ErrorMessage);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LeaveAsync_WhenServiceThrows_SetsErrorMessageContent()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var toLeave = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.LeaveEventAsync(EventId3, CurrentUserId))
                .ThrowsAsync(new Exception("nope"));

            // Act
            await this._viewModelCore.LeaveAsync(toLeave);

            // Assert
            Assert.Contains("Failed to leave event: nope", this._viewModelCore.ErrorMessage);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LeaveAsync_WhenServiceThrows_RaisesStateChangedAtLeastOnce()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var toLeave = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.LeaveEventAsync(EventId3, CurrentUserId))
                .ThrowsAsync(new Exception("nope"));

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.LeaveAsync(toLeave);

            // Assert
            Assert.True(stateChangedCount >= 1);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetArchivedAsync_WhenServiceSucceeds_TogglesArchiveAndMovesBetweenLists()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetArchivedAsync(EventId3, CurrentUserId, true))
                .Returns(Task.CompletedTask);

            // Act
            await this._viewModelCore.SetArchivedAsync(target);

            // Assert
            Assert.True(target.IsArchived);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetArchivedAsync_WhenServiceSucceeds_RemovesFromActiveList()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetArchivedAsync(EventId3, CurrentUserId, true))
                .Returns(Task.CompletedTask);

            // Act
            await this._viewModelCore.SetArchivedAsync(target);

            // Assert
            Assert.DoesNotContain(this._viewModelCore.AttendedEvents, ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetArchivedAsync_WhenServiceSucceeds_AddsToArchivedList()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetArchivedAsync(EventId3, CurrentUserId, true))
                .Returns(Task.CompletedTask);

            // Act
            await this._viewModelCore.SetArchivedAsync(target);

            // Assert
            Assert.Contains(this._viewModelCore.ArchivedEvents, ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetArchivedAsync_WhenServiceThrows_SetsIsArchivedFalse()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetArchivedAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("bad"));

            // Act
            await this._viewModelCore.SetArchivedAsync(target);

            // Assert
            Assert.False(target.IsArchived);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetArchivedAsync_WhenServiceThrows_SetsErrorMessage()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetArchivedAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("bad"));

            // Act
            await this._viewModelCore.SetArchivedAsync(target);

            // Assert
            Assert.NotNull(this._viewModelCore.ErrorMessage);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetArchivedAsync_WhenServiceThrows_SetsErrorMessageContent()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetArchivedAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("bad"));

            // Act
            await this._viewModelCore.SetArchivedAsync(target);

            // Assert
            Assert.Contains("Failed to update archive status: bad", this._viewModelCore.ErrorMessage);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetArchivedAsync_WhenServiceThrows_RaisesStateChangedAtLeastOnce()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetArchivedAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("bad"));

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.SetArchivedAsync(target);

            // Assert
            Assert.True(stateChangedCount >= 1);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetFavouriteAsync_WhenServiceSucceeds_TogglesFavouriteAndUpdatesFavouriteEvents()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetFavouriteAsync(EventId3, CurrentUserId, true))
                .Returns(Task.CompletedTask);

            // Act
            await this._viewModelCore.SetFavouriteAsync(target);

            // Assert
            Assert.True(target.IsFavourite);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetFavouriteAsync_WhenServiceSucceeds_AddsToFavouriteEvents()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetFavouriteAsync(EventId3, CurrentUserId, true))
                .Returns(Task.CompletedTask);

            // Act
            await this._viewModelCore.SetFavouriteAsync(target);

            // Assert
            Assert.Contains(this._viewModelCore.FavouriteEvents, ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetFavouriteAsync_WhenServiceThrows_SetsIsFavouriteFalse()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetFavouriteAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("badfav"));

            // Act
            await this._viewModelCore.SetFavouriteAsync(target);

            // Assert
            Assert.False(target.IsFavourite);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetFavouriteAsync_WhenServiceThrows_SetsErrorMessage()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetFavouriteAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("badfav"));

            // Act
            await this._viewModelCore.SetFavouriteAsync(target);

            // Assert
            Assert.NotNull(this._viewModelCore.ErrorMessage);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetFavouriteAsync_WhenServiceThrows_SetsErrorMessageContent()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetFavouriteAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("badfav"));

            // Act
            await this._viewModelCore.SetFavouriteAsync(target);

            // Assert
            Assert.Contains("Failed to update favourite status: badfav", this._viewModelCore.ErrorMessage);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetFavouriteAsync_WhenServiceThrows_RaisesStateChangedAtLeastOnce()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetFavouriteAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("badfav"));

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.SetFavouriteAsync(target);

            // Assert
            Assert.True(stateChangedCount >= 1);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task DefaultSort_WhenSomeActiveAreFavourite_OrdersFavouritesFirst()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "A", null, DateTime.UtcNow, DateTime.UtcNow, isArchived: false, isFavourite: false),
                MakeAttendedEvent(CurrentUserId, EventId2, "B", null, DateTime.UtcNow, DateTime.UtcNow, isArchived: false, isFavourite: true),
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(new List<User>());

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(new Dictionary<int, int>());

            // Act
            await this._viewModelCore.LoadAsync();

            // Assert
            Assert.Equal(new[] { EventId2, EventId1 }, this._viewModelCore.AttendedEvents.Select(ae => ae.Event.EventId).ToArray());

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        private static AttendedEventViewModelCore MakeCore(
            Mock<IAttendedEventService> attendedEventServiceMock,
            Mock<IUserService> userServiceMock,
            Mock<IAnnouncementService> announcementServiceMock)
        {
            return new AttendedEventViewModelCore(
                attendedEventServiceMock.Object,
                userServiceMock.Object,
                announcementServiceMock.Object);
        }

        private static AttendedEvent MakeAttendedEvent(
            int userId,
            int eventId,
            string name,
            Category? category,
            DateTime start,
            DateTime end,
            bool isArchived,
            bool isFavourite)
        {
            return new AttendedEvent
            {
                User = new User { UserId = userId },
                Event = new Event
                {
                    EventId = eventId,
                    Name = name,
                    Category = category,
                    StartDateTime = start,
                    EndDateTime = end,
                },
                IsArchived = isArchived,
                IsFavourite = isFavourite,
            };
        }

        private async Task LoadHappyPathAsync()
        {
            await LoadHappyPathReturningAllEventsAsync();
        }

        private async Task<(Category music, Category sports)> LoadHappyPathWithCategoriesAsync()
        {
            var user = new User { UserId = CurrentUserId };

            var music = new Category { CategoryId = CategoryIdMusic, Title = "Music" };
            var sports = new Category { CategoryId = CategoryIdSports, Title = "Sports" };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "AlphaActive", music, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: false),
                MakeAttendedEvent(CurrentUserId, EventId2, "BetaActive", sports, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: false, isFavourite: false),

                // archived extra to ensure archived path exists too
                MakeAttendedEvent(CurrentUserId, EventId3, "ZArchived", music, new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: true, isFavourite: false),
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(new List<User>());

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(new Dictionary<int, int>());

            await this._viewModelCore.LoadAsync();

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();

            return (music, sports);
        }

        private async Task<List<AttendedEvent>> LoadHappyPathReturningAllEventsAsync()
        {
            var user = new User { UserId = CurrentUserId };

            var events = new List<AttendedEvent>
            {
                MakeAttendedEvent(CurrentUserId, EventId1, "Alpha", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), isArchived: false, isFavourite: true),
                MakeAttendedEvent(CurrentUserId, EventId2, "Beta", null, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), isArchived: true, isFavourite: false),
                MakeAttendedEvent(CurrentUserId, EventId3, "Gamma", null, new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), isArchived: false, isFavourite: false),
            };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this.attendedEventServiceMock
                .Setup(service => service.GetAttendedEventsAsync(CurrentUserId))
                .ReturnsAsync(events);

            this.userServiceMock
                .Setup(service => service.GetFriends(CurrentUserId))
                .Returns(new List<User>());

            this.announcementServiceMock
                .Setup(service => service.GetUnreadCountsForUserAsync(CurrentUserId))
                .ReturnsAsync(new Dictionary<int, int>());

            await this._viewModelCore.LoadAsync();

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();

            return events;
        }
    }
}