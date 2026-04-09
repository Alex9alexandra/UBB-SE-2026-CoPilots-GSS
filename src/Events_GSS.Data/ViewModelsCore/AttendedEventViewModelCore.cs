using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.announcementServices;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.Data.Services.ViewModelCore
{
    /// <summary>
    /// Testable core logic extracted from AttendedEventViewModel
    /// </summary>
    public sealed class AttendedEventViewModelCore
    {
        private readonly IAttendedEventService attendedEventService;
        private readonly IUserService userService;
        private readonly IAnnouncementService announcementService;

        private List<AttendedEvent> allEvents = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendedEventViewModelCore"/> class.
        /// </summary>
        /// <param name="attendedEventService">The attended event service.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="announcementService">The announcement service.</param>
        public AttendedEventViewModelCore(
            IAttendedEventService attendedEventService,
            IUserService userService,
            IAnnouncementService announcementService)
        {
            this.attendedEventService = attendedEventService;
            this.userService = userService;
            this.announcementService = announcementService;
        }

        /// <summary>
        /// Gets the current user.
        /// </summary>
        public User? CurrentUser { get; private set; }

        /// <summary>
        /// Gets a value indicating whether data is currently being loaded.
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Gets the error message if an error occurred.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the current search query.
        /// </summary>
        public string SearchQuery { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the currently selected category filter.
        /// </summary>
        public Category? SelectedCategory { get; private set; }

        /// <summary>
        /// Gets the currently selected sort option.
        /// </summary>
        public SortOption SelectedSort { get; private set; } = SortOption.Default;

        /// <summary>
        /// Gets the list of available categories.
        /// </summary>
        public IReadOnlyList<Category> AvailableCategories { get; private set; } = new List<Category>();

        /// <summary>
        /// Gets the filtered list of friends.
        /// </summary>
        public IReadOnlyList<User> FilteredFriends { get; private set; } = new List<User>();

        /// <summary>
        /// Gets the list of attended events.
        /// </summary>
        public IReadOnlyList<AttendedEvent> AttendedEvents { get; private set; } = new List<AttendedEvent>();

        /// <summary>
        /// Gets the list of archived events.
        /// </summary>
        public IReadOnlyList<AttendedEvent> ArchivedEvents { get; private set; } = new List<AttendedEvent>();

        /// <summary>
        /// Gets the list of favourite events.
        /// </summary>
        public IReadOnlyList<AttendedEvent> FavouriteEvents { get; private set; } = new List<AttendedEvent>();

        /// <summary>
        /// Gets the list of common events between the current user and a friend.
        /// </summary>
        public IReadOnlyList<AttendedEvent> CommonEvents { get; private set; } = new List<AttendedEvent>();

        /// <summary>
        /// Occurs when the state of the view model changes.
        /// </summary>
        public event Action? StateChanged;

        /// <summary>
        /// Defines the available sort options for events.
        /// </summary>
        public enum SortOption
        {
            /// <summary>
            /// Default sort order.
            /// </summary>
            Default,

            /// <summary>
            /// Sort by title in ascending order.
            /// </summary>
            TitleAscending,

            /// <summary>
            /// Sort by title in descending order.
            /// </summary>
            TitleDescending,

            /// <summary>
            /// Sort by category in ascending order.
            /// </summary>
            CategoryAscending,

            /// <summary>
            /// Sort by category in descending order.
            /// </summary>
            CategoryDescending,

            /// <summary>
            /// Sort by start date in ascending order.
            /// </summary>
            StartDateAscending,

            /// <summary>
            /// Sort by start date in descending order.
            /// </summary>
            StartDateDescending,

            /// <summary>
            /// Sort by end date in ascending order.
            /// </summary>
            EndDateAscending,

            /// <summary>
            /// Sort by end date in descending order.
            /// </summary>
            EndDateDescending,
        }

        /// <summary>
        /// Loads the attended events for the current user asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task LoadAsync()
        {
            this.IsLoading = true;
            this.ErrorMessage = null;
            this.StateChanged?.Invoke();

            try
            {
                this.CurrentUser = this.userService.GetCurrentUser();

                this.allEvents = await this.attendedEventService.GetAttendedEventsAsync(this.CurrentUser.UserId);

                this.AvailableCategories = this.allEvents
                    .Where(attendedEvent => attendedEvent.Event.Category != null)
                    .Select(attendedEvent => attendedEvent.Event.Category!)
                    .GroupBy(category => category.CategoryId)
                    .Select(groupingByCategory => groupingByCategory.First())
                    .ToList();

                this.FilteredFriends = this.userService.GetFriends(this.CurrentUser.UserId);

                var unreadCounts = await this.announcementService.GetUnreadCountsForUserAsync(this.CurrentUser.UserId);
                foreach (var attendedEvent in this.allEvents)
                {
                    attendedEvent.UnreadAnnouncementCount =
                        unreadCounts.TryGetValue(attendedEvent.Event.EventId, out var count) ? count : 0;
                }

                this.ApplyFiltersAndSort();
            }
            catch (Exception exception)
            {
                this.ErrorMessage = $"Failed to load events: {exception.Message}";
            }
            finally
            {
                this.IsLoading = false;
                this.StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Loads the common events between the current user and a friend asynchronously.
        /// </summary>
        /// <param name="friend">The friend user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task LoadCommonEventsAsync(User friend)
        {
            if (this.CurrentUser == null)
            {
                throw new InvalidOperationException("CurrentUser is not loaded. Call LoadAsync first.");
            }

            this.CommonEvents = await this.attendedEventService.GetCommonEventsAsync(this.CurrentUser.UserId, friend.UserId);
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the search query and applies filters.
        /// </summary>
        /// <param name="query">The search query.</param>
        public void SetSearchQuery(string query)
        {
            this.SearchQuery = query ?? string.Empty;
            this.ApplyFiltersAndSort();
        }

        /// <summary>
        /// Sets the selected category filter and applies filters.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        public void SetSelectedCategory(Category? category)
        {
            this.SelectedCategory = category;
            this.ApplyFiltersAndSort();
        }

        /// <summary>
        /// Sets the selected sort option and applies sorting.
        /// </summary>
        /// <param name="sort">The sort option.</param>
        public void SetSelectedSort(SortOption sort)
        {
            this.SelectedSort = sort;
            this.ApplyFiltersAndSort();
        }

        /// <summary>
        /// Clears all filters and resets to default state.
        /// </summary>
        public void ClearFilters()
        {
            this.SearchQuery = string.Empty;
            this.SelectedCategory = null;
            this.SelectedSort = SortOption.Default;
            this.ApplyFiltersAndSort();
        }

        /// <summary>
        /// Leaves the specified event asynchronously.
        /// </summary>
        /// <param name="attendedEvent">The event to leave.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task LeaveAsync(AttendedEvent attendedEvent)
        {
            try
            {
                await this.attendedEventService.LeaveEventAsync(attendedEvent.Event.EventId, attendedEvent.User.UserId);
                this.allEvents.Remove(attendedEvent);
                this.ApplyFiltersAndSort();
            }
            catch (Exception exception)
            {
                this.ErrorMessage = $"Failed to leave event: {exception.Message}";
                this.StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Toggles the archived status of the specified event asynchronously.
        /// </summary>
        /// <param name="attendedEvent">The event to archive or unarchive.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SetArchivedAsync(AttendedEvent attendedEvent)
        {
            try
            {
                var newValue = !attendedEvent.IsArchived;
                await this.attendedEventService.SetArchivedAsync(attendedEvent.Event.EventId, attendedEvent.User.UserId, newValue);
                attendedEvent.IsArchived = newValue;
                this.ApplyFiltersAndSort();
            }
            catch (Exception exception)
            {
                this.ErrorMessage = $"Failed to update archive status: {exception.Message}";
                this.StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Toggles the favourite status of the specified event asynchronously.
        /// </summary>
        /// <param name="attendedEvent">The event to mark or unmark as favourite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SetFavouriteAsync(AttendedEvent attendedEvent)
        {
            try
            {
                var newValue = !attendedEvent.IsFavourite;
                await this.attendedEventService.SetFavouriteAsync(attendedEvent.Event.EventId, attendedEvent.User.UserId, newValue);
                attendedEvent.IsFavourite = newValue;
                this.ApplyFiltersAndSort();
            }
            catch (Exception exception)
            {
                this.ErrorMessage = $"Failed to update favourite status: {exception.Message}";
                this.StateChanged?.Invoke();
            }
        }

        private void ApplyFiltersAndSort()
        {
            IEnumerable<AttendedEvent> filtered = allEvents;

            if (!string.IsNullOrWhiteSpace(this.SearchQuery))
            {
                filtered = filtered.Where(ae =>
                    ae.Event.Name.Contains(this.SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (this.SelectedCategory != null)
            {
                filtered = filtered.Where(ae =>
                    ae.Event.Category?.CategoryId == this.SelectedCategory.CategoryId);
            }

            var active = filtered.Where(ae => !ae.IsArchived).ToList();
            var archived = filtered.Where(ae => ae.IsArchived).ToList();

            var favourites = this.allEvents
                .Where(attendedEvent => attendedEvent.IsFavourite && !attendedEvent.IsArchived)
                .Where(attendedEvent => string.IsNullOrWhiteSpace(this.SearchQuery) ||
                             attendedEvent.Event.Name.Contains(this.SearchQuery, StringComparison.OrdinalIgnoreCase))
                .Where(attendedEvent => this.SelectedCategory == null ||
                                        attendedEvent.Event.Category?.CategoryId == this.SelectedCategory.CategoryId)
                .ToList();

            active = this.Sort(active);
            archived = this.Sort(archived);
            favourites = this.Sort(favourites);

            if (this.SelectedSort == SortOption.Default)
            {
                active = active.OrderByDescending(attendedEvent => attendedEvent.IsFavourite).ToList();
            }

            this.AttendedEvents = active;
            this.ArchivedEvents = archived;
            this.FavouriteEvents = favourites;

            this.StateChanged?.Invoke();
        }

        private List<AttendedEvent> Sort(List<AttendedEvent> list)
        {
            return this.SelectedSort switch
            {
                SortOption.TitleAscending => list.OrderBy(attendedEvent => attendedEvent.Event.Name).ToList(),
                SortOption.TitleDescending => list.OrderByDescending(attendedEvent => attendedEvent.Event.Name).ToList(),
                SortOption.CategoryAscending => list.OrderBy(attendedEvent => attendedEvent.Event.Category?.Title).ToList(),
                SortOption.CategoryDescending => list.OrderByDescending(attendedEvent => attendedEvent.Event.Category?.Title).ToList(),
                SortOption.StartDateAscending => list.OrderBy(attendedEvent => attendedEvent.Event.StartDateTime).ToList(),
                SortOption.StartDateDescending => list.OrderByDescending(attendedEvent => attendedEvent.Event.StartDateTime).ToList(),
                SortOption.EndDateAscending => list.OrderBy(attendedEvent => attendedEvent.Event.EndDateTime).ToList(),
                SortOption.EndDateDescending => list.OrderByDescending(attendedEvent => attendedEvent.Event.EndDateTime).ToList(),
                _ => list
            };
        }
    }
}