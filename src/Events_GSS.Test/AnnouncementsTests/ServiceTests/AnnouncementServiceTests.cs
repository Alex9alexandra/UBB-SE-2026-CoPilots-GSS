using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services;

using Moq;

using Xunit;

namespace Events_GSS.Test.AnnouncementsTests.ServiceTests
{
    public class AnnouncementServiceTests
    {
        private readonly Mock<IAnnouncementRepository> _mockRepo;
        private readonly Mock<IEventRepository> _mockEventRepo;
        private readonly AnnouncementService _service;
        private readonly Event _validEvent;

        public AnnouncementServiceTests()
        {
            _mockRepo = new Mock<IAnnouncementRepository>();
            _mockEventRepo = new Mock<IEventRepository>();

            _service = new AnnouncementService(
                _mockRepo.Object,
                _mockEventRepo.Object);

            _validEvent = new Event
            {
                EventId = 1,
                Admin = new User { UserId = 10, Name = "Admin" }
            };
        }

        private void SetupAdmin(int eventId = 1, int userId = 10)
        {
            _mockEventRepo
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(_validEvent);
        }

        [Fact]
        public async Task CreateAnnouncement_ValidMessage_CallsRepository()
        {
            // Arrange
            SetupAdmin();

            // Act
            await _service.CreateAnnouncementAsync("Hello", 1, 10);

            // Assert
            _mockRepo.Verify(r =>
                r.AddAnnouncementAsync(It.IsAny<Announcement>(), 1, 10),
                Times.Once);
        }

        [Fact]
        public async Task CreateAnnouncement_EmptyMessage_ThrowsException()
        {
            // Arrange
            SetupAdmin();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateAnnouncementAsync("", 1, 10));
        }

