// <copyright file="MemoryServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Events_GSS.Data.Models;
    using Events_GSS.Data.Repositories;
    using Events_GSS.Data.Services;
    using Events_GSS.Data.Services.reputationService;

    using Moq;

    using Xunit;

    /// <summary>
    /// Unit tests for the MemoryService.
    /// </summary>
    public class MemoryServiceTests
    {
        private Mock<IMemoryRepository> mockMemoryRepository;
        private Mock<IAttendedEventRepository> mockAttendedEventRepository;
        private Mock<IReputationService> mockReputationService;

        private MemoryService service;

        /// <summary>
        /// Sets up the mocked dependencies before each test.
        /// </summary>
        public MemoryServiceTests()
        {
            this.mockMemoryRepository = new Mock<IMemoryRepository>();
            this.mockAttendedEventRepository = new Mock<IAttendedEventRepository>();
            this.mockReputationService = new Mock<IReputationService>();

            // ONLY PASS 3 ARGUMENTS HERE!
            this.service = new MemoryService(
                this.mockMemoryRepository.Object,
                this.mockAttendedEventRepository.Object,
                this.mockReputationService.Object);
        }

        [Fact]
        public async Task AddAsync_LowReputation_ThrowsInvalidOperationException()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var author = new User { UserId = 1 };

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(author.UserId)).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.service.AddAsync(currentEvent, author, "photo.jpg", "text"));
        }

        [Fact]
        public async Task AddAsync_NotEnrolled_ThrowsInvalidOperationException()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var author = new User { UserId = 1 };

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(author.UserId)).ReturnsAsync(true);
            this.mockAttendedEventRepository.Setup(r => r.GetAsync(currentEvent.EventId, author.UserId)).ReturnsAsync((AttendedEvent?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.service.AddAsync(currentEvent, author, "photo.jpg", "text"));
        }

        [Fact]
        public async Task DeleteAsync_NotOwnerOrAdmin_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var requestingUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 10 };

            var fullMemory = new Memory
            {
                MemoryId = 10,
                Event = new Event { Admin = new User { UserId = 2 } },
                Author = new User { UserId = 3 }
            };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId)).ReturnsAsync(fullMemory);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                this.service.DeleteAsync(memory, requestingUser));
        }

        [Fact]
        public async Task ToggleLikeAsync_OwnMemory_ThrowsInvalidOperationException()
        {
            // Arrange
            var currentUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 10 };

            var fullMemory = new Memory { MemoryId = 10, Author = new User { UserId = 1 } };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId)).ReturnsAsync(fullMemory);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.service.ToggleLikeAsync(memory, currentUser));
        }

        [Fact]
        public async Task ToggleLikeAsync_NotCurrentlyLiked_CallsAddLikeAsync()
        {
            // Arrange
            var currentUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 10 };

            var fullMemory = new Memory { MemoryId = 10, Author = new User { UserId = 2 } };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId)).ReturnsAsync(fullMemory);

            // NEW MOCK: Return an empty list so the service thinks we haven't liked it yet
            this.mockMemoryRepository.Setup(m => m.GetLikesAsync(memory.MemoryId)).ReturnsAsync(new List<int>());

            // Act
            await this.service.ToggleLikeAsync(memory, currentUser);

            // Assert
            this.mockMemoryRepository.Verify(m => m.AddLikeAsync(memory.MemoryId, currentUser.UserId), Times.Once);
        }

        [Fact]
        public async Task ToggleLikeAsync_AlreadyLiked_CallsRemoveLikeAsync()
        {
            // Arrange
            var currentUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 10 };

            var fullMemory = new Memory { MemoryId = 10, Author = new User { UserId = 2 } };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId)).ReturnsAsync(fullMemory);

            // NEW MOCK: Return a list containing our User ID, so the service thinks we already liked it
            this.mockMemoryRepository.Setup(m => m.GetLikesAsync(memory.MemoryId)).ReturnsAsync(new List<int> { 1 });

            // Act
            await this.service.ToggleLikeAsync(memory, currentUser);

            // Assert
            this.mockMemoryRepository.Verify(m => m.RemoveLikeAsync(memory.MemoryId, currentUser.UserId), Times.Once);
        }
    }
}