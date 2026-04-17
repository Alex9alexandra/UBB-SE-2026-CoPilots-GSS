// <copyright file="MemoryServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Test
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
        private readonly Mock<IMemoryRepository> mockMemoryRepository;
        private readonly Mock<IAttendedEventRepository> mockAttendedEventRepository;
        private readonly Mock<IReputationService> mockReputationService;

        private readonly MemoryService service;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryServiceTests"/> class.
        /// Sets up the mocked dependencies before each test.
        /// </summary>
        public MemoryServiceTests()
        {
            this.mockMemoryRepository = new Mock<IMemoryRepository>();
            this.mockAttendedEventRepository = new Mock<IAttendedEventRepository>();
            this.mockReputationService = new Mock<IReputationService>();

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
            this.mockMemoryRepository.Setup(m => m.GetLikesAsync(memory.MemoryId)).ReturnsAsync(new List<int> { 1 });

            // Act
            await this.service.ToggleLikeAsync(memory, currentUser);

            // Assert
            this.mockMemoryRepository.Verify(m => m.RemoveLikeAsync(memory.MemoryId, currentUser.UserId), Times.Once);
        }
        [Fact]
        public async Task GetByEventAsync_PopulatesIsLikedByCurrentUser()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 10 };

            this.mockMemoryRepository.Setup(r => r.GetByEventAsync(1)).ReturnsAsync(new List<Memory> { memory });
            this.mockMemoryRepository.Setup(r => r.GetLikesAsync(10)).ReturnsAsync(new List<int> { 1 });

            // Act
            var result = await this.service.GetByEventAsync(currentEvent, currentUser);

            // Assert
            Assert.True(result[0].IsLikedByCurrentUser);

        }
        [Fact]
        public async Task GetOnlyPhotosAsync_ReturnsOnlyMemoriesWithPhotos()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var memWithPhoto = new Memory { PhotoPath = "photo.jpg" };
            var memNoPhoto = new Memory { PhotoPath = null };

            this.mockMemoryRepository.Setup(r => r.GetByEventAsync(1)).ReturnsAsync(new List<Memory> { memWithPhoto, memNoPhoto });

            // Act
            var result = await this.service.GetOnlyPhotosAsync(currentEvent);

            // Assert
            Assert.Single(result); // Asserts that exactly 1 item was returned
        }
        [Fact]
        public async Task FilterByMyMemoriesAsync_ReturnsOnlyCurrentUserMemories()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };
            var myMemory = new Memory { MemoryId = 10, Author = new User { UserId = 1 } };
            var otherMemory = new Memory { MemoryId = 11, Author = new User { UserId = 2 } };

            this.mockMemoryRepository.Setup(r => r.GetByEventAsync(1)).ReturnsAsync(new List<Memory> { myMemory, otherMemory });
            this.mockMemoryRepository.Setup(r => r.GetLikesAsync(It.IsAny<int>())).ReturnsAsync(new List<int>());

            // Act
            var result = await this.service.FilterByMyMemoriesAsync(currentEvent, currentUser);

            // Assert
            Assert.Single(result);
        }
        [Fact]
        public async Task OrderByDateAsync_Ascending_ReturnsOldestFirst()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };
            var oldMem = new Memory { MemoryId = 1, CreatedAt = new DateTime(2020, 1, 1) };
            var newMem = new Memory { MemoryId = 2, CreatedAt = new DateTime(2021, 1, 1) };

            this.mockMemoryRepository.Setup(r => r.GetByEventAsync(1)).ReturnsAsync(new List<Memory> { newMem, oldMem });
            this.mockMemoryRepository.Setup(r => r.GetLikesAsync(It.IsAny<int>())).ReturnsAsync(new List<int>());

            // Act
            var result = await this.service.OrderByDateAsync(currentEvent, currentUser, true);

            // Assert
            Assert.Equal(1, result[0].MemoryId); // Asserts the first item is the old memory
        }
        [Fact]
        public async Task AddAsync_NoPhotoAndNoText_ThrowsInvalidOperationException()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(1)).ReturnsAsync(true);
            this.mockAttendedEventRepository.Setup(r => r.GetAsync(1, 1)).ReturnsAsync(new AttendedEvent());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.service.AddAsync(currentEvent, currentUser, null, "   ")); // Pass null photo, empty text
        }

        [Fact]
        public async Task AddAsync_ValidData_CallsRepositoryAddAsync()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(1)).ReturnsAsync(true);
            this.mockAttendedEventRepository.Setup(r => r.GetAsync(1, 1)).ReturnsAsync(new AttendedEvent());

            // Act
            await this.service.AddAsync(currentEvent, currentUser, "photo.jpg", "caption");

            // Assert
            this.mockMemoryRepository.Verify(r => r.AddAsync(It.IsAny<Memory>()), Times.Once); // The Verify is the assert
        }
        [Fact]
        public async Task DeleteAsync_MemoryNotFound_ThrowsException()
        {
            // Arrange
            var memory = new Memory { MemoryId = 99 };
            var currentUser = new User { UserId = 1 };

            this.mockMemoryRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Memory?)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => this.service.DeleteAsync(memory, currentUser));
        }

        [Fact]
        public async Task DeleteAsync_UserIsOwner_CallsRepositoryDeleteAsync()
        {
            // Arrange
            var memory = new Memory { MemoryId = 10 };
            var currentUser = new User { UserId = 1 };

            var fullMemory = new Memory
            {
                Event = new Event { Admin = new User { UserId = 2 } },
                Author = new User { UserId = 1 } // User is the author
            };

            this.mockMemoryRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fullMemory);

            // Act
            await this.service.DeleteAsync(memory, currentUser);

            // Assert
            this.mockMemoryRepository.Verify(r => r.DeleteAsync(10), Times.Once);
        }
        [Fact]
        public async Task GetLikesCountAsync_ReturnsCountFromRepository()
        {
            // Arrange
            this.mockMemoryRepository.Setup(r => r.GetLikesAsync(10)).ReturnsAsync(new List<int> { 1, 2, 3 });

            // Act
            var result = await this.service.GetLikesCountAsync(10);

            // Assert
            Assert.Equal(3, result);
        }
        [Fact]
        public void IsOwnMemory_UserIsAuthor_ReturnsTrue()
        {
            var memory = new Memory { Author = new User { UserId = 1 } };
            var user = new User { UserId = 1 };

            Assert.True(this.service.IsOwnMemory(memory, user));
        }

        [Fact]
        public void CanDelete_UserIsAdmin_ReturnsTrue()
        {
            // User is the admin, but NOT the author
            var memory = new Memory { Event = new Event { Admin = new User { UserId = 1 } }, Author = new User { UserId = 2 } };
            var user = new User { UserId = 1 };

            Assert.True(this.service.CanDelete(memory, user));
        }

        [Fact]
        public void CanDelete_NeitherAdminNorAuthor_ReturnsFalse()
        {
            var memory = new Memory { Event = new Event { Admin = new User { UserId = 2 } }, Author = new User { UserId = 3 } };
            var user = new User { UserId = 1 };

            Assert.False(this.service.CanDelete(memory, user));
        }

        [Fact]
        public void CanLike_UserIsNotAuthor_ReturnsTrue()
        {
            var memory = new Memory { Author = new User { UserId = 2 } };
            var user = new User { UserId = 1 };

            Assert.True(this.service.CanLike(memory, user));
        }
        [Fact]
        public async Task OrderByDateAsync_Descending_ReturnsNewestFirst()
        {
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };
            var oldMem = new Memory { MemoryId = 1, CreatedAt = new DateTime(2020, 1, 1) };
            var newMem = new Memory { MemoryId = 2, CreatedAt = new DateTime(2021, 1, 1) };

            this.mockMemoryRepository.Setup(r => r.GetByEventAsync(1)).ReturnsAsync(new List<Memory> { oldMem, newMem });
            this.mockMemoryRepository.Setup(r => r.GetLikesAsync(It.IsAny<int>())).ReturnsAsync(new List<int>());

            // Pass false to trigger the descending branch
            var result = await this.service.OrderByDateAsync(currentEvent, currentUser, false);

            Assert.Equal(2, result[0].MemoryId);
        }

        [Fact]
        public async Task AddAsync_TextOnly_CallsRepositoryAddAsync()
        {
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(1)).ReturnsAsync(true);
            this.mockAttendedEventRepository.Setup(r => r.GetAsync(1, 1)).ReturnsAsync(new AttendedEvent());

            // Pass null for photoPath to cover the missing ternary branches
            await this.service.AddAsync(currentEvent, currentUser, null, "Just text");

            this.mockMemoryRepository.Verify(r => r.AddAsync(It.IsAny<Memory>()), Times.Once);

        }
        [Fact]
        public async Task DeleteAsync_UserIsAdmin_CallsRepositoryDeleteAsync()
        {
            var memory = new Memory { MemoryId = 10 };
            var currentUser = new User { UserId = 1 };

            var fullMemory = new Memory
            {
                Event = new Event { Admin = new User { UserId = 1 } }, // Matches currentUser
                Author = new User { UserId = 2 }
            };

            this.mockMemoryRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fullMemory);

            await this.service.DeleteAsync(memory, currentUser);

            this.mockMemoryRepository.Verify(r => r.DeleteAsync(10), Times.Once);
        }

        [Fact]
        public async Task ToggleLikeAsync_MemoryNotFound_ThrowsException()
        {
            var currentUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 99 };

            // Force repository to return null
            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(99)).ReturnsAsync((Memory?)null);

            await Assert.ThrowsAsync<Exception>(() => this.service.ToggleLikeAsync(memory, currentUser));
        }
        [Fact]
        public void CanDelete_AuthorAndEventAreNull_ReturnsFalse()
        {
            // Arrange
            var memory = new Memory { Author = null, Event = null };
            var user = new User { UserId = 1 };

            // Act & Assert
            Assert.False(this.service.CanDelete(memory, user));
        }
        [Fact]
        public void CanLike_AuthorIsNull_ReturnsTrue()
        {
            // Arrange
            var memory = new Memory { Author = null };
            var user = new User { UserId = 1 };

            // Act & Assert

            Assert.True(this.service.CanLike(memory, user));
        }
        [Fact]
        public void CanDelete_EventExistsButAdminIsNull_ReturnsFalse()
        {
            // Arrange
            var memory = new Memory
            {
                Author = new User { UserId = 2 }, // User is not the author
                Event = new Event { Admin = null } // Event exists, but Admin is null
            };
            var user = new User { UserId = 1 };

            // Act & Assert
            Assert.False(this.service.CanDelete(memory, user));
        }
        [Fact]
        public async Task AddAsync_PhotoOnly_CallsRepositoryAddAsync()
        {
            // Arrange
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(1)).ReturnsAsync(true);
            this.mockAttendedEventRepository.Setup(r => r.GetAsync(1, 1)).ReturnsAsync(new AttendedEvent());

            // Act
            // Pass a valid photo path, but null for text to trigger the hasText = false branch
            await this.service.AddAsync(currentEvent, currentUser, "photo.jpg", null);

            // Assert
            this.mockMemoryRepository.Verify(r => r.AddAsync(It.IsAny<Memory>()), Times.Once);
        }
    }
}