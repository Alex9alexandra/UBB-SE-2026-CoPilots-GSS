using System;
using System.Collections.Generic;

using Events_GSS.ViewModels;
using Events_GSS.ViewModelsCore;

using Xunit;

namespace Events_GSS.Tests.ViewModels;

public class DiscussionViewModelCoreTests
{
 

    public class CanSendTests
    {
        [Fact]
        public void CanSend_UserSendsMessageWithTextOnly_MessageIsSentSuccessfully ()
        {
            Assert.True(DiscussionViewModelCore.CanSend(newMessage: "hello",mediaPath: null,isLoading: false, isMuted: false));
        }

        [Fact]
        public void CanSend_UserSendsMessageWithMediaOnly_MessageIsSentSuccessfully()
        {
            Assert.True(DiscussionViewModelCore.CanSend(newMessage: null,mediaPath: "/tmp/photo.jpg",isLoading: false, isMuted: false));
        }

        [Fact]
        public void CanSend_UserSendsMessageWithBothTextAndMedia_MessageIsSentSuccessfully()
        {
            Assert.True(DiscussionViewModelCore.CanSend(newMessage: "check this", mediaPath: "/tmp/photo.jpg", isLoading: false, isMuted: false));
        }

        [Fact]
        public void CanSend_UserSendsMessageWithOnlyWhitespaces_MessageIsNotSent()
        {
            Assert.False(DiscussionViewModelCore.CanSend(newMessage: "   ",mediaPath: null, isLoading: false, isMuted: false));
        }

        [Fact]
        public void CanSend_UserSendsMessageWithNullTextAndNullMedia_MessageIsNotSent()
        {
            Assert.False(DiscussionViewModelCore.CanSend(newMessage: null,mediaPath: null,isLoading: false,isMuted: false));
        }

        [Fact]
        public void CanSend_UserSendsMessageDuringLoading_MessageIsNotSent()
        {
            Assert.False(DiscussionViewModelCore.CanSend(newMessage: "hello", mediaPath: null,isLoading: true, isMuted: false));
        }

        [Fact]
        public void CanSend_UserTriesToSendMessageWhileMuted_MessageIsNotSent()
        {
            Assert.False(DiscussionViewModelCore.CanSend(newMessage: "hello",mediaPath: null,isLoading: false, isMuted: true));
        }

        [Fact]
        public void CanSend_UserTriesToSendMessageWhileMutedAndDuringLoading_MessageIsNotSent()
        {
            Assert.False(DiscussionViewModelCore.CanSend(newMessage: "hello", mediaPath: null, isLoading: true, isMuted: true));
        }

        [Fact]
        public void CanSend_UserSendsMessageWithMediaPathContainingWhiteSpaces_MessageIsNotSent()
        {
            Assert.False(DiscussionViewModelCore.CanSend(newMessage: null,mediaPath: "   ",isLoading: false, isMuted: false));
        }
    }


    public class InsertMentionTests
    {
        [Fact]
        public void InsertMention_UserMentionsWithEmptyMessage_PrependsMention()
        {
            var result = DiscussionViewModelCore.InsertMention(string.Empty, "David");
            Assert.Equal("@David ", result);
        }

        [Fact]
        public void InsertMention_UserSendsMessageWithTrailingWhitespaceBeforeMentioning_DoesNotAppendAnotherWhitespace()
        {
            var result = DiscussionViewModelCore.InsertMention("Hey ", "David");
            Assert.Equal("Hey @David ", result);
        }

        [Fact]
        public void InsertMention_UserSendsMessageWithoutTrailingWhitespaceBeforeMentioning_AppendsWhitespaceBeforeMention()
        {
            var result = DiscussionViewModelCore.InsertMention("Hey", "David");
            Assert.Equal("Hey @David ", result);
        }

        [Fact]
        public void InsertMention_UserAttemptsToMentionAnEmptyString_OriginalMessageIsSent()
        {
            var result = DiscussionViewModelCore.InsertMention("Hello", "   ");
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void InsertMention_UserTriesToMentionNullUser_OriginalMessageIsSent()
        {
            var result = DiscussionViewModelCore.InsertMention("Hello", null!);
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void InsertMention_UserTriesToMentionEmptyString_OriginalMessageIsSent()
        {
            var result = DiscussionViewModelCore.InsertMention("Hello", string.Empty);
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void InsertMention_UserSendsNullMessageWithValidMention_MentionIsAppendedToTheNullMessage()
        {
            var result = DiscussionViewModelCore.InsertMention(null!, "Isa");
            Assert.Equal("@Isa ", result);
        }
    }


    public class CalculateMuteExpiryTests
    {
        private static readonly DateTime Now = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void CalculateMuteExpiry_MuteForOneHour_MuteIsExtendedByAnHour()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("1 hour", 0, 0, Now);
            Assert.Equal(Now.AddHours(1), result);
        }

        [Fact]
        public void CalculateMuteExpiry_MuteForTwentyFourHours_MuteIsExtendedByAFullDay()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("24 hours", 0, 0, Now);
            Assert.Equal(Now.AddDays(1), result);
        }

        [Fact]
        public void CalculateMuteExpiry_MuteForCustomAmountOfTime_MuteIsExtendedAccordingly()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("Custom", 2.5, 45, Now);
            Assert.Equal(Now.AddHours(2.5).AddMinutes(45), result);
        }

