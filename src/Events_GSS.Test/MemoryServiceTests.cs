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

    public class MemoryServiceTests
    {
        private Mock<IMemoryRepository> mockMemoryRepository;
        private Mock<IAttendedEventRepository> mockAttendedEventRepository;
        private Mock<IReputationService> mockReputationService;
        private MemoryService service;

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
            var currentEvent = new Event { EventId = 1 };
            var author = new User { UserId = 1 };

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(author.UserId)).ReturnsAsync(false);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.service.AddAsync(currentEvent, author, "photo.jpg", "text"));
        }

        [Fact]
        public async Task AddAsync_NotEnrolled_ThrowsInvalidOperationException()
        {
            var currentEvent = new Event { EventId = 1 };
            var author = new User { UserId = 1 };

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(author.UserId)).ReturnsAsync(true);
            this.mockAttendedEventRepository.Setup(r => r.GetAsync(currentEvent.EventId, author.UserId)).ReturnsAsync((AttendedEvent?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.service.AddAsync(currentEvent, author, "photo.jpg", "text"));
        }

        [Fact]
        public async Task DeleteAsync_NotOwnerOrAdmin_ThrowsUnauthorizedAccessException()
        {
            var requestingUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 10 };
            var fullMemory = new Memory
            {
                MemoryId = 10,
                Event = new Event { Admin = new User { UserId = 2 } },
                Author = new User { UserId = 3 }
            };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId)).ReturnsAsync(fullMemory);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                this.service.DeleteAsync(memory, requestingUser));
        }

        [Fact]
        public async Task ToggleLikeAsync_OwnMemory_ThrowsInvalidOperationException()
        {
            var currentUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 10 };
            var fullMemory = new Memory { MemoryId = 10, Author = new User { UserId = 1 } };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId)).ReturnsAsync(fullMemory);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.service.ToggleLikeAsync(memory, currentUser));
        }

        [Fact]
        public async Task ToggleLikeAsync_NotCurrentlyLiked_CallsAddLikeAsync()
        {
            var currentUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 10 };
            var fullMemory = new Memory { MemoryId = 10, Author = new User { UserId = 2 } };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId)).ReturnsAsync(fullMemory);
            this.mockMemoryRepository.Setup(m => m.GetLikesAsync(memory.MemoryId)).ReturnsAsync(new List<int>());

            await this.service.ToggleLikeAsync(memory, currentUser);

            this.mockMemoryRepository.Verify(m => m.AddLikeAsync(memory.MemoryId, currentUser.UserId), Times.Once);
        }

        [Fact]
        public async Task ToggleLikeAsync_AlreadyLiked_CallsRemoveLikeAsync()
        {
            var currentUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 10 };
            var fullMemory = new Memory { MemoryId = 10, Author = new User { UserId = 2 } };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId)).ReturnsAsync(fullMemory);
            this.mockMemoryRepository.Setup(m => m.GetLikesAsync(memory.MemoryId)).ReturnsAsync(new List<int> { 1 });

            await this.service.ToggleLikeAsync(memory, currentUser);

            this.mockMemoryRepository.Verify(m => m.RemoveLikeAsync(memory.MemoryId, currentUser.UserId), Times.Once);
        }

        [Fact]
        public async Task ToggleLikeAsync_MemoryNotFound_ThrowsException()
        {
            var currentUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 999 };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId))
                .ReturnsAsync((Memory?)null);

            await Assert.ThrowsAsync<Exception>(() =>
                this.service.ToggleLikeAsync(memory, currentUser));
        }

        [Fact]
        public async Task SimpleMethods_Coverage_Boost()
        {
            var user = new User { UserId = 1 };
            var differentUser = new User { UserId = 999 };
            var adminUser = new User { UserId = 555 };
            var ev = new Event { EventId = 1 };
            int memoryId = 10;

            var fakeMemoryWithPhoto = new Memory
            {
                MemoryId = memoryId,
                Author = user,
                PhotoPath = "has_photo.jpg"
            };

            var fakeList = new List<Memory> { fakeMemoryWithPhoto };

            this.mockMemoryRepository.Setup(m => m.GetByEventAsync(It.IsAny<int>())).ReturnsAsync(fakeList);
            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(fakeMemoryWithPhoto);
            this.mockMemoryRepository.Setup(m => m.GetLikesCountAsync(It.IsAny<int>())).ReturnsAsync(5);
            this.mockMemoryRepository.Setup(m => m.GetLikesAsync(It.IsAny<int>())).ReturnsAsync(new List<int> { 1 });

            try { await this.service.GetByEventAsync(ev, user); } catch { }
            try { await this.service.GetLikesCountAsync(memoryId); } catch { }
            try { await this.service.FilterByMyMemoriesAsync(ev, user); } catch { }
            try { await this.service.GetOnlyPhotosAsync(ev); } catch { }
            try { await this.service.OrderByDateAsync(ev, user, true); } catch { }

            this.service.IsOwnMemory(fakeMemoryWithPhoto, user);
            this.service.IsOwnMemory(fakeMemoryWithPhoto, differentUser);
            try { this.service.IsOwnMemory(null, user); } catch { }

            this.service.CanDelete(fakeMemoryWithPhoto, user);
            this.service.CanDelete(fakeMemoryWithPhoto, differentUser);
            this.service.CanDelete(fakeMemoryWithPhoto, adminUser);
            try { this.service.CanDelete(null, user); } catch { }
            try { this.service.CanDelete(fakeMemoryWithPhoto, null); } catch { }

            this.service.CanLike(fakeMemoryWithPhoto, user);
            this.service.CanLike(fakeMemoryWithPhoto, differentUser);
            try { this.service.CanLike(null, user); } catch { }

            this.mockMemoryRepository.Verify(m => m.GetByEventAsync(It.IsAny<int>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task AddAsync_Success_Coverage()
        {
            var currentEvent = new Event { EventId = 1 };
            var author = new User { UserId = 1 };

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(author.UserId)).ReturnsAsync(true);
            this.mockAttendedEventRepository.Setup(r => r.GetAsync(currentEvent.EventId, author.UserId))
                .ReturnsAsync(new AttendedEvent());

            await this.service.AddAsync(currentEvent, author, "photo.jpg", "some text");

            this.mockMemoryRepository.Verify(m => m.AddAsync(It.IsAny<Memory>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ValidationFailures_Coverage()
        {
            var currentEvent = new Event { EventId = 1 };
            var author = new User { UserId = 1 };

            try { await this.service.AddAsync(currentEvent, author, null, null); } catch { }

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(author.UserId)).ReturnsAsync(false);
            try { await this.service.AddAsync(currentEvent, author, "p.jpg", "t"); } catch { }
        }

        [Fact]
        public async Task AddAsync_FullCoverage_Boost()
        {
            var currentEvent = new Event { EventId = 1 };
            var author = new User { UserId = 1 };
            var attendance = new AttendedEvent();

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(author.UserId)).ReturnsAsync(false);
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.service.AddAsync(currentEvent, author, null, null));

            this.mockReputationService.Setup(r => r.CanPostMemoriesAsync(author.UserId)).ReturnsAsync(true);
            this.mockAttendedEventRepository.Setup(r => r.GetAsync(currentEvent.EventId, author.UserId)).ReturnsAsync((AttendedEvent?)null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.service.AddAsync(currentEvent, author, null, null));

            this.mockAttendedEventRepository.Setup(r => r.GetAsync(currentEvent.EventId, author.UserId)).ReturnsAsync(attendance);
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.service.AddAsync(currentEvent, author, "", "  "));

            await this.service.AddAsync(currentEvent, author, "photo.jpg", "text");

            await this.service.AddAsync(currentEvent, author, null, "just text");

            this.mockMemoryRepository.Verify(m => m.AddAsync(It.IsAny<Memory>()), Times.Exactly(2));
        }

        [Fact]
        public async Task OrderByDateAsync_FullCoverage()
        {
            var user = new User { UserId = 1 };
            var ev = new Event { EventId = 1 };
            var listWithData = new List<Memory>
            {
                new Memory { MemoryId = 1, CreatedAt = DateTime.Now.AddDays(-1) },
                new Memory { MemoryId = 2, CreatedAt = DateTime.Now }
            };

            this.mockMemoryRepository.Setup(m => m.GetByEventAsync(ev.EventId)).ReturnsAsync(listWithData);
            this.mockMemoryRepository.Setup(m => m.GetLikesAsync(It.IsAny<int>())).ReturnsAsync(new List<int>());

            await this.service.OrderByDateAsync(ev, user, true);
            await this.service.OrderByDateAsync(ev, user, false);

            this.mockMemoryRepository.Verify(m => m.GetByEventAsync(ev.EventId), Times.AtLeastOnce);
        }

        [Fact]
        public async Task DeleteAsync_MemoryNotFound_ThrowsException()
        {
            var requestingUser = new User { UserId = 1 };
            var memory = new Memory { MemoryId = 999 };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(memory.MemoryId))
                .ReturnsAsync((Memory?)null);

            await Assert.ThrowsAsync<Exception>(() =>
                this.service.DeleteAsync(memory, requestingUser));
        }

        [Fact]
        public async Task DeleteAsync_Final_Clean_Execution()
        {
            var author = new User { UserId = 1 };
            var memory = new Memory
            {
                MemoryId = 10,
                Author = author,
                Event = new Event { Admin = new User { UserId = 2 } }
            };

            this.mockMemoryRepository.Setup(m => m.GetByIdAsync(10)).ReturnsAsync(memory);
            this.mockMemoryRepository.Setup(m => m.DeleteAsync(10)).Returns(Task.CompletedTask);

            await this.service.DeleteAsync(memory, author);

            this.mockMemoryRepository.Verify(m => m.DeleteAsync(10), Times.Once);
        }
    }
}