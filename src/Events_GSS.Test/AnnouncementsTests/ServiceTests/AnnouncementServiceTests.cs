using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services;

using Moq;

using NSubstitute;

using Xunit;

namespace Events_GSS.Test.AnnouncementsTests.ServiceTests
{
    public class AnnouncementServiceTests
    {
        private const int EventId = 1;
        private const int UserId = 10;
        private const int AnnouncementId = 5;

        private readonly Mock<IAnnouncementRepository> repoMock;
        private readonly Mock<IEventRepository> eventRepoMock;
        private readonly AnnouncementService service;

        public AnnouncementServiceTests()
        {
            repoMock = new Mock<IAnnouncementRepository>(MockBehavior.Strict);
            eventRepoMock = new Mock<IEventRepository>(MockBehavior.Strict);

            service = new AnnouncementService(repoMock.Object, eventRepoMock.Object);
        }

        private void SetupAdmin()
        {
            eventRepoMock
                .Setup(r => r.GetByIdAsync(EventId))
                .ReturnsAsync(new Event
                {
                    EventId = EventId,
                    Admin = new User { UserId = UserId }
                });
        }

        [Fact]
        public async Task CreateAnnouncement_Valid_CallsRepository()
        {
            SetupAdmin();

            repoMock
                .Setup(r => r.AddAnnouncementAsync(It.IsAny<Announcement>(), EventId, UserId))
                .ReturnsAsync(1);

            await service.CreateAnnouncementAsync("hello", EventId, UserId);

            repoMock.Verify(r =>
                r.AddAnnouncementAsync(It.IsAny<Announcement>(), EventId, UserId),
                Times.Once);
        }

        [Fact]
        public async Task CreateAnnouncement_Empty_Throws()
        {
            SetupAdmin();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateAnnouncementAsync("", EventId, UserId));
        }

