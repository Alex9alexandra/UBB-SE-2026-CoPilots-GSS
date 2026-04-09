using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;

using Events_GSS.ViewModels;
using Events_GSS.Data.Models;
using Events_GSS.Data.Services.announcementServices;

using Xunit;

namespace Events_GSS.Tests.AnnouncementsTests.ViewModelTests
{
    public class AnnouncementViewModelTests : IDisposable
    {
        private readonly Mock<IAnnouncementService> _mockService;
        private readonly AnnouncementViewModel _vm;
        private readonly Event _event;

        public AnnouncementViewModelTests()
        {
            _mockService = new Mock<IAnnouncementService>();

            _event = new Event
            {
                EventId = 1,
                Admin = new User { UserId = 10 }
            };

            _vm = new AnnouncementViewModel(_event, _mockService.Object, 10, true);
        }

        public void Dispose()
        {
            // Cleanup if needed - xUnit will call this after each test
            // No specific cleanup needed for this test class
        }

        // -----------------------------
        // INIT / LOAD
        // -----------------------------

        [Fact]
        public async Task Initialize_LoadsAnnouncements()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetAnnouncementsAsync(1, 10))
                .ReturnsAsync(new List<Announcement>
                {
                    new Announcement(1, "msg", DateTime.UtcNow)
                });

            // Act
            await _vm.InitializeAsync();