        [Fact]
        public void CalculateMuteExpiry_MutePermanently_MuteIsNullSinceItWillNeverExpire()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("Permanent", 0, 0, Now);
            Assert.Null(result);
        }

        [Fact]
        public void CalculateMuteExpiry_MuteForUnknownExpiry_FallsBackToThirtyMinutesDefault()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("something", 0, 0, Now);
            Assert.Equal(Now.AddMinutes(30), result);
        }

        [Fact]
        public void CalculateMuteExpiry_MuteForZeroHoursAndMinutes_DoesntExtendMute()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("Custom", 0, 0, Now);
            Assert.Equal(Now, result);
        }
    }


    public class NormaliseSlowModeSecondsTests
    {
        [Fact]
        public void NormaliseSlowModeSeconds_InputIsNull_ReturnsNull()
        {
            Assert.Null(DiscussionViewModelCore.NormaliseSlowModeSeconds(null));
        }

        [Fact]
        public void NormaliseSlowModeSeconds_ExactInteger_ReturnsSameValue()
        {
            Assert.Equal(10, DiscussionViewModelCore.NormaliseSlowModeSeconds(10.0));
        }

        [Fact]
        public void NormaliseSlowModeSeconds_FractionBelowPointFive_RoundsDown()
        {
            Assert.Equal(5, DiscussionViewModelCore.NormaliseSlowModeSeconds(5.3));
        }

        [Fact]
        public void NormaliseSlowModeSeconds_FractionAbovePointFive_RoundsUp()
        {
            Assert.Equal(6, DiscussionViewModelCore.NormaliseSlowModeSeconds(5.7));
        }


        [Fact]
        public void NormaliseSlowModeSeconds_ZeroSeconds_ReturnsZero()
        {
            Assert.Equal(0, DiscussionViewModelCore.NormaliseSlowModeSeconds(0.0));
        }
    }



    public class TryParseSlowModeSecondsTests
    {
        [Fact]
        public void TryParseSlowModeSeconds_MessageWithNumber_ReturnsNumber()
        {
            var result = DiscussionViewModelCore.TryParseSlowModeSeconds("Slow mode: wait 42 seconds");
            Assert.Equal(42, result);
        }

        [Fact]
        public void TryParseSlowModeSeconds_NoNumberInMessage_ReturnsNull()
        {
            var result = DiscussionViewModelCore.TryParseSlowModeSeconds("Slow mode active");
            Assert.Null(result);
        }

        [Fact]
        public void TryParseSlowModeSeconds_EmptyString_ReturnsNull()
        {
            var result = DiscussionViewModelCore.TryParseSlowModeSeconds(string.Empty);
            Assert.Null(result);
        }

        [Fact]
        public void TryParseSlowModeSeconds_MultipleNumbers_ReturnsFirstOne()
        {
            var result = DiscussionViewModelCore.TryParseSlowModeSeconds("Wait 10 or 20 seconds");
            Assert.Equal(10, result);
        }
    }



    public class IsMuteExceptionTests
    {
        [Theory]
        [InlineData("You are muted")]
        [InlineData("User is MUTED until tomorrow")]
        [InlineData("muted")]
        public void IsMuteException_WhenMessageContainsMuted_ReturnsTrue(string message)
        {
            Assert.True(DiscussionViewModelCore.IsMuteException(message));
        }

        [Theory]
        [InlineData("Something something active")]
        [InlineData("No M*ted here!")]
        public void IsMuteException_WhenMessageDoesNotContainMuted_ReturnsFalse(string message)
        {
            Assert.False(DiscussionViewModelCore.IsMuteException(message));
        }
    }


    public class IsSlowModeExceptionTests
    {
        [Theory]
        [InlineData("Slow mode: wait 10 seconds")]
        [InlineData("SLOW MODE restriction")]
        public void IsSlowModeException_WhenMessageContainsSlowMode_ReturnsTrue(string message)
        {   
            Assert.True(DiscussionViewModelCore.IsSlowModeException(message));
        }

        [Theory]
        [InlineData("You are done for...")]
        [InlineData("no slowmode here")]
        public void IsSlowModeException_WhenMessageDoesNotContainSlowMode_ReturnsFalse(string message)
        {
            Assert.False(DiscussionViewModelCore.IsSlowModeException(message));
        }
    }
}