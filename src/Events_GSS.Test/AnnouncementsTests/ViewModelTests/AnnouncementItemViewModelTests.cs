using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using Events_GSS.ViewModels;
using Events_GSS.Data.Models;

namespace Events_GSS.Tests.AnnouncementsTests.ViewModelTests
{
    [TestFixture]
    public class AnnouncementItemViewModelTests
    {
        [Test]
        public void PreviewText_EmptyMessage_ReturnsEmpty()
        {
            var model = new Announcement(1, "", DateTime.UtcNow);

            var vm = new AnnouncementItemViewModel(model, 1, false);

            NUnit.Framework.Assert.That(vm.PreviewText, Is.EqualTo(string.Empty));
        }

        [Test]
        public void PreviewText_LongMessage_TrimsTo120Chars()
        {
            var longText = new string('a', 150);
            var model = new Announcement(1, longText, DateTime.UtcNow);

            var vm = new AnnouncementItemViewModel(model, 1, false);

            NUnit.Framework.Assert.That(vm.PreviewText.Length, Is.EqualTo(121)); // 120 + "…"
        }

        [Test]
        public void HasFullContent_WithNewline_ReturnsTrue()
        {
            var model = new Announcement(1, "line1\nline2", DateTime.UtcNow);

            var vm = new AnnouncementItemViewModel(model, 1, false);

            NUnit.Framework.Assert.That(vm.HasFullContent, Is.True);
        }

        [Test]
        public void HasFullContent_ShortMessage_ReturnsFalse()
        {
            var model = new Announcement(1, "short", DateTime.UtcNow);

            var vm = new AnnouncementItemViewModel(model, 1, false);

            NUnit.Framework.Assert.That(vm.HasFullContent, Is.False);
        }

        [Test]
        public void ReactionGroups_GroupsByEmoji_CorrectCounts()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
                {
                    new AnnouncementReaction { Emoji = "👍", Author = new User { UserId = 1 } },
                    new AnnouncementReaction { Emoji = "👍", Author = new User { UserId = 2 } },
                    new AnnouncementReaction { Emoji = "🔥", Author = new User { UserId = 3 } },
                }
            };

            var vm = new AnnouncementItemViewModel(model, 1, false);

            var groups = vm.ReactionGroups;

            NUnit.Framework.Assert.That(groups.Count, Is.EqualTo(2));
            NUnit.Framework.Assert.That(groups.First(g => g.Emoji == "👍").Count, Is.EqualTo(2));
        }

        [Test]
        public void ReactionGroups_CurrentUserReacted_DetectedCorrectly()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
                {
                    new AnnouncementReaction { Emoji = "👍", Author = new User { UserId = 1 } }
                }
            };

            var vm = new AnnouncementItemViewModel(model, 1, false);

            var group = vm.ReactionGroups.First();

            NUnit.Framework.Assert.That(group.CurrentUserReacted, Is.True);
        }

        [Test]
        public void CurrentUserEmoji_UserHasReaction_ReturnsEmoji()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>
                {
                    new AnnouncementReaction { Emoji = "🔥", Author = new User { UserId = 1 } }
                }
            };

            var vm = new AnnouncementItemViewModel(model, 1, false);

            NUnit.Framework.Assert.That(vm.CurrentUserEmoji, Is.EqualTo("🔥"));
        }

        [Test]
        public void HasReactions_WhenEmpty_ReturnsFalse()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                Reactions = new List<AnnouncementReaction>()
            };

            var vm = new AnnouncementItemViewModel(model, 1, false);

            NUnit.Framework.Assert.That(vm.HasReactions, Is.False);
        }

        [Test]
        public void IsUnread_WhenRead_ReturnsFalse()
        {
            var model = new Announcement(1, "msg", DateTime.UtcNow)
            {
                IsRead = true
            };

            var vm = new AnnouncementItemViewModel(model, 1, false);

            NUnit.Framework.Assert.That(vm.IsUnread, Is.False);
        }
    }
}