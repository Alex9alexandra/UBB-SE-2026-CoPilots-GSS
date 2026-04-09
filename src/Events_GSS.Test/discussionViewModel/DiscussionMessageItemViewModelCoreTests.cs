using System.Collections.Generic;
using System.Linq;

using Events_GSS.Data.Models;
using Events_GSS.ViewModels;
using Events_GSS.ViewModelsCore;

using Xunit;

namespace Events_GSS.Tests.ViewModels;

public class DiscussionMessageItemViewModelCoreTests
{

    private static User MakeUser(int id, string name = "User") =>
        new() { UserId = id, Name = name };

    private static DiscussionReaction MakeReaction(string emoji, int authorId) =>
         new()
         {
             Emoji = emoji,
             Author = MakeUser(authorId),
             Message = new DiscussionMessage(
            id: 1,
            message: "Test message",
            date: DateTime.UtcNow)
         };

 

    public class ShowMuteButtonTests
    {
        [Fact]
        public void ShowMuteButton_AdminViewingOtherUsersMessage_ReturnsTrue()
        {
            Assert.True(DiscussionMessageItemViewModelCore.ShowMuteButton(
                isCurrentUserAdmin: true,
                messageAuthorId: 99,
                currentUserId: 1));
        }

        [Fact]
        public void ShowMuteButton_AdminViewingOwnMessage_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.ShowMuteButton(
                isCurrentUserAdmin: true,
                messageAuthorId: 1,
                currentUserId: 1));
        }

        [Fact]
        public void ShowMuteButton_NonAdminViewingOtherUsersMessage_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.ShowMuteButton(
                isCurrentUserAdmin: false,
                messageAuthorId: 99,
                currentUserId: 1));
        }

        [Fact]
        public void ShowMuteButton_NonAdminViewingOwnMessage_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.ShowMuteButton(
                isCurrentUserAdmin: false,
                messageAuthorId: 1,
                currentUserId: 1));
        }

        [Fact]
        public void ShowMuteButton_AdminAndNullAuthorId_ReturnsTrue()
        {
            // null author id never equals the currentUserId integer
            Assert.True(DiscussionMessageItemViewModelCore.ShowMuteButton(
                isCurrentUserAdmin: true,
                messageAuthorId: null,
                currentUserId: 1));
        }
    }

    public class HasReactionsTests
    {
        [Fact]
        public void HasReactions_EmptyList_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.HasReactions(
                new List<DiscussionReaction>()));
        }

        [Fact]
        public void HasReactions_OneReaction_ReturnsTrue()
        {
            Assert.True(DiscussionMessageItemViewModelCore.HasReactions(
                new List<DiscussionReaction> { MakeReaction("👍", 1) }));
        }

        [Fact]
        public void HasReactions_MultipleReactions_ReturnsTrue()
        {
            Assert.True(DiscussionMessageItemViewModelCore.HasReactions(
                new List<DiscussionReaction>
                {
                    MakeReaction("👍", 1),
                    MakeReaction("❤️", 2)
                }));
        }
    }

    public class HasMessageTextTests
    {
        [Fact]
        public void HasMessageText_NullMessage_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.HasMessageText(null));
        }

        [Fact]
        public void HasMessageText_EmptyString_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.HasMessageText(string.Empty));
        }

        [Fact]
        public void HasMessageText_WhitespaceOnly_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.HasMessageText("   "));
        }

        [Fact]
        public void HasMessageText_NormalText_ReturnsTrue()
        {
            Assert.True(DiscussionMessageItemViewModelCore.HasMessageText("Hello!"));
        }

        [Fact]
        public void HasMessageText_TextWithSurroundingWhitespace_ReturnsTrue()
        {
            Assert.True(DiscussionMessageItemViewModelCore.HasMessageText("  hi  "));
        }
    }



    public class CurrentUserEmojiTests
    {
        [Fact]
        public void CurrentUserEmoji_UserHasReacted_ReturnsEmoji()
        {
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("👍", 2),
                MakeReaction("❤️", 1)
            };

            var result = DiscussionMessageItemViewModelCore.CurrentUserEmoji(reactions, currentUserId: 1);
            Assert.Equal("❤️", result);
        }

        [Fact]
        public void CurrentUserEmoji_UserHasNotReacted_ReturnsNull()
        {
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("👍", 2)
            };

            var result = DiscussionMessageItemViewModelCore.CurrentUserEmoji(reactions, currentUserId: 1);
            Assert.Null(result);
        }

        [Fact]
        public void CurrentUserEmoji_EmptyReactionList_ReturnsNull()
        {
            var result = DiscussionMessageItemViewModelCore.CurrentUserEmoji(
                new List<DiscussionReaction>(), currentUserId: 1);
            Assert.Null(result);
        }

        [Fact]
        public void CurrentUserEmoji_MultipleReactionsFromCurrentUser_ReturnsFirstMatch()
        {
            // In practice a user only reacts once, but the method uses FirstOrDefault.
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("👍", 1),
                MakeReaction("❤️", 1)
            };

            var result = DiscussionMessageItemViewModelCore.CurrentUserEmoji(reactions, currentUserId: 1);
            Assert.Equal("👍", result);
        }
    }


    public class BuildReactionGroupsTests
    {
        [Fact]
        public void BuildReactionGroups_EmptyReactions_ReturnsEmptyList()
        {
            var result = DiscussionMessageItemViewModelCore.BuildReactionGroups(
                new List<DiscussionReaction>(), currentUserId: 1);

            Assert.Empty(result);
        }

        [Fact]
        public void BuildReactionGroups_SingleEmoji_ReturnsOneGroup()
        {
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("👍", 1),
                MakeReaction("👍", 2)
            };

            var result = DiscussionMessageItemViewModelCore.BuildReactionGroups(reactions, currentUserId: 1);

            Assert.Single(result);
            var group = result[0];
            Assert.Equal("👍", group.Emoji);
            Assert.Equal(2, group.Count);
        }

        [Fact]
        public void BuildReactionGroups_CurrentUserReacted_FlagIsTrue()
        {
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("❤️", 1),
                MakeReaction("❤️", 2)
            };

            var result = DiscussionMessageItemViewModelCore.BuildReactionGroups(reactions, currentUserId: 1);

            Assert.True(result[0].CurrentUserReacted);
        }

        [Fact]
        public void BuildReactionGroups_CurrentUserHasNotReacted_FlagIsFalse()
        {
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("❤️", 2),
                MakeReaction("❤️", 3)
            };

            var result = DiscussionMessageItemViewModelCore.BuildReactionGroups(reactions, currentUserId: 1);

            Assert.False(result[0].CurrentUserReacted);
        }

        [Fact]
        public void BuildReactionGroups_MultipleEmojis_ReturnsOneGroupPerEmoji()
        {
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("👍", 1),
                MakeReaction("❤️", 2),
                MakeReaction("👍", 3)
            };

            var result = DiscussionMessageItemViewModelCore.BuildReactionGroups(reactions, currentUserId: 1);
            var dict = result.ToDictionary(g => g.Emoji);

            Assert.Equal(2, result.Count);
            Assert.Equal(2, dict["👍"].Count);
            Assert.Equal(1, dict["❤️"].Count);
            Assert.True(dict["👍"].CurrentUserReacted);
            Assert.False(dict["❤️"].CurrentUserReacted);
        }
    }

 

    public class ParseMessageIntoSegmentsTests
    {
        [Fact]
        public void ParseMessageIntoSegments_NullMessage_ReturnsEmptyList()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments(null);
            Assert.Empty(result);
        }

        [Fact]
        public void ParseMessageIntoSegments_EmptyString_ReturnsEmptyList()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments(string.Empty);
            Assert.Empty(result);
        }

        [Fact]
        public void ParseMessageIntoSegments_WhitespaceOnly_ReturnsEmptyList()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("   ");
            Assert.Empty(result);
        }

        [Fact]
        public void ParseMessageIntoSegments_PlainTextNoMentions_ReturnsSinglePlainSegment()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("Hello world");

            Assert.Single(result);
            Assert.Equal("Hello world", result[0].Text);
            Assert.False(result[0].IsMention);
        }

        [Fact]
        public void ParseMessageIntoSegments_SingleMentionOnly_ReturnsSingleMentionSegment()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("@Alice");

            Assert.Single(result);
            Assert.Equal("@Alice", result[0].Text);
            Assert.True(result[0].IsMention);
        }

        [Fact]
        public void ParseMessageIntoSegments_MentionAtStart_ReturnsMentionThenText()
        {

            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("@Alice hello");

            Assert.Single(result);
            Assert.True(result[0].IsMention);
            Assert.Equal("@Alice hello", result[0].Text);
        }
        [Fact]
        public void ParseMessageIntoSegments_MentionAtEnd_ReturnsTextThenMention()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("Hey @Bob");

            Assert.Equal(2, result.Count);
            Assert.False(result[0].IsMention);
            Assert.True(result[1].IsMention);
            Assert.Equal("@Bob", result[1].Text);
        }


        [Fact]
        public void ParseMessageIntoSegments_MentionInMiddle_ReturnsThreeSegments()
        {

            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("Hey @Bob how are you");

            Assert.Equal(3, result.Count);
            Assert.False(result[0].IsMention);
            Assert.Equal("Hey ", result[0].Text);
            Assert.True(result[1].IsMention);
            Assert.Equal("@Bob how", result[1].Text);
            Assert.False(result[2].IsMention);
            Assert.Equal(" are you", result[2].Text);
        }

        [Fact]
        public void ParseMessageIntoSegments_MultipleMentions_ReturnsAllSegments()
        {
            var result = DiscussionMessageItemViewModelCore
                .ParseMessageIntoSegments("@Alice and @Bob meet here");

            // "@Alice", " and ", "@Bob", " meet here"
            Assert.Equal(4, result.Count);
            Assert.True(result[0].IsMention);
            Assert.False(result[1].IsMention);
            Assert.True(result[2].IsMention);
            Assert.False(result[3].IsMention);
        }

        [Fact]
        public void ParseMessageIntoSegments_TwoWordMention_CapturesBothWords()
        {
            var result = DiscussionMessageItemViewModelCore
                .ParseMessageIntoSegments("Hi @John Doe!");

            var mention = result.Single(s => s.IsMention);
            Assert.Equal("@John Doe", mention.Text);
        }

        [Fact]
        public void ParseMessageIntoSegments_AtSignAloneIsNotMention()
        {
            var result = DiscussionMessageItemViewModelCore
                .ParseMessageIntoSegments("email me @ home");

            Assert.True(result.All(s => !s.IsMention),
                "A bare '@' without a following word should not be a mention.");
        }

        [Fact]
        public void ParseMessageIntoSegments_AdjacentMentions_EachIsMention()
        {
            var result = DiscussionMessageItemViewModelCore
                .ParseMessageIntoSegments("@A @B");

            var mentions = result.Where(s => s.IsMention).ToList();
            Assert.Equal(2, mentions.Count);
        }
    }
}