            // Assert
            Assert.Single(_vm.Announcements);
        }

        // -----------------------------
        // SUBMIT (CREATE)
        // -----------------------------

        [Fact]
        public async Task SubmitAnnouncement_NewMessage_CallsCreate()
        {
            // Arrange
            _vm.NewMessage = "Hello";

            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Announcement>());

            // Act
            await _vm.SubmitAnnouncementCommand.ExecuteAsync(null);

            // Assert
            _mockService.Verify(s =>
                s.CreateAnnouncementAsync("Hello", 1, 10),
                Times.Once);
        }

        [Fact]
        public async Task SubmitAnnouncement_EmptyMessage_DoesNothing()
        {
            // Arrange
            _vm.NewMessage = "   ";

            // Act
            await _vm.SubmitAnnouncementCommand.ExecuteAsync(null);

            // Assert
            _mockService.Verify(s =>
                s.CreateAnnouncementAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        // -----------------------------
        // SUBMIT (UPDATE)
        // -----------------------------

        [Fact]
        public async Task SubmitAnnouncement_EditMode_CallsUpdate()
        {
            // Arrange
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "old", DateTime.UtcNow),
                10,
                true);

            _vm.EditingAnnouncement = item;
            _vm.NewMessage = "updated";

            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Announcement>());

            // Act
            await _vm.SubmitAnnouncementCommand.ExecuteAsync(null);

            // Assert
            _mockService.Verify(s =>
                s.UpdateAnnouncementAsync(5, "updated", 10, 1),
                Times.Once);
        }

        // -----------------------------
        // DELETE
        // -----------------------------

        [Fact]
        public async Task DeleteAnnouncement_RemovesFromCollection()
        {
            // Arrange
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "msg", DateTime.UtcNow),
                10,
                true);

            _vm.Announcements.Add(item);

            // Act
            await _vm.DeleteAnnouncementCommand.ExecuteAsync(item);

            // Assert
            _mockService.Verify(s =>
                s.DeleteAnnouncementAsync(5, 10, 1),
                Times.Once);

            Assert.DoesNotContain(item, _vm.Announcements);
        }

        // -----------------------------
        // PIN
        // -----------------------------

        [Fact]
        public async Task PinAnnouncement_CallsService()
        {
            // Arrange
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "msg", DateTime.UtcNow),
                10,
                true);

            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Announcement>());

            // Act
            await _vm.PinAnnouncementCommand.ExecuteAsync(item);

            // Assert
            _mockService.Verify(s =>
                s.PinAnnouncementAsync(5, 1, 10),
                Times.Once);
        }

        // -----------------------------
        // TOGGLE EXPAND
        // -----------------------------

        [Fact]
        public async Task ToggleExpand_Unread_MarksAsRead()
        {
            // Arrange
            var model = new Announcement(5, "msg", DateTime.UtcNow)
            {
                IsRead = false
            };

            var item = new AnnouncementItemViewModel(model, 10, true);

            _vm.Announcements.Add(item);

            _mockService
                .Setup(s => s.MarkAsReadIfNeededAsync(5, 10, false))
                .ReturnsAsync(true);

            // Act
            await _vm.ToggleExpandCommand.ExecuteAsync(item);

            // Assert
            Assert.True(item.IsRead);
        }

        [Fact]
        public async Task ToggleExpand_AlreadyRead_DoesNotMarkAsReadAgain()
        {
            var model = new Announcement(5, "msg", DateTime.UtcNow)
            {
                IsRead = true
            };

            var item = new AnnouncementItemViewModel(model, 10, true);
            _vm.Announcements.Add(item);

            // Act
            await _vm.ToggleExpandCommand.ExecuteAsync(item);

            // Assert
            _mockService.Verify(s =>
                s.MarkAsReadIfNeededAsync(5, 10, true),  // Called with IsRead = true
                Times.Once);

            // The service should handle the logic of not marking as read
            Assert.True(item.IsRead);
        }

        // -----------------------------
        // READ RECEIPTS
        // -----------------------------

        [Fact]
        public async Task LoadReadReceipts_NonAdmin_DoesNotLoad()
        {
            // Arrange
            var nonAdminVm = new AnnouncementViewModel(_event, _mockService.Object, 10, false);
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "msg", DateTime.UtcNow),
                10,
                false);

            // Act
            await nonAdminVm.LoadReadReceiptsCommand.ExecuteAsync(item);

            // Assert
            _mockService.Verify(s =>
                s.GetReadReceiptsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        // -----------------------------
        // TOGGLE REACTION
        // -----------------------------

        [Fact]
        public async Task ToggleReaction_CallsService()
        {
            // Arrange
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "msg", DateTime.UtcNow),
                10,
                true);

            var payload = new AnnouncementReactionPayload(item, "🔥");

            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Announcement>());

            // Act
            await _vm.ToggleReactionCommand.ExecuteAsync(payload);

            // Assert
            _mockService.Verify(s =>
                s.ToggleReactionAsync(5, 10, "🔥"),
                Times.Once);
        }

        [Fact]
        public async Task ToggleReaction_NullPayload_DoesNothing()
        {
            // Act
            await _vm.ToggleReactionCommand.ExecuteAsync(null);

            // Assert
            _mockService.Verify(s =>
                s.ToggleReactionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        // -----------------------------
        // READ RECEIPT SUMMARY
        // -----------------------------

        [Fact]
        public void ReadReceiptSummary_ComputesCorrectly()
        {
            // Arrange
            _vm.ReadReceiptReadCount = 5;
            _vm.ReadReceiptTotalCount = 10;

            // Act
            var summary = _vm.ReadReceiptSummary;

            // Assert
            Assert.Equal("5 / 10 read (50%)", summary);
        }

        [Fact]
        public void ReadReceiptSummary_NoParticipants()
        {
            // Arrange
            _vm.ReadReceiptTotalCount = 0;

            // Act
            var summary = _vm.ReadReceiptSummary;

            // Assert
            Assert.Equal("No participants", summary);
        }

        [Theory]
        [InlineData(0, 10, "0 / 10 read (0%)")]
        [InlineData(3, 3, "3 / 3 read (100%)")]
        [InlineData(1, 3, "1 / 3 read (33%)")]
        [InlineData(7, 10, "7 / 10 read (70%)")]
        public void ReadReceiptSummary_VariousCounts_ComputesCorrectly(int read, int total, string expected)
        {
            // Arrange
            _vm.ReadReceiptReadCount = read;
            _vm.ReadReceiptTotalCount = total;

            // Act
            var summary = _vm.ReadReceiptSummary;

            // Assert
            Assert.Equal(expected, summary);
        }

        // -----------------------------
        // ERROR HANDLING
        // -----------------------------

        [Fact]
        public async Task RunGuardedAsync_Unauthorized_SetsErrorMessage()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            await _vm.InitializeAsync();

            // Assert
            Assert.Equal("You don't have permission for this action.", _vm.ErrorMessage);
        }

        [Fact]
        public async Task RunGuardedAsync_GenericException_SetsErrorMessage()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetAnnouncementsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            await _vm.InitializeAsync();

            // Assert
            Assert.Equal("Something went wrong", _vm.ErrorMessage);
        }

        // -----------------------------
        // EDIT MODE
        // -----------------------------

        [Fact]
        public void StartEdit_SetsEditingState()
        {
            // Arrange
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "Test message", DateTime.UtcNow),
                10,
                true);

            // Act
            _vm.StartEditCommand.Execute(item);

            // Assert
            Assert.Equal(item, _vm.EditingAnnouncement);
            Assert.Equal("Test message", _vm.NewMessage);
            Assert.True(_vm.IsEditing);
            Assert.Equal("Save Edit", _vm.CreateButtonText);
        }

        [Fact]
        public void CancelEdit_ClearsEditingState()
        {
            // Arrange
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "Test message", DateTime.UtcNow),
                10,
                true);

            _vm.EditingAnnouncement = item;
            _vm.NewMessage = "Test message";

            // Act
            _vm.CancelEditCommand.Execute(null);

            // Assert
            Assert.Null(_vm.EditingAnnouncement);
            Assert.Equal(string.Empty, _vm.NewMessage);
            Assert.False(_vm.IsEditing);
        }

        [Fact]
        public void StartEdit_NullItem_DoesNotChangeState()
        {
            // Arrange
            _vm.EditingAnnouncement = null;
            _vm.NewMessage = "initial";

            // Act
            _vm.StartEditCommand.Execute(null);

            // Assert
            Assert.Null(_vm.EditingAnnouncement);
            Assert.Equal("initial", _vm.NewMessage);
        }

        [Fact]
        public async Task LoadReadReceipts_WhenNullOrNotAdmin_DoesNothing()
        {
            var vm = new AnnouncementViewModel(_event, _mockService.Object, 10, false);

            await vm.LoadReadReceiptsCommand.ExecuteAsync(null);

            _mockService.Verify(
                s => s.GetReadReceiptsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task LoadReadReceipts_WhenServiceFails_DoesNotCrash()
        {
            _mockService
                .Setup(s => s.GetReadReceiptsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("fail"));

            var item = new AnnouncementItemViewModel(new Announcement(5, "msg", DateTime.UtcNow),
    10,
    false);

            await _vm.LoadReadReceiptsCommand.ExecuteAsync(item);

            Assert.False(_vm.IsReadReceiptsLoading);
        }

        [Fact]
        public async Task DeleteAnnouncement_NullItem_DoesNothing()
        {
            // Act
            await _vm.DeleteAnnouncementCommand.ExecuteAsync(null);

            // Assert
            _mockService.Verify(s =>
                s.DeleteAnnouncementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task PinAnnouncement_NullItem_DoesNothing()
        {
            // Act
            await _vm.PinAnnouncementCommand.ExecuteAsync(null);

            // Assert
            _mockService.Verify(s =>
                s.PinAnnouncementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task ToggleExpand_NullItem_DoesNothing()
        {
            // Act
            await _vm.ToggleExpandCommand.ExecuteAsync(null);

            // Assert
            _mockService.Verify(s =>
                s.MarkAsReadIfNeededAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task LoadReadReceipts_Admin_LoadsAndPopulatesCollection()
        {
            // Arrange
            var item = new AnnouncementItemViewModel(
                new Announcement(5, "msg", DateTime.UtcNow),
                10,
                true);

            var readers = new List<AnnouncementReadReceipt>
    {
        new AnnouncementReadReceipt(),
        new AnnouncementReadReceipt()
    };

            _mockService
                .Setup(s => s.GetReadReceiptsAsync(5, 1, 10))
                .ReturnsAsync((readers, 2));

            // Act
            await _vm.LoadReadReceiptsCommand.ExecuteAsync(item);

            // Assert (THIS is what matters)
            Assert.Equal(2, _vm.ReadReceiptUsers.Count);
            Assert.Equal(2, _vm.ReadReceiptReadCount);
            Assert.Equal(2, _vm.ReadReceiptTotalCount);
        }
    }
}