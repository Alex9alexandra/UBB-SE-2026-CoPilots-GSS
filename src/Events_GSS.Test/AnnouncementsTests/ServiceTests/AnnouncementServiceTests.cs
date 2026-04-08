using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services;

using Moq;

using NUnit.Framework;

using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Events_GSS.Test.AnnouncementsTests.ServiceTests
{

    [TestFixture]
    public class AnnouncementServiceTests
    {

        private Mock<IAnnouncementRepository>? _mockRepo;
        private Mock<IEventRepository>? _mockEventRepo;
        private AnnouncementService? _service;

        private Event? _validEvent;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<IAnnouncementRepository>();
            _mockEventRepo = new Mock<IEventRepository>();

            _service = new AnnouncementService(
                _mockRepo.Object,
                _mockEventRepo.Object);

            _validEvent = new Event
            {
                EventId = 1,
                Admin = new Data.Models.User { UserId = 10, Name = "Admin" }
            };
        }

        private void SetupAdmin(int eventId = 1, int userId = 10)
        {
            _mockEventRepo
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(_validEvent);
        }

        [Test]
        public async Task CreateAnnouncement_ValidMessage_CallsRepository()
        {
            SetupAdmin();

            await this._service.CreateAnnouncementAsync("Hello", 1, 10);

            this._mockRepo.Verify(r =>
                r.AddAnnouncementAsync(It.IsAny<Announcement>(), 1, 10),
                Times.Once);
        }

        [Test]
        public void CreateAnnouncement_EmptyMessage_ThrowsException()
        {
            SetupAdmin();

            NUnit.Framework.Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._service.CreateAnnouncementAsync("", 1, 10));
        }

        [Test]
        public void CreateAnnouncement_WhitespaceMessage_ThrowsException()
        {
            SetupAdmin();

            NUnit.Framework.Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._service.CreateAnnouncementAsync("   ", 1, 10));
        }

        [Test]
        public void CreateAnnouncement_NonAdmin_ThrowsUnauthorized()
        {
            this._mockEventRepo
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Event
                {
                    EventId = 1,
                    Admin = new Data.Models.User { UserId = 999 }
                });

            NUnit.Framework.Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await this._service.CreateAnnouncementAsync("Hello", 1, 10));
        }

        [Test]
        public async Task UpdateAnnouncement_ValidData_CallsUpdate()
        {
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync(new Announcement(5, "old", DateTime.UtcNow));

            await _service.UpdateAnnouncementAsync(5, "new message", 10, 1);

            _mockRepo.Verify(r =>
                r.UpdateAnnouncementAsync(5, "new message"),
                Times.Once);
        }

        [Test]
        public void UpdateAnnouncement_NotFound_ThrowsKeyNotFound()
        {
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync((Announcement)null);

            NUnit.Framework.Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _service.UpdateAnnouncementAsync(5, "new", 10, 1));
        }

        [Test]
        public async Task DeleteAnnouncement_Valid_CallsDelete()
        {
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync(new Announcement(5, "msg", DateTime.UtcNow));

            await _service.DeleteAnnouncementAsync(5, 10, 1);

            _mockRepo.Verify(r => r.DeleteAnnouncementAsync(5), Times.Once);
        }

        [Test]
        public void DeleteAnnouncement_NotFound_Throws()
        {
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetAnnouncementByIdAsync(5))
                .ReturnsAsync((Announcement)null);

            NUnit.Framework.Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _service.DeleteAnnouncementAsync(5, 10, 1));
        }

        [Test]
        public async Task PinAnnouncement_Valid_UnpinsThenPins()
        {
            SetupAdmin();

            await _service.PinAnnouncementAsync(5, 1, 10);

            _mockRepo.Verify(r => r.UnpinAnnouncementAsync(1), Times.Once);
            _mockRepo.Verify(r => r.PinAsync(5, 1), Times.Once);
        }

        [Test]
        public async Task MarkAsRead_Always_CallsRepository()
        {
            await _service.MarkAsReadAsync(5, 10);

            _mockRepo.Verify(r => r.MarkAsReadAsync(5, 10), Times.Once);
        }

        [Test]
        public async Task MarkAsReadIfNeeded_NotRead_MarksAsRead()
        {
            var result = await _service.MarkAsReadIfNeededAsync(5, 10, false);

            NUnit.Framework.Assert.IsTrue(result);

            _mockRepo.Verify(r => r.MarkAsReadAsync(5, 10), Times.Once);
        }

        [Test]
        public async Task MarkAsReadIfNeeded_AlreadyRead_DoesNothing()
        {
            var result = await _service.MarkAsReadIfNeededAsync(5, 10, true);

            NUnit.Framework.Assert.IsFalse(result);

            _mockRepo.Verify(r => r.MarkAsReadAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task ToggleReaction_SameEmoji_RemovesReaction()
        {
            _mockRepo
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync("👍");

            await _service.ToggleReactionAsync(5, 10, "👍");

            _mockRepo.Verify(r => r.RemoveReactionAsync(5, 10), Times.Once);
        }

        [Test]
        public async Task ToggleReaction_DifferentEmoji_UpdatesReaction()
        {
            _mockRepo
                .Setup(r => r.GetUserReactionAsync(5, 10))
                .ReturnsAsync("👍");

            await _service.ToggleReactionAsync(5, 10, "🔥");

            _mockRepo.Verify(r =>
                r.AddOrUpdateReactionAsync(5, 10, "🔥"),
                Times.Once);
        }

        [Test]
        public async Task GetReadReceipts_ReturnsData()
        {
            SetupAdmin();

            _mockRepo
                .Setup(r => r.GetReadReceiptsAsync(5))
                .ReturnsAsync(new List<AnnouncementReadReceipt>());

            _mockRepo
                .Setup(r => r.GetTotalParticipantsAsync(1))
                .ReturnsAsync(10);

            var result = await _service.GetReadReceiptsAsync(5, 1, 10);

            NUnit.Framework.Assert.That(result.TotalParticipants, Is.EqualTo(10));
        }

        [Test]
        public async Task GetNonReaders_FiltersCorrectly()
        {
            var readers = new List<AnnouncementReadReceipt>
            {
                new AnnouncementReadReceipt
                {
                    User = new Data.Models.User { UserId = 1 }
                }
            };

                    var users = new List<Data.Models.User>
            {
                new Data.Models.User { UserId = 1 },
                new Data.Models.User { UserId = 2 }
            };

            _mockRepo.Setup(r => r.GetReadReceiptsAsync(5)).ReturnsAsync(readers);
            _mockRepo.Setup(r => r.GetAllParticipantsAsync(1)).ReturnsAsync(users);

            var result = await _service.GetNonReadersAsync(5, 1);

            NUnit.Framework.Assert.That(result.Count, Is.EqualTo(1));
            NUnit.Framework.Assert.That(result[0].UserId, Is.EqualTo(2));
        }


        [Test]
        public async Task GetUnreadCounts_ReturnsRepositoryData()
        {
            _mockRepo
                .Setup(r => r.GetUnreadCountsForUserAsync(10))
                .ReturnsAsync(new Dictionary<int, int> { { 1, 5 } });

            var result = await _service.GetUnreadCountsForUserAsync(10);

            NUnit.Framework.Assert.That(result.ContainsKey(1), Is.True);
        }

        [Test]
        public async Task AddOrUpdateReact_CallsRepository()
        {
            await _service.AddOrUpdateReactAsync(5, 10, "🔥");

            _mockRepo.Verify(r =>
                r.AddOrUpdateReactionAsync(5, 10, "🔥"),
                Times.Once);
        }

        [Test]
        public async Task RemoveReaction_CallsRepository()
        {
            await _service.RemoveReactionAsync(5, 10);

            _mockRepo.Verify(r =>
                r.RemoveReactionAsync(5, 10),
                Times.Once);
        }
    }
}
