using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

using Moq;

using Events_GSS.ViewModels;
using Events_GSS.Data.Models;
using Events_GSS.Data.Services.announcementServices;

namespace Events_GSS.Tests.AnnouncementsTests.ViewModelTests
{
    [TestFixture]
    public class AnnouncementViewModelTests
    {
        private Mock<IAnnouncementService> _mockService;
        private AnnouncementViewModel _vm;
        private Event _event;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IAnnouncementService>();

            _event = new Event
            {
                EventId = 1,
                Admin = new User { UserId = 10 }
            };

            _vm = new AnnouncementViewModel(_event, _mockService.Object, 10, true);
        }

        // -----------------------------
        // INIT / LOAD
        // -----------------------------

        [Test]
        public async Task Initialize_LoadsAnnouncements()
        {
            _mockService
                .Setup(s => s.GetAnnouncementsAsync(1, 10))
                .ReturnsAsync(new List<Announcement>
                {
                    new Announcement(1, "msg", DateTime.UtcNow)
                });

            await _vm.InitializeAsync();

            NUnit.Framework.Assert.That(_vm.Announcements.Count, Is.EqualTo(1));
        }

        // -----------------------------
        // SUBMIT (CREATE)
        // -----------------------------

        [Test]
        public async Task SubmitAnnouncement_NewMessage_CallsCreate()
        {
            _vm.NewMessage = "Hello";

            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Announcement>());

            await _vm.SubmitAnnouncementCommand.ExecuteAsync(null);

            _mockService.Verify(s =>
                s.CreateAnnouncementAsync("Hello", 1, 10),
                Times.Once);
        }

        [Test]
        public async Task SubmitAnnouncement_EmptyMessage_DoesNothing()
        {
            _vm.NewMessage = "   ";

            await _vm.SubmitAnnouncementCommand.ExecuteAsync(null);

            _mockService.Verify(s =>
                s.CreateAnnouncementAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        // -----------------------------
        // SUBMIT (UPDATE)
        // -----------------------------

        [Test]
        public async Task SubmitAnnouncement_EditMode_CallsUpdate()
        {
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "old", DateTime.UtcNow),
                10,
                true);

            _vm.EditingAnnouncement = item;
            _vm.NewMessage = "updated";

            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Announcement>());

            await _vm.SubmitAnnouncementCommand.ExecuteAsync(null);

            _mockService.Verify(s =>
                s.UpdateAnnouncementAsync(5, "updated", 10, 1),
                Times.Once);
        }

        // -----------------------------
        // DELETE
        // -----------------------------

        [Test]
        public async Task DeleteAnnouncement_RemovesFromCollection()
        {
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "msg", DateTime.UtcNow),
                10,
                true);

            _vm.Announcements.Add(item);

            await _vm.DeleteAnnouncementCommand.ExecuteAsync(item);

            _mockService.Verify(s =>
                s.DeleteAnnouncementAsync(5, 10, 1),
                Times.Once);

            NUnit.Framework.Assert.That(_vm.Announcements.Contains(item), Is.False);
        }

        // -----------------------------
        // PIN
        // -----------------------------

        [Test]
        public async Task PinAnnouncement_CallsService()
        {
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "msg", DateTime.UtcNow),
                10,
                true);

            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Announcement>());

            await _vm.PinAnnouncementCommand.ExecuteAsync(item);

            _mockService.Verify(s =>
                s.PinAnnouncementAsync(5, 1, 10),
                Times.Once);
        }

        // -----------------------------
        // TOGGLE EXPAND
        // -----------------------------

        [Test]
        public async Task ToggleExpand_Unread_MarksAsRead()
        {
            var model = new Announcement(5, "msg", DateTime.UtcNow)
            {
                IsRead = false
            };

            var item = new AnnouncementItemViewModel(model, 10, true);

            _vm.Announcements.Add(item);

            _mockService
                .Setup(s => s.MarkAsReadIfNeededAsync(5, 10, false))
                .ReturnsAsync(true);

            await _vm.ToggleExpandCommand.ExecuteAsync(item);

            NUnit.Framework.Assert.That(item.IsRead, Is.True);
        }

        // -----------------------------
        // READ RECEIPTS
        // -----------------------------

        [Test]
        public async Task LoadReadReceipts_Admin_LoadsData()
        {
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "msg", DateTime.UtcNow),
                10,
                true);

            _mockService
                .Setup(s => s.GetReadReceiptsAsync(5, 1, 10))
                .ReturnsAsync((new List<AnnouncementReadReceipt>(), 5));

            await _vm.LoadReadReceiptsCommand.ExecuteAsync(item);

            NUnit.Framework.Assert.That(_vm.ReadReceiptTotalCount, Is.EqualTo(5));
        }

        // -----------------------------
        // TOGGLE REACTION
        // -----------------------------

        [Test]
        public async Task ToggleReaction_CallsService()
        {
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "msg", DateTime.UtcNow),
                10,
                true);

            var payload = new AnnouncementReactionPayload(item, "🔥");

            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Announcement>());

            await _vm.ToggleReactionCommand.ExecuteAsync(payload);

            _mockService.Verify(s =>
                s.ToggleReactionAsync(5, 10, "🔥"),
                Times.Once);
        }

        // -----------------------------
        // READ RECEIPT SUMMARY
        // -----------------------------

        [Test]
        public void ReadReceiptSummary_ComputesCorrectly()
        {
            _vm.ReadReceiptReadCount = 5;
            _vm.ReadReceiptTotalCount = 10;

            NUnit.Framework.Assert.That(_vm.ReadReceiptSummary, Is.EqualTo("5 / 10 read (50%)"));
        }

        [Test]
        public void ReadReceiptSummary_NoParticipants()
        {
            _vm.ReadReceiptTotalCount = 0;

            NUnit.Framework.Assert.That(_vm.ReadReceiptSummary, Is.EqualTo("No participants"));
        }

        // -----------------------------
        // ERROR HANDLING
        // -----------------------------

        [Test]
        public async Task RunGuardedAsync_Unauthorized_SetsErrorMessage()
        {
            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            await _vm.InitializeAsync();

            NUnit.Framework.Assert.That(_vm.ErrorMessage, Is.EqualTo("You don't have permission for this action."));
        }
    }
}