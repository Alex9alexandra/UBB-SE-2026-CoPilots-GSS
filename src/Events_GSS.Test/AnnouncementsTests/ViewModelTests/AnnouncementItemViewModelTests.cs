using System;
using System.Collections.Generic;
using System.Linq;

using Events_GSS.Data.Models;
using Events_GSS.Data.ViewModelsCore;

using Xunit;

namespace Events_GSS.Tests.AnnouncementsTests.ViewModelTests
{
    public class AnnouncementItemViewModelTests
    {
        [Fact]
        public void PreviewText_EmptyMessage_ReturnsEmpty()
        {
            var model = new Announcement(1, "", DateTime.UtcNow);
            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.Equal(string.Empty, core.PreviewText);
        }

        [Fact]
        public void PreviewText_Under120Chars_ReturnsFullText()
        {
            var text = "short message";
            var model = new Announcement(1, text, DateTime.UtcNow);
            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.Equal(text, core.PreviewText);
        }

        [Fact]
        public void PreviewText_Over120Chars_TrimsAndAddsEllipsis()
        {
            var text = new string("never gonna give you up, " +
                                    "never gonna let you down, " +
                                    "never gonna run around and desert you. " +
                                    "never gonna make you cry, " +
                                    "never gonna say goodbye, " +
                                    "is this over 120 characters yeeeet?");
            var model = new Announcement(1, text, DateTime.UtcNow);
            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.Equal(121, core.PreviewText.Length);
        }

        [Fact]
        public void PreviewText_WithNewline_ReturnsFirstLineOnly()
        {
            var model = new Announcement(1, "hehehe\nhaw", DateTime.UtcNow);
            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.Equal("hehehe", core.PreviewText);
        }

        [Fact]
        public void HasFullContent_WithNewline_ReturnsTrue()
        {
            var model = new Announcement(1, "yabadaba\ndooooo", DateTime.UtcNow);
            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.True(core.HasFullContent);
        }

        [Fact]
        public void HasFullContent_Over120Chars_ReturnsTrue()
        {
            var model = new Announcement(1, new string('a', 150), DateTime.UtcNow);
            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.True(core.HasFullContent);
        }

        [Fact]
        public void HasFullContent_ShortMessage_ReturnsFalse()
        {
            var model = new Announcement(1, "shortie", DateTime.UtcNow);
            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.False(core.HasFullContent);
        }

        [Fact]
        public void ReactionGroups_GroupCount_IsCorrect()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
        {
            new() { Emoji = "👍", Author = new User { UserId = 1 } },
            new() { Emoji = "🔥", Author = new User { UserId = 2 } }
        }
            };

            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.Equal(2, core.ReactionGroups.Count);
        }

        [Fact]
        public void ReactionGroups_CountPerEmoji_IsCorrect()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
        {
            new() { Emoji = "👍", Author = new User { UserId = 1 } },
            new() { Emoji = "👍", Author = new User { UserId = 2 } }
        }
            };

            var core = new AnnouncementItemViewModelCore(model, 1);

            var group = core.ReactionGroups.First(g => g.Emoji == "👍");

            Assert.Equal(2, group.Count);
        }

        [Fact]
        public void ReactionGroups_CurrentUserReacted_TrueWhenUserReacted()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
        {
            new() { Emoji = "👍", Author = new User { UserId = 1 } }
        }
            };

            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.True(core.ReactionGroups.First().CurrentUserReacted);
        }

        [Fact]
        public void ReactionGroups_CurrentUserReacted_FalseWhenUserDidNotReact()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
        {
            new() { Emoji = "👍", Author = new User { UserId = 2 } }
        }
            };

            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.False(core.ReactionGroups.First().CurrentUserReacted);
        }

        [Fact]
        public void CurrentUserEmoji_WhenUserReacted_ReturnsEmoji()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
        {
            new() { Emoji = "🔥", Author = new User { UserId = 1 } }
        }
            };

            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.Equal("🔥", core.CurrentUserEmoji);
        }

        [Fact]
        public void CurrentUserEmoji_WhenUserDidNotReact_ReturnsNull()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
        {
            new() { Emoji = "🔥", Author = new User { UserId = 2 } }
        }
            };

            var core = new AnnouncementItemViewModelCore(model, 1);

            Assert.Null(core.CurrentUserEmoji);
        }


    }
}