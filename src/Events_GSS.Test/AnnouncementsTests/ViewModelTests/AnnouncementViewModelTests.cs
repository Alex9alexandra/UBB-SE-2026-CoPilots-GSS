using System;
using System.Collections.Generic;
using System.Linq;

using Events_GSS.Data.Models;
using Events_GSS.Data.ViewModelsCore;

using Xunit;

namespace Events_GSS.Tests.ViewModelsCore
{
    public class AnnouncementsViewModelCoreTests
    {
        [Fact]
        public void GetReadReceiptSummary_TotalZero_ReturnsNoParticipants()
        {
            var result = AnnouncementsViewModelCore.GetReadReceiptSummary(0, 0);

            Assert.Equal("No participants", result);
        }

        [Fact]
        public void GetReadReceiptSummary_ValidValues_ReturnsFormattedString()
        {
            var result = AnnouncementsViewModelCore.GetReadReceiptSummary(2, 4);

            Assert.Equal("2 / 4 read (50%)", result);
        }

        [Fact]
        public void CalculateUnreadCount_AllRead_ReturnsZero()
        {
            var announcements = new List<Announcement>
            {
                new Announcement(1, "Indiana Jones is gay", DateTime.UtcNow) { IsRead = true },
                new Announcement(2, "He's also cannonically a groomer btw", DateTime.UtcNow) { IsRead = true }
            };

            var result = AnnouncementsViewModelCore.CalculateUnreadCount(announcements);

            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateUnreadCount_SomeUnread_ReturnsCorrectCount()
        {
            var announcements = new List<Announcement>
            {
                new Announcement(1, "THEY. WANT. YOU", DateTime.UtcNow) { IsRead = false },
                new Announcement(2, "Oh my goodness", DateTime.UtcNow) { IsRead = true },
                new Announcement(3, "WHAT AM I GONNA DO IN A SUBMARINE", DateTime.UtcNow) { IsRead = false }
            };

            var result = AnnouncementsViewModelCore.CalculateUnreadCount(announcements);

            Assert.Equal(2, result);
        }

        [Fact]
        public void CanSubmit_EmptyString_ReturnsFalse()
        {
            var result = AnnouncementsViewModelCore.CanSubmit("");

            Assert.False(result);
        }

        [Fact]
        public void CanSubmit_Whitespace_ReturnsFalse()
        {
            var result = AnnouncementsViewModelCore.CanSubmit("   ");

            Assert.False(result);
        }

        [Fact]
        public void CanSubmit_ValidMessage_ReturnsTrue()
        {
            var result = AnnouncementsViewModelCore.CanSubmit("hello");

            Assert.True(result);
        }

        [Fact]
        public void GetSubmitMode_WhenEditing_ReturnsEdit()
        {
            var result = AnnouncementsViewModelCore.GetSubmitMode(true);

            Assert.Equal(AnnouncementsViewModelCore.SubmitMode.Edit, result);
        }

        [Fact]
        public void GetSubmitMode_WhenNotEditing_ReturnsCreate()
        {
            var result = AnnouncementsViewModelCore.GetSubmitMode(false);

            Assert.Equal(AnnouncementsViewModelCore.SubmitMode.Create, result);
        }

        [Fact]
        public void NormalizeMessage_TrimsWhitespace()
        {
            var result = AnnouncementsViewModelCore.NormalizeMessage("  butt  ");

            Assert.Equal("butt", result);
        }

        [Fact]
        public void GetEditableMessage_ReturnsOriginalMessage()
        {
            var announcement = new Announcement(1, "Aiaia cocojambo aiaie", DateTime.UtcNow);

            var result = AnnouncementsViewModelCore.GetEditableMessage(announcement);

            Assert.Equal("Aiaia cocojambo aiaie", result);
        }

        [Fact]
        public void Toggle_True_ReturnsFalse()
        {
            var result = AnnouncementsViewModelCore.Toggle(true);

            Assert.False(result);
        }

        [Fact]
        public void Toggle_False_ReturnsTrue()
        {
            var result = AnnouncementsViewModelCore.Toggle(false);

            Assert.True(result);
        }

        [Fact]
        public void ProcessReadReceipts_ReturnsCorrectCount()
        {
            var readers = new List<AnnouncementReadReceipt>
            {
                new(), new(), new()
            };

            var (_, count) = AnnouncementsViewModelCore.ProcessReadReceipts(readers);

            Assert.Equal(3, count);
        }

        [Fact]
        public void ProcessReadReceipts_ReturnsListWithSameElements()
        {
            var readers = new List<AnnouncementReadReceipt>
            {
                new(), new()
            };

            var (list, _) = AnnouncementsViewModelCore.ProcessReadReceipts(readers);

            Assert.Equal(2, list.Count);
        }
    }
}