        [Fact]
        public async Task CreateAnnouncement_NotAdmin_Throws()
        {
            eventRepoMock
                .Setup(r => r.GetByIdAsync(EventId))
                .ReturnsAsync(new Event { Admin = new User { UserId = 999 } });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.CreateAnnouncementAsync("msg", EventId, UserId));
        }

        [Fact]
        public async Task UpdateAnnouncement_Valid_CallsRepository()
        {
            SetupAdmin();

            repoMock
                .Setup(r => r.GetAnnouncementByIdAsync(AnnouncementId))
                .ReturnsAsync(new Announcement(AnnouncementId, "old", DateTime.UtcNow));

            repoMock
                .Setup(r => r.UpdateAnnouncementAsync(AnnouncementId, "new"))
                .Returns(Task.CompletedTask);

            await service.UpdateAnnouncementAsync(AnnouncementId, "new", UserId, EventId);

            repoMock.Verify(r =>
                r.UpdateAnnouncementAsync(AnnouncementId, "new"),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAnnouncement_NotFound_Throws()
        {
            SetupAdmin();

            repoMock
                .Setup(r => r.GetAnnouncementByIdAsync(AnnouncementId))
                .ReturnsAsync((Announcement?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateAnnouncementAsync(AnnouncementId, "new", UserId, EventId));
        }

        [Fact]
        public async Task DeleteAnnouncement_Valid_CallsRepository()
        {
            SetupAdmin();

            repoMock
                .Setup(r => r.GetAnnouncementByIdAsync(AnnouncementId))
                .ReturnsAsync(new Announcement(AnnouncementId, "msg", DateTime.UtcNow));

            repoMock
                .Setup(r => r.DeleteAnnouncementAsync(AnnouncementId))
                .Returns(Task.CompletedTask);

            await service.DeleteAnnouncementAsync(AnnouncementId, UserId, EventId);

            repoMock.Verify(r =>
                r.DeleteAnnouncementAsync(AnnouncementId),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAnnouncement_NotFound_Throws()
        {
            SetupAdmin();

            repoMock
                .Setup(r => r.GetAnnouncementByIdAsync(AnnouncementId))
                .ReturnsAsync((Announcement?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.DeleteAnnouncementAsync(AnnouncementId, UserId, EventId));
        }

        [Fact]
        public async Task PinAnnouncement_CallsUnpin()
        {
            SetupAdmin();

            repoMock
                .Setup(r => r.UnpinAnnouncementAsync(EventId))
                .Returns(Task.CompletedTask);

            repoMock
                .Setup(r => r.PinAsync(AnnouncementId))
                .Returns(Task.CompletedTask);

            await service.PinAnnouncementAsync(AnnouncementId, EventId, UserId);

            repoMock.Verify(r =>
                r.UnpinAnnouncementAsync(EventId),
                Times.Once);
        }

        [Fact]
        public async Task PinAnnouncement_CallsPin()
        {
            SetupAdmin();

            repoMock.Setup(r => r.UnpinAnnouncementAsync(EventId)).Returns(Task.CompletedTask);
            repoMock.Setup(r => r.PinAsync(AnnouncementId)).Returns(Task.CompletedTask);

            await service.PinAnnouncementAsync(AnnouncementId, EventId, UserId);

            repoMock.Verify(r =>
                r.PinAsync(AnnouncementId),
                Times.Once);
        }

        [Fact]
        public async Task MarkAsRead_NotRead_ReturnsTrue()
        {
            repoMock
                .Setup(r => r.HasUserReadAsync(AnnouncementId, UserId))
                .ReturnsAsync(false);

            repoMock
                .Setup(r => r.InsertReadReceiptAsync(AnnouncementId, UserId))
                .Returns(Task.CompletedTask);

            var result = await service.MarkAsReadAsync(AnnouncementId, UserId);

            Assert.True(result);
        }

        [Fact]
        public async Task MarkAsRead_AlreadyRead_ReturnsFalse()
        {
            repoMock
                .Setup(r => r.HasUserReadAsync(AnnouncementId, UserId))
                .ReturnsAsync(true);

            var result = await service.MarkAsReadAsync(AnnouncementId, UserId);

            Assert.False(result);
        }

        [Fact]
        public async Task ToggleReaction_SameEmoji_Removes()
        {
            repoMock
                .Setup(r => r.GetUserReactionAsync(AnnouncementId, UserId))
                .ReturnsAsync("👍");

            repoMock
                .Setup(r => r.RemoveReactionAsync(AnnouncementId, UserId))
                .Returns(Task.CompletedTask);

            await service.ToggleReactionAsync(AnnouncementId, UserId, "👍");

            repoMock.Verify(r =>
                r.RemoveReactionAsync(AnnouncementId, UserId),
                Times.Once);
        }

        [Fact]
        public void Constructor_NullAnnouncementRepository_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new AnnouncementService(null!, eventRepoMock.Object));

            Assert.Equal("announcementRepository", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullEventRepository_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new AnnouncementService(repoMock.Object, null!));

            Assert.Equal("eventRepository", ex.ParamName);
        }

        [Fact]
        public async Task ToggleReaction_NewReaction_Inserts()
        {
            repoMock
                .Setup(r => r.GetUserReactionAsync(AnnouncementId, UserId))
                .ReturnsAsync((string?)null);

            repoMock
                .Setup(r => r.InsertReactionAsync(AnnouncementId, UserId, "🔥"))
                .Returns(Task.CompletedTask);

            await service.ToggleReactionAsync(AnnouncementId, UserId, "🔥");

            repoMock.Verify(r =>
                r.InsertReactionAsync(AnnouncementId, UserId, "🔥"),
                Times.Once);
        }

        [Fact]
        public async Task GetAnnouncements_CallsRepo()
        {
            repoMock
                .Setup(r => r.GetAnnouncementsByEventAsync(EventId, UserId))
                .ReturnsAsync(new List<Announcement>());

            repoMock
                .Setup(r => r.GetReactionsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<(int, AnnouncementReaction)>());

            await service.GetAnnouncementsAsync(EventId, UserId);

            repoMock.Verify(r =>
                r.GetAnnouncementsByEventAsync(EventId, UserId),
                Times.Once);
        }

        [Fact]
        public async Task GetReadReceipts_ReturnsTotal()
        {
            SetupAdmin();

            repoMock
                .Setup(r => r.GetReadReceiptsAsync(AnnouncementId))
                .ReturnsAsync(new List<AnnouncementReadReceipt>());

            repoMock
                .Setup(r => r.GetTotalParticipantsAsync(EventId))
                .ReturnsAsync(10);

            var result = await service.GetReadReceiptsAsync(AnnouncementId, EventId, UserId);

            Assert.Equal(10, result.TotalParticipants);
        }

        [Fact]
        public async Task EnsureAdmin_EventMissing_Throws()
        {
            eventRepoMock
                .Setup(r => r.GetByIdAsync(EventId))
                .ReturnsAsync((Event?)null);

            repoMock
                .Setup(r => r.GetAnnouncementByIdAsync(AnnouncementId))
                .ReturnsAsync(new Announcement(AnnouncementId, "msg", DateTime.UtcNow));

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.DeleteAnnouncementAsync(AnnouncementId, UserId, EventId));

            Assert.Equal($"Event with ID {EventId} does not exist.", ex.Message);
        }

        [Fact]
        public async Task EnsureAdmin_UserNotAdmin_Throws()
        {
            eventRepoMock
                .Setup(r => r.GetByIdAsync(EventId))
                .ReturnsAsync(new Event
                {
                    Admin = new User { UserId = 999 }
                });

            repoMock
                .Setup(r => r.GetAnnouncementByIdAsync(AnnouncementId))
                .ReturnsAsync(new Announcement(AnnouncementId, "msg", DateTime.UtcNow));

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.DeleteAnnouncementAsync(AnnouncementId, UserId, EventId));

            Assert.Equal("Only the EventAdmin can perform this action.", ex.Message);
        }

        [Fact]
        public async Task EnsureAdmin_AdminIsNull_ThrowsUnauthorized()
        {
            eventRepoMock
                .Setup(r => r.GetByIdAsync(EventId))
                .ReturnsAsync(new Event
                {
                    Admin = null
                });

            repoMock
                .Setup(r => r.GetAnnouncementByIdAsync(AnnouncementId))
                .ReturnsAsync(new Announcement(AnnouncementId, "msg", DateTime.UtcNow));

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.DeleteAnnouncementAsync(AnnouncementId, UserId, EventId));

            Assert.Equal("Only the EventAdmin can perform this action.", ex.Message);
        }

        [Fact]
        public async Task GetAnnouncementsAsync_PassesCorrectIdsToReactions()
        {
            var announcements = new List<Announcement>
            {
                new Announcement(1, "A1", DateTime.UtcNow),
                new Announcement(2, "A2", DateTime.UtcNow)
            };

            repoMock
                .Setup(r => r.GetAnnouncementsByEventAsync(EventId, UserId))
                .ReturnsAsync(announcements);

            List<int>? capturedIds = null;

            repoMock
                .Setup(r => r.GetReactionsAsync(It.IsAny<List<int>>()))
                .Callback<List<int>>(ids => capturedIds = ids)
                .ReturnsAsync(new List<(int, AnnouncementReaction)>());

            await service.GetAnnouncementsAsync(EventId, UserId);

            Assert.Equal(new List<int> { 1, 2 }, capturedIds);
        }

        [Fact]
        public async Task UpdateAnnouncement_EmptyMessage_ThrowsArgumentException()
        {
            SetupAdmin();

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UpdateAnnouncementAsync(1, "", 10, 1));

            Assert.Equal("Announcement message cannot be empty.", exception.Message);
        }

        [Fact]
        public async Task UpdateAnnouncement_WhitespaceMessage_ThrowsArgumentException()
        {
            SetupAdmin();

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UpdateAnnouncementAsync(1, "   ", 10, 1));

            Assert.Equal("Announcement message cannot be empty.", exception.Message);
        }

        [Fact]
        public async Task UpdateAnnouncement_NullMessage_ThrowsArgumentException()
        {
            SetupAdmin();

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UpdateAnnouncementAsync(1, null!, 10, 1));

            Assert.Equal("Announcement message cannot be empty.", exception.Message);
        }

        [Fact]
        public async Task AddOrUpdateReact_SameEmoji_RemovesReaction()
        {
            repoMock
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync("👍");

            repoMock
                .Setup(r => r.RemoveReactionAsync(5, 10))
                .Returns(Task.CompletedTask);

            await service.AddOrUpdateReactAsync(5, 10, "👍");

            repoMock.Verify(r =>
                r.RemoveReactionAsync(5, 10),
                Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateReact_DifferentEmoji_UpdatesReaction()
        {
            repoMock
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync("👍");

            repoMock
                .Setup(r => r.UpdateReactionAsync(5, 10, "🔥"))
                .Returns(Task.CompletedTask);

            await service.AddOrUpdateReactAsync(5, 10, "🔥");

            repoMock.Verify(r =>
                r.UpdateReactionAsync(5, 10, "🔥"),
                Times.Once);
        }

        [Fact]
        public async Task RemoveReaction_CallsRepository()
        {
            repoMock
                .Setup(r => r.RemoveReactionAsync(5, 10))
                .Returns(Task.CompletedTask);

            await service.RemoveReactionAsync(5, 10);

            repoMock.Verify(r =>
                r.RemoveReactionAsync(5, 10),
                Times.Once);
        }

        [Fact]
        public async Task GetUnreadCounts_ReturnsRepositoryData()
        {
            var expected = new Dictionary<int, int>{{ 1, 5 }};

            repoMock
                .Setup(r => r.GetUnreadCountsForUserAsync(10))
                .ReturnsAsync(expected);

            var result = await service.GetUnreadCountsForUserAsync(10);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task UpdateAnnouncementAsync_EmptyMessage_ThrowsArgumentException()
        {
            SetupAdmin();

            repoMock
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync(new Announcement(5, "old", DateTime.UtcNow));

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UpdateAnnouncementAsync(5, "", 10, 1));

            Assert.Equal("Announcement message cannot be empty.", ex.Message);
        }

        [Fact]
        public async Task AddOrUpdateReactAsync_NoExistingReaction_InsertsReaction()
        {
            repoMock
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync((string?)null);

            repoMock
                .Setup(r => r.InsertReactionAsync(5, 10, "🔥"))
                .Returns(Task.CompletedTask);

            await service.AddOrUpdateReactAsync(5, 10, "🔥");

            repoMock.Verify(r =>
                r.InsertReactionAsync(5, 10, "🔥"),
                Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateReactAsync_SameEmoji_RemovesReaction()
        {
            repoMock
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync("🔥");

            repoMock
                .Setup(r => r.RemoveReactionAsync(5, 10))
                .Returns(Task.CompletedTask);

            await service.AddOrUpdateReactAsync(5, 10, "🔥");

            repoMock.Verify(r =>
                r.RemoveReactionAsync(5, 10),
                Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateReactAsync_DifferentEmoji_UpdatesReaction()
        {
            repoMock
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync("🔥");

            repoMock
                .Setup(r => r.UpdateReactionAsync(5, 10, "👍"))
                .Returns(Task.CompletedTask);

            await service.AddOrUpdateReactAsync(5, 10, "👍");

            repoMock.Verify(r =>
                r.UpdateReactionAsync(5, 10, "👍"),
                Times.Once);
        }

        [Fact]
        public async Task RemoveReactionAsync_CallsRepository()
        {
            repoMock
                .Setup(r => r.RemoveReactionAsync(5, 10))
                .Returns(Task.CompletedTask);

            await service.RemoveReactionAsync(5, 10);

            repoMock.Verify(r =>
                r.RemoveReactionAsync(5, 10),
                Times.Once);
        }

        [Fact]
        public async Task GetUnreadCountsForUserAsync_ReturnsRepositoryData()
        {
            var expected = new Dictionary<int, int> { { 1, 3 } };

            repoMock
                .Setup(r => r.GetUnreadCountsForUserAsync(10))
                .ReturnsAsync(expected);

            var result = await service.GetUnreadCountsForUserAsync(10);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task MarkAsReadIfNeededAsync_AlreadyRead_ReturnsFalse()
        {
            var result = await service.MarkAsReadIfNeededAsync(5, 10, true);

            Assert.False(result);
        }

        [Fact]
        public async Task MarkAsReadIfNeededAsync_NotRead_CallsMarkAsRead()
        {
            repoMock
                .Setup(r => r.HasUserReadAsync(5, 10))
                .ReturnsAsync(false);

            repoMock
                .Setup(r => r.InsertReadReceiptAsync(5, 10))
                .Returns(Task.CompletedTask);

            var result = await service.MarkAsReadIfNeededAsync(5, 10, false);

            Assert.True(result);
        }

        [Fact]
        public async Task GetNonReadersAsync_ReturnsOnlyUsersWhoHaveNotRead()
        {
            var readers = new List<AnnouncementReadReceipt>
            {
                new AnnouncementReadReceipt
                {
                    User = new User { UserId = 1 }
                }
            };

            var participants = new List<User>
            {
                new User { UserId = 1 },
                new User { UserId = 2 }
            };

            repoMock
                .Setup(r => r.GetReadReceiptsAsync(5))
                .ReturnsAsync(readers);

            repoMock
                .Setup(r => r.GetAllParticipantsAsync(1))
                .ReturnsAsync(participants);

            var result = await service.GetNonReadersAsync(5, 1);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetNonReadersAsync_WhenAllUsersRead_ReturnsEmptyList()
        {
            var readers = new List<AnnouncementReadReceipt>
            {
                new AnnouncementReadReceipt
                {
                    User = new User { UserId = 1 }
                }
            };

            var participants = new List<User>
            {
                new User { UserId = 1 }
            };

            repoMock
                .Setup(r => r.GetReadReceiptsAsync(5))
                .ReturnsAsync(readers);

            repoMock
                .Setup(r => r.GetAllParticipantsAsync(1))
                .ReturnsAsync(participants);

            var result = await service.GetNonReadersAsync(5, 1);

            Assert.Empty(result);
        }

        [Fact]
        public void AttachReactions_WhenMatchingAnnouncement_AssignsReactions()
        {
            var service = new AnnouncementService(repoMock.Object, eventRepoMock.Object);

            var announcements = new List<Announcement>
            {
                new Announcement(1, "A1", DateTime.UtcNow)
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
                    Author = new User { UserId = 1 }
                })
            };

            service.AttachReactions(announcements, reactions);

            Assert.Single(announcements[0].Reactions);
        }

        [Fact]
        public void AttachReactions_WhenNoMatchingAnnouncement_DoesNotModifyReactions()
        {
            var service = new AnnouncementService(repoMock.Object, eventRepoMock.Object);

            var announcements = new List<Announcement>
            {
                new Announcement(99, "A1", DateTime.UtcNow)
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
                    Author = new User { UserId = 1 }
                })
            };

            service.AttachReactions(announcements, reactions);

            Assert.Empty(announcements[0].Reactions);
        }

        [Fact]
        public void AttachReactions_WhenMultipleReactions_AssignsAll()
        {
            var service = new AnnouncementService(repoMock.Object, eventRepoMock.Object);

            var announcements = new List<Announcement>
            {
                new Announcement(1, "A1", DateTime.UtcNow)
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
                    Author = new User { UserId = 1 }
                }),
                (1, new AnnouncementReaction
                {
                    Id = 2,
                    Emoji = "🔥",
                    AnnouncementId = 1,
                    Author = new User { UserId = 2 }
                })
            };

            service.AttachReactions(announcements, reactions);

            Assert.Equal(2, announcements[0].Reactions.Count);
        }
    }
}