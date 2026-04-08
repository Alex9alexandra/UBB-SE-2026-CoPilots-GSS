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
    public sealed class AttendedEventCore
    {
        private readonly IAttendedEventService _attendedEventService;
        private readonly IUserService _userService;
        private readonly IAnnouncementService _announcementService;

        private List<AttendedEvent> _allEvents = new();

        public AttendedEventCore(
            IAttendedEventService attendedEventService,
            IUserService userService,
            IAnnouncementService announcementService)
        {
            _attendedEventService = attendedEventService;
            _userService = userService;
            _announcementService = announcementService;
        }

        public User? CurrentUser { get; private set; }

        public bool IsLoading { get; private set; }
        public string? ErrorMessage { get; private set; }

        public string SearchQuery { get; private set; } = string.Empty;
        public Category? SelectedCategory { get; private set; }
        public SortOption SelectedSort { get; private set; } = SortOption.Default;

        public IReadOnlyList<Category> AvailableCategories { get; private set; } = new List<Category>();
        public IReadOnlyList<User> FilteredFriends { get; private set; } = new List<User>();

        public IReadOnlyList<AttendedEvent> AttendedEvents { get; private set; } = new List<AttendedEvent>();
        public IReadOnlyList<AttendedEvent> ArchivedEvents { get; private set; } = new List<AttendedEvent>();
        public IReadOnlyList<AttendedEvent> FavouriteEvents { get; private set; } = new List<AttendedEvent>();
        public IReadOnlyList<AttendedEvent> CommonEvents { get; private set; } = new List<AttendedEvent>();

        public event Action? StateChanged;

        public enum SortOption
        {
            Default,
            TitleAscending,
            TitleDescending,
            CategoryAscending,
            CategoryDescending,
            StartDateAscending,
            StartDateDescending,
            EndDateAscending,
            EndDateDescending,
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            StateChanged?.Invoke();

            try
            {
                CurrentUser = _userService.GetCurrentUser();

                _allEvents = await _attendedEventService.GetAttendedEventsAsync(CurrentUser.UserId);

                AvailableCategories = _allEvents
                    .Where(ae => ae.Event.Category != null)
                    .Select(ae => ae.Event.Category!)
                    .GroupBy(c => c.CategoryId)
                    .Select(g => g.First())
                    .ToList();

                FilteredFriends = _userService.GetFriends(CurrentUser.UserId);

                var unreadCounts = await _announcementService.GetUnreadCountsForUserAsync(CurrentUser.UserId);
                foreach (var attendedEvent in _allEvents)
                {
                    attendedEvent.UnreadAnnouncementCount =
                        unreadCounts.TryGetValue(attendedEvent.Event.EventId, out var count) ? count : 0;
                }

                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load events: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                StateChanged?.Invoke();
            }
        }

        public async Task LoadCommonEventsAsync(User friend)
        {
            if (CurrentUser == null)
            {
                throw new InvalidOperationException("CurrentUser is not loaded. Call LoadAsync first.");
            }

            CommonEvents = await _attendedEventService.GetCommonEventsAsync(CurrentUser.UserId, friend.UserId);
            StateChanged?.Invoke();
        }

        public void SetSearchQuery(string query)
        {
            SearchQuery = query ?? string.Empty;
            ApplyFiltersAndSort();
        }

        public void SetSelectedCategory(Category? category)
        {
            SelectedCategory = category;
            ApplyFiltersAndSort();
        }

        public void SetSelectedSort(SortOption sort)
        {
            SelectedSort = sort;
            ApplyFiltersAndSort();
        }

        public void ClearFilters()
        {
            SearchQuery = string.Empty;
            SelectedCategory = null;
            SelectedSort = SortOption.Default;
            ApplyFiltersAndSort();
        }

        public async Task LeaveAsync(AttendedEvent attendedEvent)
        {
            try
            {
                await _attendedEventService.LeaveEventAsync(attendedEvent.Event.EventId, attendedEvent.User.UserId);
                _allEvents.Remove(attendedEvent);
                ApplyFiltersAndSort();
            }
            catch (Exception exception)
            {
                ErrorMessage = $"Failed to leave event: {exception.Message}";
                StateChanged?.Invoke();
            }
        }

        public async Task SetArchivedAsync(AttendedEvent attendedEvent)
        {
            try
            {
                var newValue = !attendedEvent.IsArchived;
                await _attendedEventService.SetArchivedAsync(attendedEvent.Event.EventId, attendedEvent.User.UserId, newValue);
                attendedEvent.IsArchived = newValue;
                ApplyFiltersAndSort();
            }
            catch (Exception exception)
            {
                ErrorMessage = $"Failed to update archive status: {exception.Message}";
                StateChanged?.Invoke();
            }
        }

        public async Task SetFavouriteAsync(AttendedEvent attendedEvent)
        {
            try
            {
                var newValue = !attendedEvent.IsFavourite;
                await _attendedEventService.SetFavouriteAsync(attendedEvent.Event.EventId, attendedEvent.User.UserId, newValue);
                attendedEvent.IsFavourite = newValue;
                ApplyFiltersAndSort();
            }
            catch (Exception exception)
            {
                ErrorMessage = $"Failed to update favourite status: {exception.Message}";
                StateChanged?.Invoke();
            }
        }

        private void ApplyFiltersAndSort()
        {
            IEnumerable<AttendedEvent> filtered = _allEvents;

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                filtered = filtered.Where(ae =>
                    ae.Event.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedCategory != null)
            {
                filtered = filtered.Where(ae =>
                    ae.Event.Category?.CategoryId == SelectedCategory.CategoryId);
            }

            var active = filtered.Where(ae => !ae.IsArchived).ToList();
            var archived = filtered.Where(ae => ae.IsArchived).ToList();

            var favourites = _allEvents
                .Where(ae => ae.IsFavourite && !ae.IsArchived)
                .Where(ae => string.IsNullOrWhiteSpace(SearchQuery) ||
                             ae.Event.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .Where(ae => SelectedCategory == null ||
                             ae.Event.Category?.CategoryId == SelectedCategory.CategoryId)
                .ToList();

            active = Sort(active);
            archived = Sort(archived);
            favourites = Sort(favourites);

            if (SelectedSort == SortOption.Default)
            {
                active = active.OrderByDescending(ae => ae.IsFavourite).ToList();
            }

            AttendedEvents = active;
            ArchivedEvents = archived;
            FavouriteEvents = favourites;

            StateChanged?.Invoke();
        }

        private List<AttendedEvent> Sort(List<AttendedEvent> list)
        {
            return SelectedSort switch
            {
                SortOption.TitleAscending => list.OrderBy(ae => ae.Event.Name).ToList(),
                SortOption.TitleDescending => list.OrderByDescending(ae => ae.Event.Name).ToList(),
                SortOption.CategoryAscending => list.OrderBy(ae => ae.Event.Category?.Title).ToList(),
                SortOption.CategoryDescending => list.OrderByDescending(ae => ae.Event.Category?.Title).ToList(),
                SortOption.StartDateAscending => list.OrderBy(ae => ae.Event.StartDateTime).ToList(),
                SortOption.StartDateDescending => list.OrderByDescending(ae => ae.Event.StartDateTime).ToList(),
                SortOption.EndDateAscending => list.OrderBy(ae => ae.Event.EndDateTime).ToList(),
                SortOption.EndDateDescending => list.OrderByDescending(ae => ae.Event.EndDateTime).ToList(),
                _ => list
            };
        }
    }
}