using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.Interfaces;

namespace Events_GSS.ViewModels
{
    public class MemoryViewModel : INotifyPropertyChanged
    {
        private readonly IMemoryService _memoryService;

        private Event _event = null!;
        private User _currentUser = null!;

        private ObservableCollection<MemoryItemViewModel> _memories = new();
        private ObservableCollection<string> _galleryPhotos = new();

        private bool _showOnlyMine;
        private bool _isLoading;
        private string? _errorMessage;
        private bool _sortAscending = false;
        private bool _isGalleryOpen = false;

        public IAsyncRelayCommand SortAscendingCommand { get; }
        public IAsyncRelayCommand SortDescendingCommand { get; }
        public IAsyncRelayCommand OpenGalleryCommand { get; }
        public IRelayCommand CloseGalleryCommand { get; }


        public ObservableCollection<MemoryItemViewModel> Memories
        {
            get => _memories;
            private set { _memories = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEmpty)); }
        }

        public ObservableCollection<string> GalleryPhotos
        {
            get => _galleryPhotos;
            private set { _galleryPhotos = value; OnPropertyChanged(); }
        }


        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);
        public bool IsEmpty => !_isLoading && _memories.Count == 0 && !_isGalleryOpen;
        public bool IsMemoryListVisible => !_isGalleryOpen;
        public bool IsGalleryVisible => _isGalleryOpen;

        public bool IsShowOnlyMineChecked
        {
            get => _showOnlyMine;
            private set { _showOnlyMine = value; OnPropertyChanged(); }
        }

        public bool ShowOnlyMine
        {
            get => _showOnlyMine;
            set
            {
                if (_showOnlyMine == value) return;
                _showOnlyMine = value;
                OnPropertyChanged();
                _ = LoadMemoriesAsync();
            }
        }

        public MemoryViewModel(IMemoryService memoryService)
        {
            _memoryService = memoryService;

            SortAscendingCommand = new AsyncRelayCommand(() => SortInternalAsync(ascending: true));
            SortDescendingCommand = new AsyncRelayCommand(() => SortInternalAsync(ascending: false));
            OpenGalleryCommand = new AsyncRelayCommand(OpenGalleryInternalAsync);
            CloseGalleryCommand = new RelayCommand(CloseGalleryInternal);
        }



        public async Task InitializeAsync(Event currentEvent, User currentUser)
        {
            _event = currentEvent;
            _currentUser = currentUser;
            await LoadMemoriesAsync();
        }


        public async Task AddMemoryAsync(string? photoPath, string? text)
        {
            ErrorMessage = null;
            try
            {
                await _memoryService.AddAsync(_event, _currentUser, photoPath, text);
                await LoadMemoriesAsync();
            }
            catch (InvalidOperationException ex) { ErrorMessage = ex.Message; }
            catch (UnauthorizedAccessException ex) { ErrorMessage = ex.Message; }
            catch (Exception ex) { ErrorMessage = $"Could not add memory: {ex.Message}"; }
        }

        public async Task DeleteMemoryAsync(MemoryItemViewModel item)
        {
            ErrorMessage = null;
            try
            {
                await _memoryService.DeleteAsync(item.Memory, _currentUser);
                await LoadMemoriesAsync();
            }
            catch (UnauthorizedAccessException ex) { ErrorMessage = ex.Message; }
            catch (Exception ex) { ErrorMessage = $"Could not delete memory: {ex.Message}"; }
        }

        public async Task ToggleLikeAsync(MemoryItemViewModel item)
        {
            ErrorMessage = null;
            try
            {
                await _memoryService.ToggleLikeAsync(item.Memory, _currentUser);
                await LoadMemoriesAsync();
            }
            catch (InvalidOperationException ex) { ErrorMessage = ex.Message; }
            catch (Exception ex) { ErrorMessage = $"Could not toggle like: {ex.Message}"; }
        }


        public void ResetSortAndFilter()
        {
            _showOnlyMine = false;
            OnPropertyChanged(nameof(ShowOnlyMine));
        }

        private async Task SortInternalAsync(bool ascending)
        {
            _sortAscending = ascending;
            _showOnlyMine = false;
            OnPropertyChanged(nameof(ShowOnlyMine));
            IsShowOnlyMineChecked = false;
            await LoadMemoriesAsync();
        }

        private async Task OpenGalleryInternalAsync()
        {
            ErrorMessage = null;
            try
            {
                var photos = await _memoryService.GetOnlyPhotosAsync(_event);
                GalleryPhotos = new ObservableCollection<string>(photos);
                _isGalleryOpen = true;
                NotifyVisibilityChanged();
            }
            catch (Exception ex) { ErrorMessage = $"Could not load gallery: {ex.Message}"; }
        }

        private void CloseGalleryInternal()
        {
            _isGalleryOpen = false;
            NotifyVisibilityChanged();
        }

        private async Task LoadMemoriesAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                List<Memory> list;

                if (_showOnlyMine)
                    list = await _memoryService.FilterByMyMemoriesAsync(_event, _currentUser);
                else
                    list = await _memoryService.OrderByDateAsync(_event, _currentUser, _sortAscending);

                var items = new ObservableCollection<MemoryItemViewModel>();
                foreach (var m in list)
                    items.Add(new MemoryItemViewModel(
                        m,
                        canDelete: _memoryService.CanDelete(m, _currentUser),
                        canLike: _memoryService.CanLike(m, _currentUser)
                    ));

                Memories = items;
            }
            catch (Exception ex) { ErrorMessage = $"Could not load memories: {ex.Message}"; }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        private void NotifyVisibilityChanged()
        {
            OnPropertyChanged(nameof(IsGalleryVisible));
            OnPropertyChanged(nameof(IsMemoryListVisible));
            OnPropertyChanged(nameof(IsEmpty));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}