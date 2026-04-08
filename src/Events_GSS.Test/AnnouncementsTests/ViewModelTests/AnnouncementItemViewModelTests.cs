using System;
using System.Collections.Generic;
using System.Linq;

using Events_GSS.ViewModels;
using Events_GSS.Data.Models;

using Xunit;

namespace Events_GSS.Tests.AnnouncementsTests.ViewModelTests
{
    public class AnnouncementItemViewModelTests
    {
        [Fact]
        public void PreviewText_EmptyMessage_ReturnsEmpty()
        {
            // Arrange
            var model = new Announcement(1, "", DateTime.UtcNow);

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.Equal(string.Empty, vm.PreviewText);
        }

        [Fact]
        public void PreviewText_LongMessage_TrimsTo120Chars()
        {
            // Arrange
            var longText = new string('a', 150);
            var model = new Announcement(1, longText, DateTime.UtcNow);

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.Equal(121, vm.PreviewText.Length); // 120 + "…"
        }

        [Fact]
        public void HasFullContent_WithNewline_ReturnsTrue()
        {
            // Arrange
            var model = new Announcement(1, "line1\nline2", DateTime.UtcNow);

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.True(vm.HasFullContent);
        }

        [Fact]
        public void HasFullContent_ShortMessage_ReturnsFalse()
        {
            // Arrange
            var model = new Announcement(1, "short", DateTime.UtcNow);

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.False(vm.HasFullContent);
        }

        [Fact]
        public void ReactionGroups_GroupsByEmoji_CorrectCounts()
        {
            // Arrange
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
                {
                    new AnnouncementReaction { Emoji = "👍", Author = new User { UserId = 1 } },
                    new AnnouncementReaction { Emoji = "👍", Author = new User { UserId = 2 } },
                    new AnnouncementReaction { Emoji = "🔥", Author = new User { UserId = 3 } },
                }
            };

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);
            var groups = vm.ReactionGroups;

            // Assert
            Assert.Equal(2, groups.Count);
            Assert.Equal(2, groups.First(g => g.Emoji == "👍").Count);
        }

        [Fact]
        public void ReactionGroups_CurrentUserReacted_DetectedCorrectly()
        {
            // Arrange
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
                {
                    new AnnouncementReaction { Emoji = "👍", Author = new User { UserId = 1 } }
                }
            };

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);
            var group = vm.ReactionGroups.First();

            // Assert
            Assert.True(group.CurrentUserReacted);
        }

        [Fact]
        public void CurrentUserEmoji_UserHasReaction_ReturnsEmoji()
        {
            // Arrange
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
                {
                    new AnnouncementReaction { Emoji = "🔥", Author = new User { UserId = 1 } }
                }
            };

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.Equal("🔥", vm.CurrentUserEmoji);
        }

        [Fact]
        public void HasReactions_WhenEmpty_ReturnsFalse()
        {
            // Arrange
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>()
            };

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.False(vm.HasReactions);
        }

        [Fact]
        public void IsUnread_WhenRead_ReturnsFalse()
        {
            // Arrange
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                IsRead = true
            };

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.False(vm.IsUnread);
        }

        [Fact]
        public void CurrentUserEmoji_UserHasNoReaction_ReturnsNull()
        {
            // Arrange
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
                {
                    new AnnouncementReaction { Emoji = "🔥", Author = new User { UserId = 2 } }
                }
            };

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.Null(vm.CurrentUserEmoji);
        }

        [Fact]
        public void IsUnread_WhenUnread_ReturnsTrue()
        {
            // Arrange
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                IsRead = false
            };

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.True(vm.IsUnread);
        }

        [Theory]
        [InlineData(120, 120)] // Exactly 120 chars, no ellipsis
        [InlineData(121, 121)] // 121 chars, gets trimmed to 120 + "…"
        [InlineData(200, 121)] // 200 chars, gets trimmed to 120 + "…"
        public void PreviewText_VariousLengths_HandlesCorrectly(int length, int expectedLength)
        {
            // Arrange
            var text = new string('a', length);
            var model = new Announcement(1, text, DateTime.UtcNow);

            // Act
            var vm = new AnnouncementItemViewModel(model, 1, false);

            // Assert
            Assert.Equal(expectedLength, vm.PreviewText.Length);
        }
    }
}