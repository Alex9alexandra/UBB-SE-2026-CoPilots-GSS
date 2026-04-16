using System.Collections.Generic;
using System.Linq;

using Events_GSS.Data.Models;
using Events_GSS.ViewModels;
using Events_GSS.ViewModelsCore;

using Xunit;

namespace Events_GSS.Tests.ViewModels;

public class DiscussionMessageItemViewModelCoreTests
{

    private static User MakeUser(int id, string name = "someoneimportant27") =>
        new() { UserId = id, Name = name };

    private static DiscussionReaction MakeReaction(string emoji, int authorId) =>
         new()
         {
             Emoji = emoji,
             Author = MakeUser(authorId),
             Message = new DiscussionMessage(
            id: 1,
            message: "this is my message",
            date: DateTime.UtcNow)
         };

 

    public class ShowMuteButtonTests
    {
        [Fact]
        public void ShowMuteButton_AdminViewingOtherUsersMessage_MuteButtonIsVisible()
        {
            Assert.True(DiscussionMessageItemViewModelCore.ShowMuteButton(isCurrentUserAdmin: true,messageAuthorId: 99, currentUserId: 1));
        }

        [Fact]
        public void ShowMuteButton_AdminViewingOwnMessage_MuteIsNotVisible()
        {
            Assert.False(DiscussionMessageItemViewModelCore.ShowMuteButton(isCurrentUserAdmin: true, messageAuthorId: 1,currentUserId: 1));
        }

        [Fact]
        public void ShowMuteButton_NonAdminViewingOtherUsersMessage_MuteButtonIsNotVisible()
        {
            Assert.False(DiscussionMessageItemViewModelCore.ShowMuteButton(isCurrentUserAdmin: false,messageAuthorId: 99,currentUserId: 1));
        }

        [Fact]
        public void ShowMuteButton_NonAdminViewingOwnMessage_MuteButtonIsNotVisible()
        {
            Assert.False(DiscussionMessageItemViewModelCore.ShowMuteButton(isCurrentUserAdmin: false,messageAuthorId: 1,currentUserId: 1));
        }

    }

    public class HasReactionsTests
    {
        [Fact]
        public void HasReactions_ReactionsListIsEmpty_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.HasReactions(new List<DiscussionReaction>()));
        }

        [Fact]
        public void HasReactions_ReactionListHasASingleReaction_ReturnsTrue()
        {
            Assert.True(DiscussionMessageItemViewModelCore.HasReactions(new List<DiscussionReaction> { MakeReaction("👍", 1) }));
        }

        [Fact]
        public void HasReactions_ReactionListHasMultipleReactions_ReturnsTrue()
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
        public void HasMessageText_MessageIsNull_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.HasMessageText(null));
        }

        [Fact]
        public void HasMessageText_MessageIsAnEmptyString_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.HasMessageText(string.Empty));
        }

        [Fact]
        public void HasMessageText_MessageContainsWhitespaceOnly_ReturnsFalse()
        {
            Assert.False(DiscussionMessageItemViewModelCore.HasMessageText("   "));
        }

        [Fact]
        public void HasMessageText_MessageContainsANormalStringAtLast_ReturnsTrue()
        {
            Assert.True(DiscussionMessageItemViewModelCore.HasMessageText("hello im a regular message!"));
        }

        [Fact]
        public void HasMessageText_MessageContainsTextWithSurroundingWhitespace_ReturnsTrue()
        {
            Assert.True(DiscussionMessageItemViewModelCore.HasMessageText("  hi  "));
        }
    }



    public class CurrentUserEmojiTests
    {
        [Fact]
        public void CurrentUserEmoji_UserHasReactedWithOneEmoji_ReturnsEmoji()
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
        public void CurrentUserEmoji_UserHasNotReactedWithAnything_ReturnsNull()
        {
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("👍", 2)
            };

            var result = DiscussionMessageItemViewModelCore.CurrentUserEmoji(reactions, currentUserId: 1);
            Assert.Null(result);
        }

        [Fact]
        public void CurrentUserEmoji_ReactionListIsCompletelyEmpty_ReturnsNull()
        {
            var result = DiscussionMessageItemViewModelCore.CurrentUserEmoji(
                new List<DiscussionReaction>(), currentUserId: 1);
            Assert.Null(result);
        }

        [Fact]
        public void CurrentUserEmoji_MultipleReactionsFromCurrentUser_ReturnsFirstMatch()
        {
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
        public void BuildReactionGroups_ReactionListIsEmpty_ReturnsEmptyList()
        {
            var result = DiscussionMessageItemViewModelCore.BuildReactionGroups(
                new List<DiscussionReaction>(), currentUserId: 1);

            Assert.Empty(result);
        }

        [Fact]
        public void BuildReactionGroups_SingleEmojiMultipleTimes_ReturnsOneGroup()
        {
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("👍", 1),
                MakeReaction("👍", 2)
            };

            var result = DiscussionMessageItemViewModelCore.BuildReactionGroups(reactions, currentUserId: 1);
            var group = result[0];
            Assert.Equal(2, group.Count);
        }

        [Fact]
        public void BuildReactionGroups_CurrentUserReacted_FlagFromResultIsTrue()
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
        public void BuildReactionGroups_CurrentUserHasNotReacted_FlagFromResultIsFalse()
        {
            var reactions = new List<DiscussionReaction>
            {
                MakeReaction("❤️", 2),
                MakeReaction("❤️", 3)
            };

            var result = DiscussionMessageItemViewModelCore.BuildReactionGroups(reactions, currentUserId: 1);

            Assert.False(result[0].CurrentUserReacted);
        }

    }

 

    public class ParseMessageIntoSegmentsTests
    {
        [Fact]
        public void ParseMessageIntoSegments_MessageIsNull_ReturnsEmptyList()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments(null);
            Assert.Empty(result);
        }

        [Fact]
        public void ParseMessageIntoSegments_MessageIsEmptyString_ReturnsEmptyList()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments(string.Empty);
            Assert.Empty(result);
        }

        [Fact]
        public void ParseMessageIntoSegments_MessageContainsWhitespaceOnly_ReturnsEmptyList()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("   ");
            Assert.Empty(result);
        }

        [Fact]
        public void ParseMessageIntoSegments_MessageContainsPlainText_ReturnsSinglePlainSegment()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("hello world");

            Assert.Equal("hello world", result[0].Text);
        }

        [Fact]
        public void ParseMessageIntoSegments_SingleMentionOnly_ReturnsSingleMentionSegment()
        {
            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("@oliverr21");

            Assert.Equal("@oliverr21", result[0].Text);
        }

        [Fact]
        public void ParseMessageIntoSegments_MentionAtStartOfMessage_ReturnsMentionThenText()
        {

            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("@david hello");

            Assert.Equal("@david hello", result[0].Text);
        }


        [Fact]
        public void ParseMessageIntoSegments_MentionInMiddle_ReturnsThreeSegments()
        {

            var result = DiscussionMessageItemViewModelCore.ParseMessageIntoSegments("Hey @someone how are you");

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void ParseMessageIntoSegments_MultipleMentions_ReturnsAllSegments()
        {
            var result = DiscussionMessageItemViewModelCore
                .ParseMessageIntoSegments("@this and @that meet here");
            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void ParseMessageIntoSegments_TwoWordMention_BothWordsAreCapturedAsASingleMentionSegment()
        {
            var result = DiscussionMessageItemViewModelCore
                .ParseMessageIntoSegments("Hi @david popescu!");

            var mention = result.Single(s => s.IsMention);
            Assert.Equal("@david popescu", mention.Text);
        }
    }
}