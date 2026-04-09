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
        public void CanSend_WithTextOnly_ReturnsTrue()
        {
            Assert.True(DiscussionViewModelCore.CanSend(
                newMessage: "hello",
                mediaPath: null,
                isLoading: false,
                isMuted: false));
        }

        [Fact]
        public void CanSend_WithMediaOnly_ReturnsTrue()
        {
            Assert.True(DiscussionViewModelCore.CanSend(
                newMessage: null,
                mediaPath: "/tmp/photo.jpg",
                isLoading: false,
                isMuted: false));
        }

        [Fact]
        public void CanSend_WithTextAndMedia_ReturnsTrue()
        {
            Assert.True(DiscussionViewModelCore.CanSend(
                newMessage: "see attached",
                mediaPath: "/tmp/photo.jpg",
                isLoading: false,
                isMuted: false));
        }

        [Fact]
        public void CanSend_WhitespaceMessageNoMedia_ReturnsFalse()
        {
            Assert.False(DiscussionViewModelCore.CanSend(
                newMessage: "   ",
                mediaPath: null,
                isLoading: false,
                isMuted: false));
        }

        [Fact]
        public void CanSend_NullMessageNullMedia_ReturnsFalse()
        {
            Assert.False(DiscussionViewModelCore.CanSend(
                newMessage: null,
                mediaPath: null,
                isLoading: false,
                isMuted: false));
        }

        [Fact]
        public void CanSend_WhenLoading_ReturnsFalse()
        {
            Assert.False(DiscussionViewModelCore.CanSend(
                newMessage: "hello",
                mediaPath: null,
                isLoading: true,
                isMuted: false));
        }

        [Fact]
        public void CanSend_WhenMuted_ReturnsFalse()
        {
            Assert.False(DiscussionViewModelCore.CanSend(
                newMessage: "hello",
                mediaPath: null,
                isLoading: false,
                isMuted: true));
        }

        [Fact]
        public void CanSend_WhenLoadingAndMuted_ReturnsFalse()
        {
            Assert.False(DiscussionViewModelCore.CanSend(
                newMessage: "hello",
                mediaPath: null,
                isLoading: true,
                isMuted: true));
        }

        [Fact]
        public void CanSend_WhitespaceMediaPath_ReturnsFalse()
        {
            // A path that is only whitespace must not count as content.
            Assert.False(DiscussionViewModelCore.CanSend(
                newMessage: null,
                mediaPath: "   ",
                isLoading: false,
                isMuted: false));
        }
    }



    public class InsertMentionTests
    {
        [Fact]
        public void InsertMention_EmptyMessage_PrependsMention()
        {
            var result = DiscussionViewModelCore.InsertMention(string.Empty, "Alice");
            Assert.Equal("@Alice ", result);
        }

        [Fact]
        public void InsertMention_MessageEndsWithSpace_AppendsMentionWithoutExtraSpace()
        {
            var result = DiscussionViewModelCore.InsertMention("Hey ", "Bob");
            Assert.Equal("Hey @Bob ", result);
        }

        [Fact]
        public void InsertMention_MessageDoesNotEndWithSpace_InsertsSpaceBeforeMention()
        {
            var result = DiscussionViewModelCore.InsertMention("Hey", "Bob");
            Assert.Equal("Hey @Bob ", result);
        }

        [Fact]
        public void InsertMention_BlankUserName_ReturnsOriginalMessage()
        {
            var result = DiscussionViewModelCore.InsertMention("Hello", "   ");
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void InsertMention_NullUserName_ReturnsOriginalMessage()
        {
            var result = DiscussionViewModelCore.InsertMention("Hello", null!);
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void InsertMention_EmptyUserName_ReturnsOriginalMessage()
        {
            var result = DiscussionViewModelCore.InsertMention("Hello", string.Empty);
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void InsertMention_NullCurrentMessage_AppendsMention()
        {
            // null current message treated as empty by the concatenation
            var result = DiscussionViewModelCore.InsertMention(null!, "Carol");
            Assert.Equal("@Carol ", result);
        }
    }



    public class CalculateMuteExpiryTests
    {
        private static readonly DateTime Now = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void CalculateMuteExpiry_OneHour_AddsOneHour()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("1 hour", 0, 0, Now);
            Assert.Equal(Now.AddHours(1), result);
        }

        [Fact]
        public void CalculateMuteExpiry_TwentyFourHours_AddsOneDay()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("24 hours", 0, 0, Now);
            Assert.Equal(Now.AddDays(1), result);
        }

        [Fact]
        public void CalculateMuteExpiry_Custom_AddsHoursAndMinutes()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("Custom", 2.5, 45, Now);
            Assert.Equal(Now.AddHours(2.5).AddMinutes(45), result);
        }

        [Fact]
        public void CalculateMuteExpiry_Permanent_ReturnsNull()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("Permanent", 0, 0, Now);
            Assert.Null(result);
        }

        [Fact]
        public void CalculateMuteExpiry_UnknownSelection_FallsBackToThirtyMinutes()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("something else", 0, 0, Now);
            Assert.Equal(Now.AddMinutes(30), result);
        }

        [Fact]
        public void CalculateMuteExpiry_Custom_ZeroHoursAndMinutes_ReturnsSameAsNow()
        {
            var result = DiscussionViewModelCore.CalculateMuteExpiry("Custom", 0, 0, Now);
            Assert.Equal(Now, result);
        }
    }


    public class NormaliseSlowModeSecondsTests
    {
        [Fact]
        public void NormaliseSlowModeSeconds_NullInput_ReturnsNull()
        {
            Assert.Null(DiscussionViewModelCore.NormaliseSlowModeSeconds(null));
        }

        [Fact]
        public void NormaliseSlowModeSeconds_ExactInteger_ReturnsSameValue()
        {
            Assert.Equal(10, DiscussionViewModelCore.NormaliseSlowModeSeconds(10.0));
        }

        [Fact]
        public void NormaliseSlowModeSeconds_FractionBelow0Point5_RoundsDown()
        {
            Assert.Equal(5, DiscussionViewModelCore.NormaliseSlowModeSeconds(5.3));
        }

        [Fact]
        public void NormaliseSlowModeSeconds_FractionAbove0Point5_RoundsUp()
        {
            Assert.Equal(6, DiscussionViewModelCore.NormaliseSlowModeSeconds(5.7));
        }

        [Fact]
        public void NormaliseSlowModeSeconds_MidpointValue_UsesRoundHalfToEven()
        {
            // Math.Round uses banker's rounding by default:  2.5 → 2 (even), 3.5 → 4 (even)
            Assert.Equal(2, DiscussionViewModelCore.NormaliseSlowModeSeconds(2.5));
            Assert.Equal(4, DiscussionViewModelCore.NormaliseSlowModeSeconds(3.5));
        }

        [Fact]
        public void NormaliseSlowModeSeconds_Zero_ReturnsZero()
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
        public void TryParseSlowModeSeconds_MessageWithLeadingNumber_ReturnsFirstNumber()
        {
            // Only the FIRST integer is extracted.
            var result = DiscussionViewModelCore.TryParseSlowModeSeconds("5 seconds remaining");
            Assert.Equal(5, result);
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
        [InlineData("Slow mode active")]
        [InlineData("Unauthorized")]
        [InlineData("")]
        public void IsMuteException_WhenMessageDoesNotContainMuted_ReturnsFalse(string message)
        {
            Assert.False(DiscussionViewModelCore.IsMuteException(message));
        }
    }


    public class IsSlowModeExceptionTests
    {
        [Theory]
        [InlineData("Slow mode: wait 10 seconds")]
        [InlineData("slow mode enabled")]        // case-insensitive
        [InlineData("SLOW MODE restriction")]
        public void IsSlowModeException_WhenMessageContainsSlowMode_ReturnsTrue(string message)
        {   
            Assert.True(DiscussionViewModelCore.IsSlowModeException(message));
        }

        [Theory]
        [InlineData("You are muted")]
        [InlineData("Unauthorized action")]
        [InlineData("")]
        public void IsSlowModeException_WhenMessageDoesNotContainSlowMode_ReturnsFalse(string message)
        {
            Assert.False(DiscussionViewModelCore.IsSlowModeException(message));
        }
    }
}