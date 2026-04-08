using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.reputationService;
using Events_GSS.Services;
using Events_GSS.Services.Interfaces;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Events_GSS.Data.Services.announcementServices;

namespace Events_GSS.ViewModels
{
    /// <summary>
    /// ViewModel for managing attended events, including filtering, sorting, and user interactions.
    /// </summary>
    public partial class AttendedEventViewModel : INotifyPropertyChanged
    {
        private readonly IAttendedEventService attendedEventService;
        private readonly IUserService userService;
        private readonly IReputationService reputationService;
        private readonly IAnnouncementService announcementService;
        private ObservableCollection<AttendedEvent> archivedEvents = new ();
        private ObservableCollection<AttendedEvent> favouriteEvents = new ();
        private ObservableCollection<User> filteredFriends = new ();
        private List<AttendedEvent> allEvents = new ();
        private ObservableCollection<AttendedEvent> commonEvents = new ();
        private string searchQuery = string.Empty;
        private string friendSearchQuery = string.Empty;
        private ObservableCollection<AttendedEvent> attendedEvents = new ();
        private Category? selectedCategory;
        private RelayCommandAttEv.SortOption selectedSort = RelayCommandAttEv.SortOption.Default;
        private ObservableCollection<Category> availableCategories = new ();
        private User? currentUser;
        private ReputationViewModel? reputationViewModel;
        private bool isLoading;
        private string? errorMessage;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the available sort options for events.
        /// </summary>
        public static Array SortOptions => Enum.GetValues(typeof(RelayCommandAttEv.SortOption));


        // The full unfiltered list — never modify this directly after loading.
        // Always filter/sort from this source.

        // ─── Observable collections bound to the UI ───────────────────────

