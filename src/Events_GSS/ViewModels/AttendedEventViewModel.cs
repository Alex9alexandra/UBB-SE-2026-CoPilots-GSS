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

namespace Events_GSS.ViewModels
{
    public class AttendedEventViewModel : INotifyPropertyChanged
    {
        public Array SortOptions => Enum.GetValues(typeof(SortOption));

        private readonly IAttendedEventService _attendedEventService;
        private readonly IUserService _userService;
        private readonly IReputationService _reputationService;

        // The full unfiltered list — never modify this directly after loading.
        // Always filter/sort from this source.
        private List<AttendedEvent> _allEvents = new();

        // ─── Observable collections bound to the UI ───────────────────────

        private ObservableCollection<AttendedEvent> _attendedEvents = new();
        public ObservableCollection<AttendedEvent> AttendedEvents
        {
            get => _attendedEvents;
            private set { _attendedEvents = value; OnPropertyChanged(); }
        }

        private ObservableCollection<AttendedEvent> _archivedEvents = new();
        public ObservableCollection<AttendedEvent> ArchivedEvents
        {
            get => _archivedEvents;
            private set { _archivedEvents = value; OnPropertyChanged(); }
        }

        private ObservableCollection<AttendedEvent> _favouriteEvents = new();
        public ObservableCollection<AttendedEvent> FavouriteEvents
        {
            get => _favouriteEvents;
            private set { _favouriteEvents = value; OnPropertyChanged(); }
        }

        private ObservableCollection<User> _filteredFriends = new();
        public ObservableCollection<User> FilteredFriends
        {
            get => _filteredFriends;
            private set { _filteredFriends = value; OnPropertyChanged(); }
        }

        private ObservableCollection<AttendedEvent> _commonEvents = new();
        public ObservableCollection<AttendedEvent> CommonEvents
        {
            get => _commonEvents;
            private set { _commonEvents = value; OnPropertyChanged(); }
        }