        [Fact]
        public async Task CreateAnnouncement_WhitespaceMessage_ThrowsException()
        {
            // Arrange
            SetupAdmin();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateAnnouncementAsync("   ", 1, 10));
        }

        [Fact]
        public async Task CreateAnnouncement_NonAdmin_ThrowsUnauthorized()
        {
            // Arrange
            _mockEventRepo
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Event
                {
                    EventId = 1,
                    Admin = new User { UserId = 999 }
                });

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _service.CreateAnnouncementAsync("Hello", 1, 10));
        }

        [Fact]
        public async Task UpdateAnnouncement_ValidData_CallsUpdate()
        {
            // Arrange
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync(new Announcement(5, "old", DateTime.UtcNow));

            // Act
            await _service.UpdateAnnouncementAsync(5, "new message", 10, 1);

            // Assert
            _mockRepo.Verify(r =>
                r.UpdateAnnouncementAsync(5, "new message"),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAnnouncement_NotFound_ThrowsKeyNotFound()
        {
            // Arrange
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync((Announcement?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _service.UpdateAnnouncementAsync(5, "new", 10, 1));
        }

        [Fact]
        public async Task DeleteAnnouncement_Valid_CallsDelete()
        {
            // Arrange
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync(new Announcement(5, "msg", DateTime.UtcNow));

            // Act
            await _service.DeleteAnnouncementAsync(5, 10, 1);

            // Assert
            _mockRepo.Verify(r => r.DeleteAnnouncementAsync(5), Times.Once);
        }

        [Theory]
        [InlineData("")]

        // empty string
        [InlineData("")]
        // whitespace
        [InlineData("   ")]
        public async Task UpdateAnnouncement_InvalidMessage_ThrowsArgumentException(string input)
        {
            // Arrange
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync(new Announcement(5, "old", DateTime.UtcNow));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.UpdateAnnouncementAsync(5, input, 10, 1));
        }

        [Fact]
        public async Task UpdateAnnouncement_NullMessage_ThrowsArgumentException()
        {
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync(new Announcement(5, "old", DateTime.UtcNow));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateAnnouncementAsync(5, null!, 10, 1));
        }

        [Fact]
        public async Task DeleteAnnouncement_NotFound_Throws()
        {
            // Arrange
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync((Announcement?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _service.DeleteAnnouncementAsync(5, 10, 1));
        }

        [Fact]
        public async Task PinAnnouncement_Valid_UnpinsThenPins()
        {
            // Arrange
            SetupAdmin();

            // Act
            await _service.PinAnnouncementAsync(5, 1, 10);

            // Assert
            _mockRepo.Verify(r => r.UnpinAnnouncementAsync(1), Times.Once);
            _mockRepo.Verify(r => r.PinAsync(5), Times.Once);
        }

        [Fact]
        public async Task MarkAsRead_NotRead_Inserts()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.HasUserReadAsync(5, 10))
                .ReturnsAsync(false);

            // Act
            var result = await _service.MarkAsReadAsync(5, 10);

            // Assert
            Assert.True(result);
            _mockRepo.Verify(r => r.InsertReadReceiptAsync(5, 10), Times.Once);
        }

        [Fact]
        public async Task MarkAsRead_AlreadyRead_DoesNothing()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.HasUserReadAsync(5, 10))
                .ReturnsAsync(true);

            // Act
            var result = await _service.MarkAsReadAsync(5, 10);

            // Assert
            Assert.False(result);
            _mockRepo.Verify(r => r.InsertReadReceiptAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ToggleReaction_SameEmoji_RemovesReaction()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync("👍");

            // Act
            await _service.ToggleReactionAsync(5, 10, "👍");

            // Assert
            _mockRepo.Verify(r => r.RemoveReactionAsync(5, 10), Times.Once);
        }

        [Fact]
        public async Task ToggleReaction_DifferentEmoji_UpdatesReaction()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync("👍");

            // Act
            await _service.ToggleReactionAsync(5, 10, "🔥");

            // Assert
            _mockRepo.Verify(r =>
                r.UpdateReactionAsync(5, 10, "🔥"),
                Times.Once);
        }

        [Fact]
        public async Task ToggleReaction_NoExistingReaction_Inserts()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync((string?)null);

            // Act
            await _service.ToggleReactionAsync(5, 10, "🔥");

            // Assert
            _mockRepo.Verify(r =>
                r.InsertReactionAsync(5, 10, "🔥"),
                Times.Once);
        }

        [Fact]
        public async Task GetReadReceipts_ReturnsData()
        {
            // Arrange
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetReadReceiptsAsync(5))
                .ReturnsAsync(new List<AnnouncementReadReceipt>());

            _mockRepo
                .Setup(r => r.GetTotalParticipantsAsync(1))
                .ReturnsAsync(10);

            // Act
            var (readReceipts, totalParticipants) = await _service.GetReadReceiptsAsync(5, 1, 10);

            // Assert
            Assert.Equal(10, totalParticipants);
        }

        [Fact]
        public async Task GetNonReaders_FiltersCorrectly()
        {
            // Arrange
            var readers = new List<AnnouncementReadReceipt>
            {
                new AnnouncementReadReceipt
                {
                    User = new User { UserId = 1 }
                }
            };

            var users = new List<User>
            {
                new User { UserId = 1 },
                new User { UserId = 2 }
            };

            _mockRepo.Setup(r => r.GetReadReceiptsAsync(5)).ReturnsAsync(readers);
            _mockRepo.Setup(r => r.GetAllParticipantsAsync(1)).ReturnsAsync(users);

            // Act
            var result = await _service.GetNonReadersAsync(5, 1);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].UserId);
        }

        [Fact]
        public async Task GetUnreadCounts_ReturnsRepositoryData()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.GetUnreadCountsForUserAsync(10))
                .ReturnsAsync(new Dictionary<int, int> { { 1, 5 } });

            // Act
            var result = await _service.GetUnreadCountsForUserAsync(10);

            // Assert
            Assert.True(result.ContainsKey(1));
        }

        [Fact]
        public async Task AddOrUpdateReact_FirstTime_InsertsReaction()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync((string?)null);

            // Act
            await _service.AddOrUpdateReactAsync(5, 10, "🔥");

            // Assert
            _mockRepo.Verify(r =>
                r.InsertReactionAsync(5, 10, "🔥"),
                Times.Once);
        }

        [Fact]
        public async Task RemoveReaction_CallsRepository()
        {
            // Act
            await _service.RemoveReactionAsync(5, 10);

            // Assert
            _mockRepo.Verify(r =>
                r.RemoveReactionAsync(5, 10),
                Times.Once);
        }

        [Fact]
        public void AttachReactions_GroupsAndAssignsCorrectly()
        {

            // Arrange
            var announcements = new List<Announcement>
            {
                new Announcement(1, "A1", DateTime.UtcNow)
                {
                    Reactions = new List<AnnouncementReaction>()
                },
                new Announcement(2, "A2", DateTime.UtcNow)
                {
                    Reactions = new List<AnnouncementReaction>()
                }
            };

            var reactions = new List<(int AnnouncementId, AnnouncementReaction Reaction)>
            {
                (1, new AnnouncementReaction
                {
                    Id = 1,
                    Emoji = "👍",
                    AnnouncementId = 1,
                    Author = new User
                    {
                        UserId = 100,
                        Name = "Test User"
                    }
                }),
                (1, new AnnouncementReaction
                {
                    Id = 2,
                    Emoji = "🔥",
                    AnnouncementId = 1,
                    Author = new User
                    {
                        UserId = 101,
                        Name = "Another User"
                    }
                }),
                (2, new AnnouncementReaction
                {
                    Id = 3,
                    Emoji = "❤️",
                    AnnouncementId = 2,
                    Author = new User
                    {
                        UserId = 102,
                        Name = "Third User"
                    }
                })
            };

            var service = new AnnouncementService(
                _mockRepo.Object,
                _mockEventRepo.Object);

            // Act
            service.AttachReactions(announcements, reactions);

            // Assert
            Assert.Equal(2, announcements[0].Reactions.Count);
            Assert.Equal(1, announcements[1].Reactions.Count);
        }

        [Fact]
        public async Task GetAnnouncementsAsync_CallsRepositories()
        {
            // Arrange
            var announcements = new List<Announcement>
    {
        new Announcement(1, "A1", DateTime.UtcNow)
    };

            _mockRepo
                .Setup(r => r.GetAnnouncementsByEventAsync(1, 10))
                .ReturnsAsync(announcements);

            _mockRepo
                .Setup(r => r.GetReactionsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<(int, AnnouncementReaction)>());

            // Act
            await _service.GetAnnouncementsAsync(1, 10);

            // Assert
            _mockRepo.Verify(r => r.GetAnnouncementsByEventAsync(1, 10), Times.Once);
            _mockRepo.Verify(r => r.GetReactionsAsync(It.IsAny<List<int>>()), Times.Once);
        }

        [Fact]
        public async Task GetAnnouncementsAsync_AttachesReactionsCorrectly()
        {
            // Arrange
            var announcements = new List<Announcement>
            {
                new Announcement(1, "A1", DateTime.UtcNow),
                new Announcement(2, "A2", DateTime.UtcNow)
            };

            var reactions = new List<(int AnnouncementId, AnnouncementReaction Reaction)>
            {
                (1, new AnnouncementReaction
                {
                    Id = 1,
                    Emoji = "👍",
                    AnnouncementId = 1,
                    Author = new User { UserId = 1, Name = "User1" }
                }),
                (1, new AnnouncementReaction
                {
                    Id = 2,
                    Emoji = "🔥",
                    AnnouncementId = 1,
                    Author = new User { UserId = 2, Name = "User2" }
                }),
                (2, new AnnouncementReaction
                {
                    Id = 3,
                    Emoji = "❤️",
                    AnnouncementId = 2,
                    Author = new User { UserId = 3, Name = "User3" }
                })
            };

            _mockRepo
                .Setup(r => r.GetAnnouncementsByEventAsync(1, 10))
                .ReturnsAsync(announcements);

            _mockRepo
                .Setup(r => r.GetReactionsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(reactions);

            // Act
            var result = await _service.GetAnnouncementsAsync(1, 10);

            // Assert
            Assert.Equal(2, result.First(a => a.Id == 1).Reactions.Count);
            Assert.Single(result.First(a => a.Id == 2).Reactions);
        }

        [Fact]
        public async Task EnsureAdmin_EventNotFound_ThrowsArgumentException()
        {
            // Arrange
            _mockEventRepo
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateAnnouncementAsync(5, "msg", 10, 1));
        }

        [Fact]
        public async Task EnsureAdmin_UserNotAdmin_ThrowsUnauthorizedAccess()
        {
            // Arrange
            _mockEventRepo
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Event
                {
                    EventId = 1,
                    Admin = new User { UserId = 999, Name = "Other Admin" }
                });

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateAnnouncementAsync(5, "msg", 10, 1));
        }

        [Fact]
        public async Task EnsureAdmin_EventExistsAndUserIsAdmin_DoesNotThrow()
        {
            // Arrange
            _mockEventRepo
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Event
                {
                    EventId = 1,
                    Admin = new User { UserId = 10 }
                });

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync(new Announcement(5, "msg", DateTime.UtcNow)); // ✅ THIS FIX

            // Act
            var exception = await Record.ExceptionAsync(() =>
                _service.UpdateAnnouncementAsync(5, "valid", 10, 1));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task EnsureAdmin_EventHasNoAdmin_ThrowsUnauthorized()
        {
            // Arrange
            _mockEventRepo
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Event
                {
                    EventId = 1,
                    Admin = null // 🔥 important case
                });

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateAnnouncementAsync(5, "valid", 10, 1));
        }

        [Fact]
        public async Task MarkAsRead_AlreadyRead_ReturnsFalse_AndDoesNotInsert()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.HasUserReadAsync(5, 10))
                .ReturnsAsync(true);

            // Act
            var result = await _service.MarkAsReadAsync(5, 10);

            // Assert
            Assert.False(result);

            _mockRepo.Verify(r => r.HasUserReadAsync(5, 10), Times.Once);

            _mockRepo.Verify(r =>
                r.InsertReadReceiptAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task MarkAsRead_NotRead_InsertsAndReturnsTrue()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.HasUserReadAsync(5, 10))
                .ReturnsAsync(false);

            // Act
            var result = await _service.MarkAsReadAsync(5, 10);

            // Assert
            Assert.True(result);

            _mockRepo.Verify(r => r.HasUserReadAsync(5, 10), Times.Once);

            _mockRepo.Verify(r =>
                r.InsertReadReceiptAsync(5, 10),
                Times.Once);
        }

        [Fact]
        public async Task MarkAsReadIfNeeded_AlreadyRead_ReturnsFalse()
        {
            // Act
            var result = await _service.MarkAsReadIfNeededAsync(5, 10, true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MarkAsReadIfNeeded_NotRead_CallsMarkAsRead()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.HasUserReadAsync(5, 10))
                .ReturnsAsync(false);

            // Act
            var result = await _service.MarkAsReadIfNeededAsync(5, 10, false);

            // Assert
            Assert.True(result);

            _mockRepo.Verify(r =>
                r.InsertReadReceiptAsync(5, 10),
                Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateReact_SameEmoji_RemovesReaction()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync("👍");

            // Act
            await _service.AddOrUpdateReactAsync(5, 10, "👍");

            // Assert
            _mockRepo.Verify(r => r.RemoveReactionAsync(5, 10), Times.Once);

            _mockRepo.Verify(r => r.InsertReactionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _mockRepo.Verify(r => r.UpdateReactionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }
    }
 }