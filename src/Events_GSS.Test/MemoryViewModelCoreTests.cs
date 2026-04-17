// <copyright file="MemoryViewModelCoreTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Events_GSS.Data.Models;
    using Events_GSS.Data.Services.Interfaces;
    using Events_GSS.Data.ViewModels;

    using Moq;

    using Xunit;

    /// <summary>
    /// Unit tests for the MemoryViewModelCore.
    /// </summary>
    public class MemoryViewModelCoreTests
    {
        private readonly Mock<IMemoryService> mockMemoryService;
        private readonly MemoryViewModelCore viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryViewModelCoreTests"/> class.
        /// Sets up the mocked dependencies.
        /// </summary>
        public MemoryViewModelCoreTests()
        {
            this.mockMemoryService = new Mock<IMemoryService>();

            this.mockMemoryService
                .Setup(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Memory>());

            this.mockMemoryService
                .Setup(s => s.FilterByMyMemoriesAsync(It.IsAny<Event>(), It.IsAny<User>()))
                .ReturnsAsync(new List<Memory>());

            this.viewModel = new MemoryViewModelCore(this.mockMemoryService.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsMemories_SetsMemoriesProperty()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };

            var fakeMemories = new List<Memory>
            {
                new Memory { MemoryId = 1, Author = currentUser, Event = currentEvent }
            };

            this.mockMemoryService
                .Setup(s => s.OrderByDateAsync(currentEvent, currentUser, false))
                .ReturnsAsync(fakeMemories);

            // Act
            await this.viewModel.InitializeAsync(currentEvent, currentUser);

            // Assert
            Assert.Single(this.viewModel.Memories); // Equivalent to asserting the count is 1
            Assert.False(this.viewModel.IsLoading);
        }

        [Fact]
        public async Task AddMemoryAsync_ServiceThrowsException_SetsErrorMessage()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };
            await this.viewModel.InitializeAsync(currentEvent, currentUser);

            this.mockMemoryService
                .Setup(s => s.AddAsync(currentEvent, currentUser, "photo.jpg", "test"))
                .ThrowsAsync(new InvalidOperationException("Reputation too low"));

            // Act
            await this.viewModel.AddMemoryAsync("photo.jpg", "test");

            // Assert
            Assert.Equal("Reputation too low", this.viewModel.ErrorMessage);
            Assert.True(this.viewModel.HasError);
        }

        [Fact]
        public async Task ShowOnlyMine_ToggledTrue_CallsFilterService()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };
            await this.viewModel.InitializeAsync(currentEvent, currentUser);

            // Act
            this.viewModel.ShowOnlyMine = true;
            await Task.Delay(50); // Allows UI property changed events to fire

            // Assert
            this.mockMemoryService.Verify(s => s.FilterByMyMemoriesAsync(currentEvent, currentUser), Times.Once);
        }
        [Fact]
        public async Task OpenGalleryCommand_Executes_SetsGalleryVisibleToTrue()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            this.mockMemoryService.Setup(s => s.GetOnlyPhotosAsync(It.IsAny<Event>())).ReturnsAsync(new List<string>());

            // Act
            await this.viewModel.OpenGalleryCommand.ExecuteAsync(null);

            // Assert
            Assert.True(this.viewModel.IsGalleryVisible);
        }

        [Fact]
        public async Task CloseGalleryCommand_Executes_SetsGalleryVisibleToFalse()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            await this.viewModel.OpenGalleryCommand.ExecuteAsync(null); // Open it first

            // Act
            this.viewModel.CloseGalleryCommand.Execute(null);

            // Assert
            Assert.False(this.viewModel.IsGalleryVisible);
        }

        [Fact]
        public async Task OpenGalleryCommand_Executes_PopulatesGalleryPhotos()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            var photos = new List<string> { "photo1.jpg", "photo2.jpg" };
            this.mockMemoryService.Setup(s => s.GetOnlyPhotosAsync(It.IsAny<Event>())).ReturnsAsync(photos);

            // Act
            await this.viewModel.OpenGalleryCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal(2, this.viewModel.GalleryPhotos.Count);
        }
        [Fact]
        public void ShowOnlyMine_SetToSameValue_DoesNotTriggerPropertyChange()
        {
            // Arrange
            var eventFired = false;
            this.viewModel.PropertyChanged += (sender, args) => eventFired = true;

            // Act
            this.viewModel.ShowOnlyMine = false; // It is already false by default

            // Assert
            Assert.False(eventFired); // The early 'return;' stops the event from firing
        }

        [Fact]
        public void ResetSortAndFilter_SetsShowOnlyMineToFalse()
        {
            // Arrange
            this.viewModel.ShowOnlyMine = true;

            // Act
            this.viewModel.ResetSortAndFilter();

            // Assert
            Assert.False(this.viewModel.ShowOnlyMine);
        }
        [Fact]
        public async Task AddMemoryAsync_ThrowsUnauthorizedAccessException_SetsErrorMessage()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            this.mockMemoryService.Setup(s => s.AddAsync(It.IsAny<Event>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new UnauthorizedAccessException("Not allowed to post"));

            // Act
            await this.viewModel.AddMemoryAsync("photo", "text");

            // Assert
            Assert.Equal("Not allowed to post", this.viewModel.ErrorMessage);
        }

        [Fact]
        public async Task DeleteMemoryAsync_ValidItem_CallsServiceDeleteAsync()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            var memoryItem = new MemoryItemViewModel(new Memory(), true, true);

            // Act
            await this.viewModel.DeleteMemoryAsync(memoryItem);

            // Assert
            this.mockMemoryService.Verify(s => s.DeleteAsync(memoryItem.Memory, It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task ToggleLikeAsync_ServiceThrowsGeneralException_SetsErrorMessage()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            var memoryItem = new MemoryItemViewModel(new Memory(), true, true);
            this.mockMemoryService.Setup(s => s.ToggleLikeAsync(It.IsAny<Memory>(), It.IsAny<User>()))
                .ThrowsAsync(new Exception("Network error"));

            // Act
            await this.viewModel.ToggleLikeAsync(memoryItem);

            // Assert
            Assert.Equal("Could not toggle like: Network error", this.viewModel.ErrorMessage);
        }

        [Fact]
        public async Task InitializeAsync_ServiceThrowsException_SetsLoadErrorMessage()
        {
            // Arrange
            this.mockMemoryService.Setup(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Database down"));

            // Act
            await this.viewModel.InitializeAsync(new Event(), new User());

            // Assert
            Assert.Equal("Could not load memories: Database down", this.viewModel.ErrorMessage);
        }
        [Fact]
        public void OnPropertyChanged_WithSubscriber_FiresEvent()
        {
            // Arrange
            bool wasFired = false;
            this.viewModel.PropertyChanged += (sender, args) => wasFired = true;

            // Act
            // Triggering any method that calls OnPropertyChanged
            this.viewModel.ResetSortAndFilter();

            // Assert
            Assert.True(wasFired);
        }
        [Fact]
        public async Task IsEmpty_WithNoMemories_ReturnsTrue()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());

            // Act & Assert
            Assert.True(this.viewModel.IsEmpty);
        }

        [Fact]
        public void IsMemoryListVisible_Initially_ReturnsTrue()
        {
            // Act & Assert
            Assert.True(this.viewModel.IsMemoryListVisible);
        }
        [Fact]
        public async Task SortAscendingCommand_Executes_CallsOrderByDateAsyncWithTrue()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());

            // Act
            await this.viewModel.SortAscendingCommand.ExecuteAsync(null);

            // Assert
            this.mockMemoryService.Verify(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), true), Times.Once);
        }

        [Fact]
        public async Task SortDescendingCommand_Executes_CallsOrderByDateAsyncWithFalse()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());

            // Act
            await this.viewModel.SortDescendingCommand.ExecuteAsync(null);

            // Assert
            // It gets called once during Initialize, and once during this command, so we check for exactly 2 times
            this.mockMemoryService.Verify(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), false), Times.Exactly(2));
        }
        [Fact]
        public async Task ShowOnlyMine_SetToTrue_CallsFilterService()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());

            // Act
            this.viewModel.ShowOnlyMine = true;
            await Task.Delay(50); // Give the async property setter time to run

            // Assert
            this.mockMemoryService.Verify(s => s.FilterByMyMemoriesAsync(It.IsAny<Event>(), It.IsAny<User>()), Times.Once);
        }
        [Fact]
        public async Task AddMemoryAsync_ValidInput_ReloadsMemories()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());

            // Act
            await this.viewModel.AddMemoryAsync("photo.jpg", "text");

            // Assert
            // Initialize calls OrderByDate once. A successful Add calls it a second time.
            this.mockMemoryService.Verify(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), false), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteMemoryAsync_ValidInput_ReloadsMemories()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            var memoryItem = new MemoryItemViewModel(new Memory(), true, true);

            // Act
            await this.viewModel.DeleteMemoryAsync(memoryItem);

            // Assert
            this.mockMemoryService.Verify(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), false), Times.Exactly(2));
        }

        [Fact]
        public async Task ToggleLikeAsync_ValidInput_ReloadsMemories()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            var memoryItem = new MemoryItemViewModel(new Memory(), true, true);

            // Act
            await this.viewModel.ToggleLikeAsync(memoryItem);

            // Assert
            this.mockMemoryService.Verify(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), false), Times.Exactly(2));
        }
        [Fact]
        public async Task AddMemoryAsync_ThrowsGeneralException_SetsErrorMessage()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            this.mockMemoryService.Setup(s => s.AddAsync(It.IsAny<Event>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database Error"));

            // Act
            await this.viewModel.AddMemoryAsync("photo.jpg", "text");

            // Assert
            Assert.Equal("Could not add memory: Database Error", this.viewModel.ErrorMessage);
        }

        [Fact]
        public async Task DeleteMemoryAsync_ThrowsUnauthorizedAccessException_SetsErrorMessage()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            var memoryItem = new MemoryItemViewModel(new Memory(), true, true);
            this.mockMemoryService.Setup(s => s.DeleteAsync(It.IsAny<Memory>(), It.IsAny<User>()))
                .ThrowsAsync(new UnauthorizedAccessException("Not Admin"));

            // Act
            await this.viewModel.DeleteMemoryAsync(memoryItem);

            // Assert
            Assert.Equal("Not Admin", this.viewModel.ErrorMessage);
        }

        [Fact]
        public async Task DeleteMemoryAsync_ThrowsGeneralException_SetsErrorMessage()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            var memoryItem = new MemoryItemViewModel(new Memory(), true, true);
            this.mockMemoryService.Setup(s => s.DeleteAsync(It.IsAny<Memory>(), It.IsAny<User>()))
                .ThrowsAsync(new Exception("Database Error"));

            // Act
            await this.viewModel.DeleteMemoryAsync(memoryItem);

            // Assert
            Assert.Equal("Could not delete memory: Database Error", this.viewModel.ErrorMessage);
        }

        [Fact]
        public async Task ToggleLikeAsync_ThrowsInvalidOperationException_SetsErrorMessage()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            var memoryItem = new MemoryItemViewModel(new Memory(), true, true);
            this.mockMemoryService.Setup(s => s.ToggleLikeAsync(It.IsAny<Memory>(), It.IsAny<User>()))
                .ThrowsAsync(new InvalidOperationException("Cannot like own memory"));

            // Act
            await this.viewModel.ToggleLikeAsync(memoryItem);

            // Assert
            Assert.Equal("Cannot like own memory", this.viewModel.ErrorMessage);
        }
        [Fact]
        public void IsShowOnlyMineChecked_Initially_ReturnsFalse()
        {
            // Act & Assert
            // Simply reading the property turns the red getter green
            Assert.False(this.viewModel.IsShowOnlyMineChecked);
        }

        [Fact]
        public async Task IsEmpty_WhenGalleryIsOpen_ReturnsFalse()
        {
            // Arrange
            await this.viewModel.InitializeAsync(new Event(), new User());
            this.mockMemoryService.Setup(s => s.GetOnlyPhotosAsync(It.IsAny<Event>())).ReturnsAsync(new List<string>());

            // Act: Open the gallery so !this.isGalleryOpen becomes false
            await this.viewModel.OpenGalleryCommand.ExecuteAsync(null);

            // Assert
            Assert.False(this.viewModel.IsEmpty);
        }
        [Fact]
        public async Task IsEmpty_WithMemories_ReturnsFalse()
        {
            // Arrange
            var fakeMemories = new List<Memory> { new Memory() }; // A list with 1 item
            this.mockMemoryService.Setup(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), It.IsAny<bool>()))
                .ReturnsAsync(fakeMemories);

            await this.viewModel.InitializeAsync(new Event(), new User());

            // Act & Assert
            Assert.False(this.viewModel.IsEmpty);
        }
        [Fact]
        public void IsEmpty_WhileLoading_ReturnsFalse()
        {
            // Arrange
            // Create a task that is "pending" and never completes
            var pendingTask = new TaskCompletionSource<List<Memory>>();
            this.mockMemoryService.Setup(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), It.IsAny<bool>()))
                .Returns(pendingTask.Task);

            // Act
            // Call Initialize but DO NOT await it. It will get stuck in the loading phase.
            _ = this.viewModel.InitializeAsync(new Event(), new User());

            // Assert
            // Because it is stuck loading, IsLoading is true, making IsEmpty false
            Assert.False(this.viewModel.IsEmpty);
        }
    }
}