        // ─── Search & filter state ────────────────────────────────────────

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                ApplyFiltersAndSort();
            }
        }

        private string _friendSearchQuery = string.Empty;
        public string FriendSearchQuery
        {
            get => _friendSearchQuery;
            set
            {
                _friendSearchQuery = value;
                OnPropertyChanged();
                FilteredFriends = new ObservableCollection<User>(_userService.SearchFriends(CurrentUser.UserId, _friendSearchQuery));
            }
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                ApplyFiltersAndSort();
            }
        }

        private SortOption _selectedSort = SortOption.Default;
        public SortOption SelectedSort
        {
            get => _selectedSort;
            set
            {
                _selectedSort = value;
                OnPropertyChanged();
                ApplyFiltersAndSort();
            }
        }

        // Populated from the loaded events — used to fill the category filter dropdown.
        private ObservableCollection<Category> _availableCategories = new();
        public ObservableCollection<Category> AvailableCategories
        {
            get => _availableCategories;
            private set { _availableCategories = value; OnPropertyChanged(); }
        }

        // ─── Current user & RP display ────────────────────────────────────

        private User? _currentUser;
        public User? CurrentUser
        {
            get => _currentUser;
            private set { _currentUser = value; OnPropertyChanged(); }
        }

        private ReputationViewModel? _reputationViewModel;
        public ReputationViewModel? ReputationViewModel
        {
            get => _reputationViewModel;
            private set { _reputationViewModel = value; OnPropertyChanged(); }
        }

        // ─── UI state

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set { _isLoading = value; OnPropertyChanged(); }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            private set { _errorMessage = value; OnPropertyChanged(); }
        }

        // ─── Commands ─────────────────────────────────────────────────────

        public ICommand LoadCommand { get; }
        public ICommand LeaveCommand { get; }
        public ICommand SetArchivedCommand { get; }
        public ICommand SetFavouriteCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        // ─── Constructor ──────────────────────────────────────────────────

        public AttendedEventViewModel(IAttendedEventService attendedEventService, IUserService userService, IReputationService reputationService)
        {
            _attendedEventService = attendedEventService;
            _userService = userService;
            _reputationService = reputationService;

            LoadCommand = new RelayCommandAttEv(async _ => await LoadAsync());
            LeaveCommand = new RelayCommandAttEv(async p => await LeaveAsync(p), p => p is AttendedEvent);
            SetArchivedCommand = new RelayCommandAttEv(async p => await SetArchivedAsync(p), p => p is AttendedEvent);
            SetFavouriteCommand = new RelayCommandAttEv(async p => await SetFavouriteAsync(p), p => p is AttendedEvent);
            ClearFiltersCommand = new RelayCommandAttEv(_ => ClearFilters());
        }

        // ─── Load ─────────────────────────────────────────────────────────

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                CurrentUser = _userService.GetCurrentUser();

                ReputationViewModel = new ReputationViewModel(_userService, _reputationService);
                await ReputationViewModel.LoadAsync();

                var all = await _attendedEventService.GetAttendedEventsAsync(CurrentUser.UserId);
                _allEvents = all;

                // Populate category dropdown from whatever categories exist in the loaded events.
                var categories = _allEvents
                    .Where(ae => ae.Event.Category != null)
                    .Select(ae => ae.Event.Category!)
                    .DistinctBy(c => c.CategoryId)
                    .ToList();

                AvailableCategories = new ObservableCollection<Category>(categories);
                FilteredFriends = new ObservableCollection<User>(_userService.GetFriends(CurrentUser.UserId));

                ApplyFiltersAndSort();

            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load events: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadCommonEventsAsync(User friend)
        {
            var common_events = await _attendedEventService.GetCommonEventsAsync(CurrentUser.UserId, friend.UserId);
            CommonEvents = new ObservableCollection<AttendedEvent>(common_events);
        }

        // ─── Filtering & sorting ──────────────────────────────────────────

        // Central method — called whenever search, category, or sort changes.
        // Always operates on _allEvents so nothing is permanently lost.
        private void ApplyFiltersAndSort()
        {
            var filtered = _allEvents.AsEnumerable();

            // Filter by search query (event title)
            if (!string.IsNullOrWhiteSpace(SearchQuery))
                filtered = filtered.Where(ae =>
                    ae.Event.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

            // Filter by selected category
            if (SelectedCategory != null)
                filtered = filtered.Where(ae =>
                    ae.Event.Category?.CategoryId == SelectedCategory.CategoryId);

            // Split archived / non-archived before sorting
            var active = filtered.Where(ae => !ae.IsArchived).ToList();
            var archived = filtered.Where(ae => ae.IsArchived).ToList();
            var favourites = _allEvents
                .Where(ae => ae.IsFavourite && !ae.IsArchived)
                .Where(ae => string.IsNullOrWhiteSpace(SearchQuery) ||
                             ae.Event.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .Where(ae => SelectedCategory == null ||
                             ae.Event.Category?.CategoryId == SelectedCategory.CategoryId)
                .ToList();

            // Sort
            active = Sort(active);
            archived = Sort(archived);

            // When default sort is active, favourites bubble to the top (req 5.5)
            if (SelectedSort == SortOption.Default)
            {
                active = active
                    .OrderByDescending(ae => ae.IsFavourite)
                    .ToList();
            }

            AttendedEvents = new ObservableCollection<AttendedEvent>(active);
            ArchivedEvents = new ObservableCollection<AttendedEvent>(archived);
            FavouriteEvents = new ObservableCollection<AttendedEvent>(Sort(favourites));
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
                _ => list  // Default — order preserved, favourites handled separately
            };
        }

        private void ClearFilters()
        {
            _searchQuery = string.Empty;
            OnPropertyChanged(nameof(SearchQuery));
            _selectedCategory = null;
            OnPropertyChanged(nameof(SelectedCategory));
            _selectedSort = SortOption.Default;
            OnPropertyChanged(nameof(SelectedSort));
            ApplyFiltersAndSort();
        }

        // ─── Commands implementation ──────────────────────────────────────

        public async Task LeaveAsync(object? parameter)
        {
            if (parameter is not AttendedEvent ae) return;

            try
            {
                await _attendedEventService.LeaveEventAsync(ae.Event.EventId, ae.User.UserId);
                _allEvents.Remove(ae);
                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to leave event: {ex.Message}";
            }
        }

        public async Task SetArchivedAsync(object? parameter)
        {
            if (parameter is not AttendedEvent ae) return;

            try
            {
                var newValue = !ae.IsArchived;
                await _attendedEventService.SetArchivedAsync(ae.Event.EventId, ae.User.UserId, newValue);
                ae.IsArchived = newValue;
                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update archive status: {ex.Message}";
            }
        }

        public async Task SetFavouriteAsync(object? parameter)
        {
            if (parameter is not AttendedEvent ae) return;

            try
            {
                var newValue = !ae.IsFavourite;
                await _attendedEventService.SetFavouriteAsync(ae.Event.EventId, ae.User.UserId, newValue);
                ae.IsFavourite = newValue;
                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update favourite status: {ex.Message}";
            }
        }

        // ─── INotifyPropertyChanged ───────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─── RelayCommand ─────────────────────────────────────────────────────

    public class RelayCommandAttEv : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public RelayCommandAttEv(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        // Convenience constructor for synchronous actions
        public RelayCommandAttEv(Action<object?> execute, Func<object?, bool>? canExecute = null)
            : this(p => { execute(p); return Task.CompletedTask; }, canExecute) { }

        public bool CanExecute(object? parameter)
            => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try { await _executeAsync(parameter); }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    // ─── SortOption enum ──────────────────────────────────────────────────

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
        EndDateDescending
    }
}