        /// <summary>
        /// Gets the collection of attended events.
        /// </summary>
        public ObservableCollection<AttendedEvent> AttendedEvents
        {
            get => this.attendedEvents;
            private set
            {
                this.attendedEvents = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the collection of archived events.
        /// </summary>
        public ObservableCollection<AttendedEvent> ArchivedEvents
        {
            get => this.archivedEvents;
            private set
            {
                this.archivedEvents = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the collection of favourite events.
        /// </summary>
        public ObservableCollection<AttendedEvent> FavouriteEvents
        {
            get => this.favouriteEvents;
            private set
            {
                this.favouriteEvents = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the collection of filtered friends.
        /// </summary>
        public ObservableCollection<User> FilteredFriends
        {
            get => this.filteredFriends;
            private set
            {
                this.filteredFriends = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the collection of common events between the current user and a friend.
        /// </summary>
        public ObservableCollection<AttendedEvent> CommonEvents
        {
            get => this.commonEvents;
            private set
            {
                this.commonEvents = value;
                this.OnPropertyChanged();

            }
        }

        // ─── Search & filter state ────────────────────────────────────────

        /// <summary>
        /// Gets or sets the search query for filtering events by name.
        /// </summary>
        public string SearchQuery
        {
            get => this.searchQuery;
            set
            {
                this.searchQuery = value;
                this.OnPropertyChanged();
                this.ApplyFiltersAndSort();
            }
        }

        /// <summary>
        /// Gets or sets the search query for filtering friends by name.
        /// </summary>
        public string FriendSearchQuery
        {
            get => this.friendSearchQuery;
            set
            {
                this.friendSearchQuery = value;
                this.OnPropertyChanged();
                this.FilteredFriends = new ObservableCollection<User>(this.userService.SearchFriends(this.CurrentUser.UserId, this.friendSearchQuery));
            }
        }

        /// <summary>
        /// Gets or sets the selected category for filtering events.
        /// </summary>
        public Category? SelectedCategory
        {
            get => this.selectedCategory;
            set
            {
                this.selectedCategory = value;
                this.OnPropertyChanged();
                this.ApplyFiltersAndSort();
            }
        }

        /// <summary>
        /// Gets or sets the selected sort option for events.
        /// </summary>
        public RelayCommandAttEv.SortOption SelectedSort
        {
            get => this.selectedSort;
            set
            {
                this.selectedSort = value;
                this.OnPropertyChanged();
                this.ApplyFiltersAndSort();
            }
        }

        // Populated from the loaded events — used to fill the category filter dropdown.

        /// <summary>
        /// Gets the available categories from loaded events.
        /// </summary>
        public ObservableCollection<Category> AvailableCategories
        {
            get => this.availableCategories;
            private set
            {
                this.availableCategories = value;
                this.OnPropertyChanged();
            }
        }

        // ─── Current user & RP display ────────────────────────────────────

        /// <summary>
        /// Gets the current logged-in user.
        /// </summary>
        public User? CurrentUser
        {
            get => this.currentUser;
            private set
            {
                this.currentUser = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the reputation view model for the current user.
        /// </summary>
        public ReputationViewModel? ReputationViewModel
        {
            get => this.reputationViewModel;
            private set
            {
                this.reputationViewModel = value;
                this.OnPropertyChanged();
            }
        }

        // ─── UI state

        /// <summary>
        /// Gets a value indicating whether the view model is currently loading data.
        /// </summary>
        public bool IsLoading
        {
            get => this.isLoading;
            private set
            {
                this.isLoading = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the current error message, if any.
        /// </summary>
        public string? ErrorMessage
        {
            get => this.errorMessage;
            private set
            {
                this.errorMessage = value;
                this.OnPropertyChanged();
            }
        }

        // ─── Commands ─────────────────────────────────────────────────────

        /// <summary>
        /// Gets the command for loading attended events.
        /// </summary>
        public ICommand LoadCommand { get; }

        /// <summary>
        /// Gets the command for leaving an event.
        /// </summary>
        public ICommand LeaveCommand { get; }

        /// <summary>
        /// Gets the command for setting the archived status of an event.
        /// </summary>
        public ICommand SetArchivedCommand { get; }

        /// <summary>
        /// Gets the command for setting the favourite status of an event.
        /// </summary>
        public ICommand SetFavouriteCommand { get; }

        /// <summary>
        /// Gets the command for clearing all filters.
        /// </summary>
        public ICommand ClearFiltersCommand { get; }

        // ─── Constructor ──────────────────────────────────────────────────

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendedEventViewModel"/> class.
        /// </summary>
        /// <param name="attendedEventService">The attended event service.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="announcementService">The announcement service.</param>
        /// <param name="reputationService">The reputation service.</param>
        public AttendedEventViewModel(IAttendedEventService attendedEventService, IUserService userService, IAnnouncementService announcementService, IReputationService reputationService)
        {
            this.attendedEventService = attendedEventService;
            this.userService = userService;
            this.reputationService = reputationService;

            this.LoadCommand = new RelayCommandAttEv(async _ => await this.LoadAsync());
            this.LeaveCommand = new RelayCommandAttEv(async p => await this.LeaveAsync(p), p => p is AttendedEvent);
            this.SetArchivedCommand = new RelayCommandAttEv(async p => await this.SetArchivedAsync(p), p => p is AttendedEvent);
            this.SetFavouriteCommand = new RelayCommandAttEv(async p => await this.SetFavouriteAsync(p), p => p is AttendedEvent);
            this.ClearFiltersCommand = new RelayCommandAttEv(_ => this.ClearFilters());
            this.announcementService = announcementService;
        }

        // ─── Load ─────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the attended events for the current user.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadAsync()
        {
            this.IsLoading = true;
            this.ErrorMessage = null;

            try
            {
                this.CurrentUser = this.userService.GetCurrentUser();

                this.ReputationViewModel = new ReputationViewModel(this.userService, this.reputationService);
                await this.ReputationViewModel.LoadAsync();

                var events = await this.attendedEventService.GetAttendedEventsAsync(this.CurrentUser.UserId);
                this.allEvents = events;

                // Populate category dropdown from whatever categories exist in the loaded events.
                var categories = this.allEvents
                    .Where(attendedEvent => attendedEvent.Event.Category != null)
                    .Select(attendedEvent => attendedEvent.Event.Category!)
                    .DistinctBy(category => category.CategoryId)
                    .ToList();

                this.AvailableCategories = new ObservableCollection<Category>(categories);
                this.FilteredFriends = new ObservableCollection<User>(this.userService.GetFriends(this.CurrentUser.UserId));

                var unreadCounts = await this.announcementService.GetUnreadCountsForUserAsync(this.CurrentUser.UserId);
                foreach (var attendedEvent in this.allEvents)
                {
                    attendedEvent.UnreadAnnouncementCount = unreadCounts.TryGetValue(attendedEvent.Event.EventId, out var count) ? count : 0;
                }

                this.ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Failed to load events: {ex.Message}";
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        /// <summary>
        /// Loads common events between the current user and a friend.
        /// </summary>
        /// <param name="friend">The friend to compare events with.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadCommonEventsAsync(User friend)
        {
            var commonEventsList = await this.attendedEventService.GetCommonEventsAsync(this.CurrentUser.UserId, friend.UserId);
            this.CommonEvents = new ObservableCollection<AttendedEvent>(commonEventsList);
        }

        // ─── Filtering & sorting ──────────────────────────────────────────

        // Central method — called whenever search, category, or sort changes.
        // Always operates on _allEvents so nothing is permanently lost.
        private void ApplyFiltersAndSort()
        {
            var filteredAttendedEvents = this.allEvents.AsEnumerable();

            // Filter by search query (event title)
            if (!string.IsNullOrWhiteSpace(this.SearchQuery))
            {
                filteredAttendedEvents = filteredAttendedEvents.Where(attendedEvent =>
                    attendedEvent.Event.Name.Contains(this.SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by selected category
            if (this.SelectedCategory != null)
            {
                filteredAttendedEvents = filteredAttendedEvents.Where(attendedEvent =>
                    attendedEvent.Event.Category?.CategoryId == this.SelectedCategory.CategoryId);
            }

            // Split archived / non-archived before sorting
            var activeAttendedEvents = filteredAttendedEvents.Where(attendedEvent => !attendedEvent.IsArchived).ToList();
            var archivedAttendedEvents = filteredAttendedEvents.Where(attendedEvent => attendedEvent.IsArchived).ToList();
            var favouriteAttendedEvents = this.allEvents
                .Where(attendedEvent => attendedEvent.IsFavourite && !attendedEvent.IsArchived)
                .Where(attendedEvent => string.IsNullOrWhiteSpace(this.SearchQuery) ||
                             attendedEvent.Event.Name.Contains(this.SearchQuery, StringComparison.OrdinalIgnoreCase))
                .Where(attendedEvent => this.SelectedCategory == null ||
                             attendedEvent.Event.Category?.CategoryId == this.SelectedCategory.CategoryId)
                .ToList();

            // Sort
            activeAttendedEvents = this.Sort(activeAttendedEvents);
            archivedAttendedEvents = this.Sort(archivedAttendedEvents);

            // When default sort is active, favourites bubble to the top (req 5.5)
            if (this.SelectedSort == RelayCommandAttEv.SortOption.Default)
            {
                activeAttendedEvents = activeAttendedEvents
                    .OrderByDescending(attendedEvent => attendedEvent.IsFavourite)
                    .ToList();
            }

            this.AttendedEvents = new ObservableCollection<AttendedEvent>(activeAttendedEvents);
            this.ArchivedEvents = new ObservableCollection<AttendedEvent>(archivedAttendedEvents);
            this.FavouriteEvents = new ObservableCollection<AttendedEvent>(this.Sort(favouriteAttendedEvents));
        }

        private List<AttendedEvent> Sort(List<AttendedEvent> list)
        {
            return this.SelectedSort switch
            {
                RelayCommandAttEv.SortOption.TitleAscending => list.OrderBy(attendedEvent => attendedEvent.Event.Name).ToList(),
                RelayCommandAttEv.SortOption.TitleDescending => list.OrderByDescending(attendedEvent => attendedEvent.Event.Name).ToList(),
                RelayCommandAttEv.SortOption.CategoryAscending => list.OrderBy(attendedEvent => attendedEvent.Event.Category?.Title).ToList(),
                RelayCommandAttEv.SortOption.CategoryDescending => list.OrderByDescending(attendedEvent => attendedEvent.Event.Category?.Title).ToList(),
                RelayCommandAttEv.SortOption.StartDateAscending => list.OrderBy(attendedEvent => attendedEvent.Event.StartDateTime).ToList(),
                RelayCommandAttEv.SortOption.StartDateDescending => list.OrderByDescending(attendedEvent => attendedEvent.Event.StartDateTime).ToList(),
                RelayCommandAttEv.SortOption.EndDateAscending => list.OrderBy(attendedEvent => attendedEvent.Event.EndDateTime).ToList(),
                RelayCommandAttEv.SortOption.EndDateDescending => list.OrderByDescending(attendedEvent => attendedEvent.Event.EndDateTime).ToList(),
                _ => list// Default — order preserved, favourites handled separately
            };
        }

        private void ClearFilters()
        {
            this.searchQuery = string.Empty;
            this.OnPropertyChanged(nameof(this.SearchQuery));
            this.selectedCategory = null;
            this.OnPropertyChanged(nameof(this.SelectedCategory));
            this.selectedSort = RelayCommandAttEv.SortOption.Default;
            this.OnPropertyChanged(nameof(this.SelectedSort));
            this.ApplyFiltersAndSort();
        }

        // ─── Commands implementation ──────────────────────────────────────

        /// <summary>
        /// Leaves an event asynchronously.
        /// </summary>
        /// <param name="parameter">The attended event to leave.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LeaveAsync(object? parameter)
        {
            if (parameter is not AttendedEvent attendedEvent)
            {
                return;
            }

            try
            {
                await this.attendedEventService.LeaveEventAsync(attendedEvent.Event.EventId, attendedEvent.User.UserId);
                this.allEvents.Remove(attendedEvent);
                this.ApplyFiltersAndSort();
            }
            catch (Exception exception)
            {
                this.ErrorMessage = $"Failed to leave event: {exception.Message}";
            }
        }

        /// <summary>
        /// Sets the archived status of an event asynchronously.
        /// </summary>
        /// <param name="parameter">The attended event to archive or unarchive.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SetArchivedAsync(object? parameter)
        {
            if (parameter is not AttendedEvent attendedEvent)
            {
                return;
            }

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
            }
        }

        /// <summary>
        /// Sets the favourite status of an event asynchronously.
        /// </summary>
        /// <param name="parameter">The attended event to favourite or unfavourite.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SetFavouriteAsync(object? parameter)
        {
            if (parameter is not AttendedEvent attendedEvent)
            {
                return;
            }

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
            }
        }

        // ─── INotifyPropertyChanged ───────────────────────────────────────

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─── RelayCommand ─────────────────────────────────────────────────────

    /// <summary>
    /// A relay command implementation for handling commands with async support.
    /// </summary>
    public class RelayCommandAttEv : ICommand
    {
        private readonly Func<object?, Task> executeAsync;
        private readonly Func<object?, bool>? canExecute;
        private bool isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommandAttEv"/> class.
        /// </summary>
        /// <param name="executeAsync">The async execute method.</param>
        /// <param name="canExecute">The can execute method.</param>
        public RelayCommandAttEv(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            this.executeAsync = executeAsync;
            this.canExecute = canExecute;
        }

        // Convenience constructor for synchronous actions

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommandAttEv"/> class with a synchronous execute method.
        /// </summary>
        /// <param name="execute">The execute method.</param>
        /// <param name="canExecute">The can execute method.</param>
        public RelayCommandAttEv(Action<object?> execute, Func<object?, bool>? canExecute = null)
            : this(
                p =>
                {
                    execute(p);
                    return Task.CompletedTask;
                }, canExecute)
        {
        }

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        // ─── SortOption enum ──────────────────────────────────────────────────

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
        /// Determines whether the command can execute.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object? parameter)
            => !this.isExecuting && (this.canExecute?.Invoke(parameter) ?? true);

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        public async void Execute(object? parameter)
        {
            if (!this.CanExecute(parameter))
            {
                return;
            }

            this.isExecuting = true;
            this.RaiseCanExecuteChanged();
            try
            {
                await this.executeAsync(parameter);
            }
            finally
            {
                this.isExecuting = false;
                this.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Raises the CanExecuteChanged event.
        /// </summary>
        public void RaiseCanExecuteChanged()
            => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}