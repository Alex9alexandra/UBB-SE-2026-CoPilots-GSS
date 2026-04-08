// <copyright file="MemoryViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using CommunityToolkit.Mvvm.Input;

    using Events_GSS.Data.Models;
    using Events_GSS.Data.Services.Interfaces;

    /// <summary>
    /// View model for managing and displaying memories.
    /// </summary>
    public class MemoryViewModel : INotifyPropertyChanged
    {
        private readonly IMemoryService memoryService;

        private Event currentEvent = null!;
        private User currentUser = null!;

        private ObservableCollection<MemoryItemViewModel> memories = new ();
        private ObservableCollection<string> galleryPhotos = new ();

        private bool showOnlyMine;
        private bool isLoading;
        private string? errorMessage;
        private bool sortAscending = false;
        private bool isGalleryOpen = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryViewModel"/> class.
        /// </summary>
        /// <param name="memoryService">The memory service.</param>
        public MemoryViewModel(IMemoryService memoryService)
        {
            this.memoryService = memoryService;

            this.SortAscendingCommand = new AsyncRelayCommand(() => this.SortInternalAsync(ascending: true));
            this.SortDescendingCommand = new AsyncRelayCommand(() => this.SortInternalAsync(ascending: false));
            this.OpenGalleryCommand = new AsyncRelayCommand(this.OpenGalleryInternalAsync);
            this.CloseGalleryCommand = new RelayCommand(this.CloseGalleryInternal);
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Gets the command to sort memories in ascending order.</summary>
        public IAsyncRelayCommand SortAscendingCommand { get; }

        /// <summary>Gets the command to sort memories in descending order.</summary>
        public IAsyncRelayCommand SortDescendingCommand { get; }

        /// <summary>Gets the command to open the photo gallery.</summary>
        public IAsyncRelayCommand OpenGalleryCommand { get; }

        /// <summary>Gets the command to close the photo gallery.</summary>
        public IRelayCommand CloseGalleryCommand { get; }

        /// <summary>
        /// Gets the collection of memory items.
        /// </summary>
        public ObservableCollection<MemoryItemViewModel> Memories
        {
            get => this.memories;
            private set
            {
                this.memories = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.IsEmpty));
            }
        }

        /// <summary>
        /// Gets the collection of gallery photos.
        /// </summary>
        public ObservableCollection<string> GalleryPhotos
        {
            get => this.galleryPhotos;
            private set
            {
                this.galleryPhotos = value;
                this.OnPropertyChanged();
            }
        }

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
                this.OnPropertyChanged(nameof(this.IsEmpty));
            }
        }

        /// <summary>
        /// Gets the error message if an operation failed.
        /// </summary>
        public string? ErrorMessage
        {
            get => this.errorMessage;
            private set
            {
                this.errorMessage = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.HasError));
            }
        }

        /// <summary>Gets a value indicating whether there is an error.</summary>
        public bool HasError => !string.IsNullOrEmpty(this.errorMessage);

        /// <summary>Gets a value indicating whether the memories list is empty.</summary>
        public bool IsEmpty => !this.isLoading && this.memories.Count == 0 && !this.isGalleryOpen;

        /// <summary>Gets a value indicating whether the memory list is visible.</summary>
        public bool IsMemoryListVisible => !this.isGalleryOpen;

        /// <summary>Gets a value indicating whether the gallery is visible.</summary>
        public bool IsGalleryVisible => this.isGalleryOpen;

        /// <summary>
        /// Gets a value indicating whether the "Show Only Mine" filter is currently active.
        /// </summary>
        public bool IsShowOnlyMineChecked
        {
            get => this.showOnlyMine;
            private set
            {
                this.showOnlyMine = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show only the current user's memories.
        /// </summary>
        public bool ShowOnlyMine
        {
            get => this.showOnlyMine;
            set
            {
                if (this.showOnlyMine == value)
                {
                    return;
                }

                this.showOnlyMine = value;
                this.OnPropertyChanged();
                _ = this.LoadMemoriesAsync();
            }
        }

        /// <summary>
        /// Initializes the view model with the current event and user context.
        /// </summary>
        /// <param name="currentEvent">The event to load memories for.</param>
        /// <param name="currentUser">The current user viewing the memories.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync(Event currentEvent, User currentUser)
        {
            this.currentEvent = currentEvent;
            this.currentUser = currentUser;
            await this.LoadMemoriesAsync();
        }

        /// <summary>
        /// Adds a new memory.
        /// </summary>
        /// <param name="photoPath">The optional path to a photo.</param>
        /// <param name="text">The optional text content.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddMemoryAsync(string? photoPath, string? text)
        {
            this.ErrorMessage = null;
            try
            {
                await this.memoryService.AddAsync(this.currentEvent, this.currentUser, photoPath, text);
                await this.LoadMemoriesAsync();
            }
            catch (InvalidOperationException ex)
            {
                this.ErrorMessage = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                this.ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Could not add memory: {ex.Message}";
            }
        }

        /// <summary>
        /// Deletes the specified memory.
        /// </summary>
        /// <param name="item">The memory item to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteMemoryAsync(MemoryItemViewModel item)
        {
            this.ErrorMessage = null;
            try
            {
                await this.memoryService.DeleteAsync(item.Memory, this.currentUser);
                await this.LoadMemoriesAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                this.ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Could not delete memory: {ex.Message}";
            }
        }

        /// <summary>
        /// Toggles the like status of the specified memory.
        /// </summary>
        /// <param name="item">The memory item to like or unlike.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ToggleLikeAsync(MemoryItemViewModel item)
        {
            this.ErrorMessage = null;
            try
            {
                await this.memoryService.ToggleLikeAsync(item.Memory, this.currentUser);
                await this.LoadMemoriesAsync();
            }
            catch (InvalidOperationException ex)
            {
                this.ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Could not toggle like: {ex.Message}";
            }
        }

        /// <summary>
        /// Resets the current sorting and filtering states.
        /// </summary>
        public void ResetSortAndFilter()
        {
            this.showOnlyMine = false;
            this.OnPropertyChanged(nameof(this.ShowOnlyMine));
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async Task SortInternalAsync(bool ascending)
        {
            this.sortAscending = ascending;
            this.showOnlyMine = false;
            this.OnPropertyChanged(nameof(this.ShowOnlyMine));
            this.IsShowOnlyMineChecked = false;
            await this.LoadMemoriesAsync();
        }

        private async Task OpenGalleryInternalAsync()
        {
            this.ErrorMessage = null;
            try
            {
                var photos = await this.memoryService.GetOnlyPhotosAsync(this.currentEvent);
                this.GalleryPhotos = new ObservableCollection<string>(photos);
                this.isGalleryOpen = true;
                this.NotifyVisibilityChanged();
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Could not load gallery: {ex.Message}";
            }
        }

        private void CloseGalleryInternal()
        {
            this.isGalleryOpen = false;
            this.NotifyVisibilityChanged();
        }

        private async Task LoadMemoriesAsync()
        {
            this.IsLoading = true;
            this.ErrorMessage = null;
            try
            {
                List<Memory> memoriesList;

                if (this.showOnlyMine)
                {
                    memoriesList = await this.memoryService.FilterByMyMemoriesAsync(this.currentEvent, this.currentUser);
                }
                else
                {
                    memoriesList = await this.memoryService.OrderByDateAsync(this.currentEvent, this.currentUser, this.sortAscending);
                }

                var items = new ObservableCollection<MemoryItemViewModel>();
                foreach (var memory in memoriesList)
                {
                    items.Add(new MemoryItemViewModel(
                        memory,
                        canDelete: this.memoryService.CanDelete(memory, this.currentUser),
                        canLike: this.memoryService.CanLike(memory, this.currentUser)));
                }

                this.Memories = items;
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Could not load memories: {ex.Message}";
            }
            finally
            {
                this.IsLoading = false;
                this.OnPropertyChanged(nameof(this.IsEmpty));
            }
        }

        private void NotifyVisibilityChanged()
        {
            this.OnPropertyChanged(nameof(this.IsGalleryVisible));
            this.OnPropertyChanged(nameof(this.IsMemoryListVisible));
            this.OnPropertyChanged(nameof(this.IsEmpty));
        }
    }
}