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

namespace Events_GSS.Tests.Services
{
    public sealed class AttendedEventCoreTests
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

        private readonly AttendedEventCore core;

        public AttendedEventCoreTests()
        {
            // Setup
            this.attendedEventServiceMock = new Mock<IAttendedEventService>(MockBehavior.Strict);
            this.userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
            this.announcementServiceMock = new Mock<IAnnouncementService>(MockBehavior.Strict);

            this.core = MakeCore(
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
            this.core.StateChanged += () => stateChangedCount++;

            // Act
            await this.core.LoadAsync();

            // Assert
            Assert.False(this.core.IsLoading);
            Assert.Null(this.core.ErrorMessage);

            Assert.Same(user, this.core.CurrentUser);

            // Categories deduped by CategoryId and excluding nulls (music, sports)
            Assert.Equal(2, this.core.AvailableCategories.Count);
            Assert.Contains(this.core.AvailableCategories, c => c.CategoryId == CategoryIdMusic);
            Assert.Contains(this.core.AvailableCategories, c => c.CategoryId == CategoryIdSports);

            Assert.Same(friends, this.core.FilteredFriends);

            // Unread counts were applied to each attended event instance
            Assert.Equal(7, events.Single(e => e.Event.EventId == EventId1).UnreadAnnouncementCount);
            Assert.Equal(0, events.Single(e => e.Event.EventId == EventId2).UnreadAnnouncementCount);
            Assert.Equal(1, events.Single(e => e.Event.EventId == EventId3).UnreadAnnouncementCount);

            // Active = not archived (EventId1, EventId3)
            Assert.Equal(2, this.core.AttendedEvents.Count);
            Assert.DoesNotContain(this.core.AttendedEvents, ae => ae.Event.EventId == EventId2);

            // Archived = archived (EventId2)
            Assert.Single(this.core.ArchivedEvents);
            Assert.Equal(EventId2, this.core.ArchivedEvents[0].Event.EventId);

            // FavouriteEvents = favourite && not archived (EventId1)
            Assert.Single(this.core.FavouriteEvents);
            Assert.Equal(EventId1, this.core.FavouriteEvents[0].Event.EventId);

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
            this.core.StateChanged += () => stateChangedCount++;

            // Act
            await this.core.LoadAsync();

            // Assert
            Assert.False(this.core.IsLoading);
            Assert.NotNull(this.core.ErrorMessage);
            Assert.Contains("Failed to load events: boom", this.core.ErrorMessage);

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
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => this.core.LoadCommonEventsAsync(friend));
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
            this.core.StateChanged += () => stateChangedCount++;

            // Act
            await this.core.LoadCommonEventsAsync(friend);

            // Assert
            Assert.Same(common, this.core.CommonEvents);
            Assert.True(stateChangedCount >= 1);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetSearchQuery_WhenNull_TreatsAsEmptyAndDoesNotFilterOutItems()
        {
            // Arrange
            await LoadHappyPathAsync();

            // Act
            this.core.SetSearchQuery(null!);

            // Assert
            Assert.Equal(string.Empty, this.core.SearchQuery);

            // From LoadHappyPathAsync we have 2 active events (not archived)
            Assert.Equal(2, this.core.AttendedEvents.Count);
        }

        [Fact]
        public async Task SetSearchQuery_WhenNonEmpty_FiltersByEventNameCaseInsensitive()
        {
            // Arrange
            await LoadHappyPathAsync();

            // Act
            this.core.SetSearchQuery("alpha");

            // Assert
            Assert.Single(this.core.AttendedEvents);
            Assert.Equal("Alpha", this.core.AttendedEvents[0].Event.Name);
        }

        [Fact]
        public async Task SetSelectedCategory_WhenSet_FiltersByCategoryId()
        {
            // Arrange
            var (music, sports) = await LoadHappyPathWithCategoriesAsync();

            // Act
            this.core.SetSelectedCategory(sports);

            // Assert
            Assert.Single(this.core.AttendedEvents);
            Assert.Equal("BetaActive", this.core.AttendedEvents[0].Event.Name);
        }

        [Fact]
        public async Task ClearFilters_WhenCalled_ResetsSearchCategoryAndSortToDefault()
        {
            // Arrange
            var (music, _) = await LoadHappyPathWithCategoriesAsync();
            this.core.SetSearchQuery("Alpha");
            this.core.SetSelectedCategory(music);
            this.core.SetSelectedSort(AttendedEventCore.SortOption.TitleDescending);

            // Act
            this.core.ClearFilters();

            // Assert
            Assert.Equal(string.Empty, this.core.SearchQuery);
            Assert.Null(this.core.SelectedCategory);
            Assert.Equal(AttendedEventCore.SortOption.Default, this.core.SelectedSort);

            // Back to full active set (2 active events in that setup)
            Assert.Equal(2, this.core.AttendedEvents.Count);
        }

        [Fact]
        public async Task SetSelectedSort_WhenTitleAscending_SortsByNameAscending()
        {
            // Arrange
            await LoadHappyPathAsync(); // active names are Alpha and Gamma

            // Act
            this.core.SetSelectedSort(AttendedEventCore.SortOption.TitleAscending);

            // Assert
            Assert.Equal(new[] { "Alpha", "Gamma" }, this.core.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenTitleDescending_SortsByNameDescending()
        {
            // Arrange
            await LoadHappyPathAsync();

            // Act
            this.core.SetSelectedSort(AttendedEventCore.SortOption.TitleDescending);

            // Assert
            Assert.Equal(new[] { "Gamma", "Alpha" }, this.core.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenCategoryAscending_SortsByCategoryTitle()
        {
            // Arrange
            var (music, sports) = await LoadHappyPathWithCategoriesAsync();
            _ = music;
            _ = sports;

            // Act
            this.core.SetSelectedSort(AttendedEventCore.SortOption.CategoryAscending);

            // Assert
            // "Music" then "Sports"
            Assert.Equal(new[] { "AlphaActive", "BetaActive" }, this.core.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenCategoryDescending_SortsByCategoryTitleDescending()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync();

            // Act
            this.core.SetSelectedSort(AttendedEventCore.SortOption.CategoryDescending);

            // Assert
            Assert.Equal(new[] { "BetaActive", "AlphaActive" }, this.core.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenStartDateAscending_SortsByStartDate()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync(); // AlphaActive has earlier start than BetaActive

            // Act
            this.core.SetSelectedSort(AttendedEventCore.SortOption.StartDateAscending);

            // Assert
            Assert.Equal(new[] { "AlphaActive", "BetaActive" }, this.core.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenStartDateDescending_SortsByStartDateDescending()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync();

            // Act
            this.core.SetSelectedSort(AttendedEventCore.SortOption.StartDateDescending);

            // Assert
            Assert.Equal(new[] { "BetaActive", "AlphaActive" }, this.core.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenEndDateAscending_SortsByEndDate()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync();

            // Act
            this.core.SetSelectedSort(AttendedEventCore.SortOption.EndDateAscending);

            // Assert
            Assert.Equal(new[] { "AlphaActive", "BetaActive" }, this.core.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
        }

        [Fact]
        public async Task SetSelectedSort_WhenEndDateDescending_SortsByEndDateDescending()
        {
            // Arrange
            await LoadHappyPathWithCategoriesAsync();

            // Act
            this.core.SetSelectedSort(AttendedEventCore.SortOption.EndDateDescending);

            // Assert
            Assert.Equal(new[] { "BetaActive", "AlphaActive" }, this.core.AttendedEvents.Select(ae => ae.Event.Name).ToArray());
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
            await this.core.LeaveAsync(toLeave);

            // Assert
            Assert.DoesNotContain(this.core.AttendedEvents, ae => ae.Event.EventId == EventId3);
            Assert.Null(this.core.ErrorMessage);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LeaveAsync_WhenServiceThrows_SetsErrorMessageAndRaisesStateChanged()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var toLeave = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.LeaveEventAsync(EventId3, CurrentUserId))
                .ThrowsAsync(new Exception("nope"));

            var stateChangedCount = 0;
            this.core.StateChanged += () => stateChangedCount++;

            // Act
            await this.core.LeaveAsync(toLeave);

            // Assert
            Assert.NotNull(this.core.ErrorMessage);
            Assert.Contains("Failed to leave event: nope", this.core.ErrorMessage);
            Assert.True(stateChangedCount >= 1);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetArchivedAsync_WhenServiceSucceeds_TogglesArchiveAndMovesBetweenLists()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);
            Assert.False(target.IsArchived);

            this.attendedEventServiceMock
                .Setup(service => service.SetArchivedAsync(EventId3, CurrentUserId, true))
                .Returns(Task.CompletedTask);

            // Act
            await this.core.SetArchivedAsync(target);

            // Assert
            Assert.True(target.IsArchived);
            Assert.DoesNotContain(this.core.AttendedEvents, ae => ae.Event.EventId == EventId3);
            Assert.Contains(this.core.ArchivedEvents, ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetArchivedAsync_WhenServiceThrows_SetsErrorMessageAndRaisesStateChanged()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetArchivedAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("bad"));

            var stateChangedCount = 0;
            this.core.StateChanged += () => stateChangedCount++;

            // Act
            await this.core.SetArchivedAsync(target);

            // Assert
            Assert.False(target.IsArchived);
            Assert.NotNull(this.core.ErrorMessage);
            Assert.Contains("Failed to update archive status: bad", this.core.ErrorMessage);
            Assert.True(stateChangedCount >= 1);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetFavouriteAsync_WhenServiceSucceeds_TogglesFavouriteAndUpdatesFavouriteEvents()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);
            Assert.False(target.IsFavourite);

            this.attendedEventServiceMock
                .Setup(service => service.SetFavouriteAsync(EventId3, CurrentUserId, true))
                .Returns(Task.CompletedTask);

            // Act
            await this.core.SetFavouriteAsync(target);

            // Assert
            Assert.True(target.IsFavourite);
            Assert.Contains(this.core.FavouriteEvents, ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock.VerifyAll();
        }

        [Fact]
        public async Task SetFavouriteAsync_WhenServiceThrows_SetsErrorMessageAndRaisesStateChanged()
        {
            // Arrange
            var loaded = await LoadHappyPathReturningAllEventsAsync();
            var target = loaded.Single(ae => ae.Event.EventId == EventId3);

            this.attendedEventServiceMock
                .Setup(service => service.SetFavouriteAsync(EventId3, CurrentUserId, true))
                .ThrowsAsync(new Exception("badfav"));

            var stateChangedCount = 0;
            this.core.StateChanged += () => stateChangedCount++;

            // Act
            await this.core.SetFavouriteAsync(target);

            // Assert
            Assert.False(target.IsFavourite);
            Assert.NotNull(this.core.ErrorMessage);
            Assert.Contains("Failed to update favourite status: badfav", this.core.ErrorMessage);
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
            await this.core.LoadAsync();

            // Assert
            Assert.Equal(new[] { EventId2, EventId1 }, this.core.AttendedEvents.Select(ae => ae.Event.EventId).ToArray());

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();
        }

        private static AttendedEventCore MakeCore(
            Mock<IAttendedEventService> attendedEventServiceMock,
            Mock<IUserService> userServiceMock,
            Mock<IAnnouncementService> announcementServiceMock)
        {
            return new AttendedEventCore(
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

            await this.core.LoadAsync();

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

            await this.core.LoadAsync();

            this.userServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.announcementServiceMock.VerifyAll();

            return events;
        }
    }
}