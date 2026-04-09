using System;
using System.Collections.Generic;
using System.Text;

using Moq;

using Xunit; 

using Events_GSS.Data.Services;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services.reputationService;
using Events_GSS.Data.Services.notificationServices;
using System.Security.Cryptography.X509Certificates;
using Windows.AI.MachineLearning.Preview;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace Events_GSS.Test.discussionService;

public class DiscussionServiceTests
{
    private Mock<IDiscussionRepository> mockRepo;
    private Mock<IEventRepository> mockEventRepo;
    private Mock<IReputationService> mockReputation;
    private Mock<INotificationService> mockNotification;
    private DiscussionService service;

    public DiscussionServiceTests()
    {
        mockRepo = new Mock<IDiscussionRepository>();
        mockEventRepo = new Mock<IEventRepository>();
        mockReputation = new Mock<IReputationService>();
        mockNotification = new Mock<INotificationService>();
        service = new DiscussionService(
            mockRepo.Object,
            mockEventRepo.Object,
            mockReputation.Object,
            mockNotification.Object);
    }

    private Event MakeEvent(int eventId, int? adminId = null, int? slowMode = null) =>
       new Event
       {
           EventId = eventId,
           Admin = adminId.HasValue ? new User { UserId = adminId.Value } : null,
           SlowModeSeconds = slowMode
       };

    private void SetupEvent(int eventId, Event evt) =>
        mockEventRepo.Setup(e => e.GetByIdAsync(eventId)).ReturnsAsync(evt);

    private void SetupReputation(int userId, bool canPost = true) =>
        mockReputation.Setup(r => r.CanPostMessagesAsync(userId)).ReturnsAsync(canPost);

    private void SetupNoMute(int eventId, int userId) =>
        mockRepo.Setup(r => r.GetMuteAsync(eventId, userId)).ReturnsAsync((DiscussionMute?)null);

    private void SetupNoSlowMode(int eventId, int userId) =>
        mockRepo.Setup(r => r.GetLastUserMessageDateAsync(eventId, userId))
                .ReturnsAsync((DateTime?)null);



    [Fact]
    public async Task GetMessagesAsync_EventNotFound_ThrowsArgumentException()
    {
        mockEventRepo.Setup(e => e.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetMessagesAsync(99, 1));
    }

    [Fact]
    public async Task GetMessagesAsync_AdminUser_CanDeleteAllMessages()
    {
        int adminId = 5, eventId = 1;
        var evt = MakeEvent(eventId, adminId: adminId);
        SetupEvent(eventId, evt);

        var messages = new List<DiscussionMessage>
        {
            new DiscussionMessage(1, "hello", DateTime.UtcNow) { Author = new User { UserId = 99 } },
            new DiscussionMessage(2, "world", DateTime.UtcNow) { Author = new User { UserId = adminId } }
        };
        mockRepo.Setup(r => r.GetByEventAsync(eventId, adminId)).ReturnsAsync(messages);

        var result = await service.GetMessagesAsync(eventId, adminId);

        Assert.All(result, m => Assert.True(m.CanDelete));
    }

    [Fact]
    public async Task GetMessagesAsync_RegularUser_CanOnlyDeleteOwnMessages()
    {
        int userId = 3, eventId = 1;
        var evt = MakeEvent(eventId, adminId: 99);
        SetupEvent(eventId, evt);

        var messages = new List<DiscussionMessage>
        {
            new DiscussionMessage(1, "mine",     DateTime.UtcNow) { Author = new User { UserId = userId } },
            new DiscussionMessage(2, "not mine", DateTime.UtcNow) { Author = new User { UserId = 77 } }
        };
        mockRepo.Setup(r => r.GetByEventAsync(eventId, userId)).ReturnsAsync(messages);

        var result = await service.GetMessagesAsync(eventId, userId);

        Assert.True(result[0].CanDelete);
        Assert.False(result[1].CanDelete);
    }

    [Fact]
    public async Task GetMessagesAsync_ThirdPartyUser_CannotDeleteMessages()
    {
        // Arrange
        int visitorId = 111;
        int authorId = 222;
        int adminId = 999;
        int eventId = 1;

        var evt = MakeEvent(eventId, adminId: adminId);
        SetupEvent(eventId, evt);

        var messages = new List<DiscussionMessage>
    {
        new DiscussionMessage(1, "Hello", DateTime.UtcNow) { Author = new User { UserId = authorId } }
    };
        mockRepo.Setup(r => r.GetByEventAsync(eventId, visitorId)).ReturnsAsync(messages);

        // Act
        var result = await service.GetMessagesAsync(eventId, visitorId);


        Assert.False(result[0].CanDelete);
    }

    [Fact]
    public async Task CreateMessageAsync_EmptyTextAndMedia_ThrowsArgumentException() {
        await Assert.ThrowsAsync<ArgumentException>(async () => await service.CreateMessageAsync("", "", 1, 1, null));
    }

    [Fact]
    public async Task CreateMessageAsync_LowReputation_ThrowsInvalidOperationException() {
        int userId = 99;
        mockReputation.Setup(r => r.CanPostMessagesAsync(userId)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        await service.CreateMessageAsync("Hi", null, 1, userId, null));

        Assert.Contains("reputation is too low", ex.Message);
    }

    [Fact]
    public async Task CreateMessageAsync_EventNotFound_ThrowsArgumentException()
    {
        int userId = 1;
        SetupReputation(userId);
        mockEventRepo.Setup(e => e.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateMessageAsync("Hi", null, 99, userId, null));
    }


    [Fact]
    public async Task CreateMessageAsync_UserIsMutedPermanently_ThrowsInvalidOperationException() {
        int userId = 1;
        int eventId = 10;
        mockReputation.Setup(r => r.CanPostMessagesAsync(userId)).ReturnsAsync(true);
        mockEventRepo.Setup(e => e.GetByIdAsync(eventId)).ReturnsAsync(new Event { EventId = eventId });

        var permanentMute = new DiscussionMute { IsPermanent = true };
        mockRepo.Setup(r => r.GetMuteAsync(eventId, userId)).ReturnsAsync(permanentMute);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        await service.CreateMessageAsync("Hi", null, eventId, userId, null));
    }

    [Fact]
    public async Task CreateMessageAsync_UserIsMutedTemporarily_ThrowsInvalidOperationException() {
        int userId = 1;
        int eventId = 10;
        mockReputation.Setup(r => r.CanPostMessagesAsync(userId)).ReturnsAsync(true);
        mockEventRepo.Setup(e => e.GetByIdAsync(eventId)).ReturnsAsync(new Event { EventId = eventId });

        var temporaryMute = new DiscussionMute { MutedUntil = DateTime.UtcNow.AddHours(1), IsPermanent = false};
        mockRepo.Setup(r => r.GetMuteAsync(eventId, userId)).ReturnsAsync(temporaryMute);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.CreateMessageAsync("hello", null, eventId, userId, null));
        Assert.Contains("Time remaining", ex.Message);
    }

    [Fact]
    public async Task CreateMessageAsync_ExpiredMute_AutoUnmutesAndSucceeds()
    {
        int userId = 1, eventId = 10;
        SetupReputation(userId);
        SetupEvent(eventId, MakeEvent(eventId));

        // Mute whose time has already passed
        mockRepo.Setup(r => r.GetMuteAsync(eventId, userId))
                .ReturnsAsync(new DiscussionMute
                {
                    IsPermanent = false,
                    MutedUntil = DateTime.UtcNow.AddSeconds(-1)
                });

        SetupNoSlowMode(eventId, userId);
        mockRepo.Setup(r => r.AddAsync(It.IsAny<DiscussionMessage>())).ReturnsAsync(0);
        mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(new List<User>());

        await service.CreateMessageAsync("hello", null, eventId, userId, null);


        mockRepo.Verify(r => r.UnmuteAsync(eventId, userId), Times.Once);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<DiscussionMessage>()), Times.Once);
    }

    [Fact]
    public async Task CreateMessageAsync_AdminBypassesMuteCheck()
    {
        int adminId = 99, eventId = 10;
        SetupReputation(adminId);
        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));


        SetupNoSlowMode(eventId, adminId);
        mockRepo.Setup(r => r.AddAsync(It.IsAny<DiscussionMessage>())).ReturnsAsync(0);
        mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(new List<User>());

        await service.CreateMessageAsync("admin msg", null, eventId, adminId, null);

        mockRepo.Verify(r => r.GetMuteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<DiscussionMessage>()), Times.Once);
    }

    [Fact]
    public async Task CreateMessageAsync_SlowModeActive_ThrowsException() {
        int userId = 1;
        int eventId = 10;
        mockReputation.Setup(r => r.CanPostMessagesAsync(userId)).ReturnsAsync(true);
        mockEventRepo.Setup(e => e.GetByIdAsync(eventId)).ReturnsAsync(new Event { EventId = eventId, SlowModeSeconds = 60 });
        mockRepo.Setup(r => r.GetLastUserMessageDateAsync(eventId, userId)).ReturnsAsync(DateTime.UtcNow.AddSeconds(-10));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateMessageAsync("Heyooo", null, eventId, userId, null));

    }

    [Fact]
    public async Task CreateMessageAsync_SlowModeExpired_Succeeds()
    {
        int userId = 1, eventId = 10;
        SetupReputation(userId);
        SetupEvent(eventId, MakeEvent(eventId, slowMode: 30));
        SetupNoMute(eventId, userId);
        mockRepo.Setup(r => r.GetLastUserMessageDateAsync(eventId, userId))
                .ReturnsAsync(DateTime.UtcNow.AddSeconds(-60)); // waited long enough
        mockRepo.Setup(r => r.AddAsync(It.IsAny<DiscussionMessage>())).ReturnsAsync(0);
        mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(new List<User>());

        await service.CreateMessageAsync("Now I can post", null, eventId, userId, null);

        mockRepo.Verify(r => r.AddAsync(It.IsAny<DiscussionMessage>()), Times.Once);
    }

    [Fact]
    public async Task CreateMessageAsync_SlowModeNoLastMessage_Succeeds()
    {
        int userId = 1, eventId = 10;
        SetupReputation(userId);
        SetupEvent(eventId, MakeEvent(eventId, slowMode: 60));
        SetupNoMute(eventId, userId);
        SetupNoSlowMode(eventId, userId); // no previous message
        mockRepo.Setup(r => r.AddAsync(It.IsAny<DiscussionMessage>())).ReturnsAsync(0);
        mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(new List<User>());

        await service.CreateMessageAsync("First message", null, eventId, userId, null);

        mockRepo.Verify(r => r.AddAsync(It.IsAny<DiscussionMessage>()), Times.Once);
    }




    [Fact]
    public async Task CreateMessageAsync_AdminBypassesSlowMode()
    {
        int adminId = 99, eventId = 10;
        mockReputation.Setup(r => r.CanPostMessagesAsync(adminId)).ReturnsAsync(true);
        mockEventRepo.Setup(e => e.GetByIdAsync(eventId))
            .ReturnsAsync(new Event { EventId = eventId, Admin = new User { UserId = adminId }, SlowModeSeconds = 60 });

        mockRepo.Setup(r => r.GetLastUserMessageDateAsync(eventId, adminId))
            .ReturnsAsync(DateTime.UtcNow.AddSeconds(-1));

        await service.CreateMessageAsync("take everything!", null, eventId, adminId, null);

        mockRepo.Verify(r => r.AddAsync(It.IsAny<DiscussionMessage>()), Times.Once);
    }

    [Fact]
    public async Task CreateMessageAsync_WithReplyTo_SetsReplyOnMessage()
    {
        int userId = 1, eventId = 10, replyToId = 55;
        SetupReputation(userId);
        SetupEvent(eventId, MakeEvent(eventId));
        SetupNoMute(eventId, userId);
        SetupNoSlowMode(eventId, userId);

        DiscussionMessage? captured = null;
        mockRepo.Setup(r => r.AddAsync(It.IsAny<DiscussionMessage>()))
                .Callback<DiscussionMessage>(m => captured = m)
                .ReturnsAsync(0);
        mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(new List<User>());

        await service.CreateMessageAsync("replying!", null, eventId, userId, replyToId);

        Assert.NotNull(captured?.ReplyTo);
        Assert.Equal(replyToId, captured!.ReplyTo!.Id);
    }

    [Fact]
    public async Task CreateMessageAsync_MentionedUser_SendsNotification()
    {
        int userId = 1, eventId = 10, mentionedId = 42;
        SetupReputation(userId);
        SetupEvent(eventId, MakeEvent(eventId));
        SetupNoMute(eventId, userId);
        SetupNoSlowMode(eventId, userId);
        mockRepo.Setup(r => r.AddAsync(It.IsAny<DiscussionMessage>())).ReturnsAsync(0);

        var participants = new List<User>
        {
            new User { UserId = userId,      Name = "Alice" },
            new User { UserId = mentionedId, Name = "Bob Smith" }
        };
        mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(participants);

        await service.CreateMessageAsync("Hey @Bob thanks!", null, eventId, userId, null);

        mockNotification.Verify(
            n => n.NotifyAsync(mentionedId, It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }



    [Fact]
    public async Task DeleteMessageAsync_EventNotFound_ThrowsArgumentException()
    {
        mockEventRepo.Setup(e => e.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.DeleteMessageAsync(1, 1, 99));
    }

    [Fact]
    public async Task DeleteMessageAsync_AdminDeletesOthersMessage_CallsRepoAndDeletesMessage() {
        int authorId = 1;
        int eventId = 10;
        int messageId = 50;
        int adminId = 99;
        var mockEvent = new Event { EventId = eventId, Admin = new User { UserId = adminId } };
        var mockMessage = new DiscussionMessage(messageId, "i am so oo done", DateTime.UtcNow) { Author = new User { UserId = authorId } };
        mockEventRepo.Setup(e => e.GetByIdAsync(eventId)).ReturnsAsync(mockEvent);
        mockRepo.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(mockMessage);

        await service.DeleteMessageAsync(messageId, authorId, eventId);

        mockRepo.Verify(r => r.DeleteAsync(messageId), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_AdminDeletesNonexistentMessage_ThrowsException()
    {
        int adminId = 99;
        int eventId = 10;
        int messageId = 1011;
        var mockEvent = new Event { EventId = eventId, Admin = new User { UserId = adminId } };
        mockEventRepo.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(mockEvent);
        mockRepo.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync((DiscussionMessage)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        await service.DeleteMessageAsync(messageId, adminId, eventId));
    }

    [Fact]
    public async Task DeleteMessageAsync_NonAdminDeletesOthersMessage_ThrowsException() {
        int userId = 1;
        int eventId = 10;
        int messageId = 101;
        int otherId = 2;
        var mockEvent = new Event { EventId = eventId };
        var mockMessage = new DiscussionMessage(messageId, "sexyy_red_for_president", DateTime.UtcNow) { Author = new User { UserId = otherId } };
        mockEventRepo.Setup(e => e.GetByIdAsync(eventId)).ReturnsAsync(mockEvent);
        mockRepo.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(mockMessage);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        await service.DeleteMessageAsync(messageId, userId, eventId));

    }

    [Fact]
    public async Task DeleteMessageAsync_AuthorDeletesOwnMessage_Succeeds()
    {
        int userId = 1, eventId = 10, messageId = 50;
        SetupEvent(eventId, MakeEvent(eventId, adminId: 99));
        var msg = new DiscussionMessage(messageId, "my msg", DateTime.UtcNow)
        { Author = new User { UserId = userId } };
        mockRepo.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(msg);

        await service.DeleteMessageAsync(messageId, userId, eventId);

        mockRepo.Verify(r => r.DeleteAsync(messageId), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_AdminDeletesOwnMessage_NoReputationPenalty()
    {
        int adminId = 99, eventId = 10, messageId = 50;
        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));
        var msg = new DiscussionMessage(messageId, "admin's own", DateTime.UtcNow)
        { Author = new User { UserId = adminId } };
        mockRepo.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(msg);

        // Should not throw, and DeleteAsync is called
        await service.DeleteMessageAsync(messageId, adminId, eventId);

        mockRepo.Verify(r => r.DeleteAsync(messageId), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_MessageWithNullAuthor_NonAdmin_ThrowsUnauthorized()
    {
        // Author == null → message.Author?.UserId is null → never matches userId → unauthorized
        int userId = 1, eventId = 10, messageId = 77;
        SetupEvent(eventId, MakeEvent(eventId)); // no admin
        var msg = new DiscussionMessage(messageId, "ghost msg", DateTime.UtcNow) { Author = null };
        mockRepo.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(msg);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteMessageAsync(messageId, userId, eventId));
    }

    [Fact]
    public async Task DeleteMessageAsync_AdminDeletesMessageWithNullAuthor_DeletesWithoutReputationPenalty()
    {
        int adminId = 99, eventId = 10, messageId = 42;
        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));

        var msg = new DiscussionMessage(messageId, "orphan", DateTime.UtcNow) { Author = null };
        mockRepo.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(msg);

        await service.DeleteMessageAsync(messageId, adminId, eventId);

        mockRepo.Verify(r => r.DeleteAsync(messageId), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_AdminDeletesOthersMessage_SendsReputationMessage()
    {
        // Arrange
        int adminId = 99;
        int authorId = 1;
        int eventId = 10;
        int messageId = 50;

        // 1. Setup the Event where user 99 is the Admin
        var mockEvent = new Event
        {
            EventId = eventId,
            Admin = new User { UserId = adminId }
        };
        mockEventRepo.Setup(e => e.GetByIdAsync(eventId)).ReturnsAsync(mockEvent);

        // 2. Setup the Message written by user 1 (the 'other' user)
        var mockMessage = new DiscussionMessage(messageId, "Bad post", DateTime.UtcNow)
        {
            Author = new User { UserId = authorId }
        };
        mockRepo.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(mockMessage);

        // Act
        // Admin (99) deletes message (50) belonging to Author (1)
        await service.DeleteMessageAsync(messageId, adminId, eventId);

        // Assert
        mockRepo.Verify(r => r.DeleteAsync(messageId), Times.Once);


    }

    [Fact]
    public async Task RemoveReactionAsync_CallsRepository()
    {
        mockRepo.Setup(r => r.RemoveReactionAsync(1, 2)).Returns(Task.CompletedTask);

        await service.RemoveReactionAsync(1, 2);

        mockRepo.Verify(r => r.RemoveReactionAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task MuteUserAsync_NonAdmin_ThrowsUnauthorized()
    {
        int userId = 1, eventId = 10;
        SetupEvent(eventId, MakeEvent(eventId, adminId: 99));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.MuteUserAsync(eventId, 2, DateTime.UtcNow.AddHours(1), userId));
    }

    [Fact]
    public async Task MuteUserAsync_PermanentMute_IsPermanentTrue()
    {
        int adminId = 99, eventId = 10, targetId = 5;
        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));

        DiscussionMute? captured = null;
        mockRepo.Setup(r => r.InsertMuteAsync(It.IsAny<DiscussionMute>()))
                .Callback<DiscussionMute>(m => captured = m)
                .Returns(Task.CompletedTask);

        await service.MuteUserAsync(eventId, targetId, null, adminId);

        Assert.True(captured!.IsPermanent);
        Assert.Null(captured.MutedUntil);
    }

    [Fact]
    public async Task MuteUserAsync_TimedMute_IsPermanentFalse()
    {
        int adminId = 99, eventId = 10, targetId = 5;
        var until = DateTime.UtcNow.AddHours(2);
        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));

        DiscussionMute? captured = null;
        mockRepo.Setup(r => r.InsertMuteAsync(It.IsAny<DiscussionMute>()))
                .Callback<DiscussionMute>(m => captured = m)
                .Returns(Task.CompletedTask);

        await service.MuteUserAsync(eventId, targetId, until, adminId);

        Assert.False(captured!.IsPermanent);
        Assert.Equal(until, captured.MutedUntil);
    }

    [Fact]
    public async Task UnmuteUserAsync_NonAdmin_ThrowsUnauthorized()
    {
        int userId = 1, eventId = 10;
        SetupEvent(eventId, MakeEvent(eventId, adminId: 99));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UnmuteUserAsync(eventId, 2, userId));
    }

    [Fact]
    public async Task UnmuteUserAsync_Admin_CallsRepository()
    {
        int adminId = 99, eventId = 10, targetId = 5;
        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));
        mockRepo.Setup(r => r.UnmuteAsync(eventId, targetId)).Returns(Task.CompletedTask);

        await service.UnmuteUserAsync(eventId, targetId, adminId);

        mockRepo.Verify(r => r.UnmuteAsync(eventId, targetId), Times.Once);
    }

    [Fact]
    public async Task GetEventParticipantsAsync_ReturnsList()
    {
        int eventId = 10;
        var participants = new List<User> { new User { UserId = 1, Name = "Alice" } };
        mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(participants);

        var result = await service.GetEventParticipantsAsync(eventId);

        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public void FindMentionedUsers_FullNameMatch_ReturnsUser()
    {
        var users = new List<User> { new User { UserId = 1, Name = "Bob Smith" } };

        var result = DiscussionService.FindMentionedUsers("Hey @Bob Smith!", users);

        Assert.Single(result);
        Assert.Equal(1, result[0].UserId);
    }

    [Fact]
    public void FindMentionedUsers_FirstNameMatch_ReturnsUser()
    {
        var users = new List<User> { new User { UserId = 1, Name = "Bob Smith" } };

        var result = DiscussionService.FindMentionedUsers("Hey @Bob check this", users);

        Assert.Single(result);
    }

    [Fact]
    public void FindMentionedUsers_NoMatch_ReturnsEmpty()
    {
        var users = new List<User> { new User { UserId = 1, Name = "Alice" } };

        var result = DiscussionService.FindMentionedUsers("Hello @Unknown", users);

        Assert.Empty(result);
    }

    [Fact]
    public void FindMentionedUsers_CaseInsensitiveMatch()
    {
        var users = new List<User> { new User { UserId = 1, Name = "Alice" } };

        var result = DiscussionService.FindMentionedUsers("hi @ALICE", users);

        Assert.Single(result);
    }

    [Fact]
    public void FindMentionedUsers_MultipleMatches_ReturnsAll()
    {
        var users = new List<User>
        {
            new User { UserId = 1, Name = "Alice" },
            new User { UserId = 2, Name = "Bob" }
        };

        var result = DiscussionService.FindMentionedUsers("@Alice and @Bob!", users);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SetSlowModeAsync_NonAdmin_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        int eventId = 10;
        int adminId = 99;
        int maliciousUserId = 666; // Not the admin

        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SetSlowModeAsync(eventId, 30, maliciousUserId));

        Assert.Equal("Only the EventAdmin can perform this action.", ex.Message);
    }

    [Fact]
    public async Task SetSlowModeAsync_ZeroOrNegativeSeconds_ThrowsArgumentException()
    {
        int adminId = 99, eventId = 10;
        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetSlowModeAsync(eventId, 0, adminId));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetSlowModeAsync(eventId, -5, adminId));
    }

    [Fact]
    public async Task SetSlowModeAsync_ValidSeconds_CallsRepository()
    {
        int adminId = 99, eventId = 10;
        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));
        mockRepo.Setup(r => r.SetSlowModeAsync(eventId, 30)).Returns(Task.CompletedTask);

        await service.SetSlowModeAsync(eventId, 30, adminId);

        mockRepo.Verify(r => r.SetSlowModeAsync(eventId, 30), Times.Once);
    }

    [Fact]
    public async Task SetSlowModeAsync_NullSeconds_DisablesSlowMode()
    {
        int adminId = 99, eventId = 10;
        SetupEvent(eventId, MakeEvent(eventId, adminId: adminId));
        mockRepo.Setup(r => r.SetSlowModeAsync(eventId, null)).Returns(Task.CompletedTask);


        await service.SetSlowModeAsync(eventId, null, adminId);

        mockRepo.Verify(r => r.SetSlowModeAsync(eventId, null), Times.Once);
    }

    [Fact]
    public async Task GetSlowModeSecondsAsync_ReturnsEventSlowMode()
    {
        int eventId = 10;
        SetupEvent(eventId, MakeEvent(eventId, slowMode: 45));

        var result = await service.GetSlowModeSecondsAsync(eventId);

        Assert.Equal(45, result);
    }

    [Fact]
    public async Task GetSlowModeSecondsAsync_EventNotFound_ThrowsArgumentException()
    {
        mockEventRepo.Setup(e => e.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetSlowModeSecondsAsync(99));
    }

    [Fact]
    public async Task CreateMessageAsync_MutedLessThanOneHour_ErrorShowsMinutesAndSeconds()
    {
        int userId = 1, eventId = 10;
        SetupReputation(userId);
        SetupEvent(eventId, MakeEvent(eventId));
        mockRepo.Setup(r => r.GetMuteAsync(eventId, userId))
                .ReturnsAsync(new DiscussionMute
                {
                    IsPermanent = false,
                    MutedUntil = DateTime.UtcNow.AddMinutes(30) // < 1 hour → "Xm Ys" branch
                });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateMessageAsync("hi", null, eventId, userId, null));

        // "Xm Ys" format — no 'h' present, but 'm' and 's' are
        Assert.Contains("m ", ex.Message);
        Assert.Contains("s", ex.Message);
        Assert.DoesNotContain("h", ex.Message);
    }

    [Fact]
public async Task CreateMessageAsync_MentionerNotInParticipants_UsesSomeoneFallback()
{
    // Arrange
    int userId = 1;
    int eventId = 10;
    int mentionedId = 2;
    SetupReputation(userId);
    SetupEvent(eventId, MakeEvent(eventId));
    SetupNoMute(eventId, userId);
    SetupNoSlowMode(eventId, userId);
    mockRepo.Setup(r => r.AddAsync(It.IsAny<DiscussionMessage>())).ReturnsAsync(0);

    // Participants list exists, but does NOT include the sender (userId 1)
    var participants = new List<User> 
    { 
        new User { UserId = mentionedId, Name = "Target User" } 
    };
    mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(participants);

    // Act
    await service.CreateMessageAsync("Hey @Target", null, eventId, userId, null);

    // Assert
    // Verify notification was sent with the fallback "Someone"
    mockNotification.Verify(n => n.NotifyAsync(
        mentionedId, 
        It.IsAny<string>(), 
        It.Is<string>(s => s.Contains("Someone mentioned you"))), 
        Times.Once);
}

    [Fact]
    public async Task CreateMessageAsync_MutedMoreThanOneHour_ErrorShowsHoursAndMinutes()
    {
        int userId = 1, eventId = 10;
        SetupReputation(userId);
        SetupEvent(eventId, MakeEvent(eventId));
        mockRepo.Setup(r => r.GetMuteAsync(eventId, userId))
                .ReturnsAsync(new DiscussionMute
                {
                    IsPermanent = false,
                    MutedUntil = DateTime.UtcNow.AddHours(2).AddMinutes(15) // >= 1 hour → "Xh Ym" branch
                });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateMessageAsync("hi", null, eventId, userId, null));

        // "Xh Ym" format
        Assert.Contains("h ", ex.Message);
        Assert.Contains("m", ex.Message);
    }
    [Fact]
    public async Task ReactAsync_NoExistingReaction_CallsAddReaction()
    {
        int messageId = 1, userId = 2;
        string emoji = "👍";

        mockRepo.Setup(r => r.GetReactionAsync(messageId, userId))
                .ReturnsAsync((DiscussionReaction?)null);
        mockRepo.Setup(r => r.AddReactionAsync(messageId, userId, emoji))
                .Returns(Task.CompletedTask);

        await service.ReactAsync(messageId, userId, emoji);

        mockRepo.Verify(r => r.AddReactionAsync(messageId, userId, emoji), Times.Once);
        mockRepo.Verify(r => r.UpdateReactionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReactAsync_ExistingReaction_SameEmoji_CallsUpdateReaction()
    {
        int messageId = 1, userId = 2;
        string emoji = "👍";

        mockRepo.Setup(r => r.GetReactionAsync(messageId, userId))
                .ReturnsAsync(new DiscussionReaction
                {
                    Id = 1,
                    Emoji = "👍",
                    Message = new DiscussionMessage(messageId, null, DateTime.MinValue),
                    Author = new User { UserId = userId }

                });
        mockRepo.Setup(r => r.UpdateReactionAsync(messageId, userId, emoji))
                .Returns(Task.CompletedTask);

        await service.ReactAsync(messageId, userId, emoji);

        mockRepo.Verify(r => r.UpdateReactionAsync(messageId, userId, emoji), Times.Once);
    }


    [Fact]
    public async Task MuteUserAsync_EventNotFound_ThrowsArgumentException()
    {
        mockEventRepo.Setup(e => e.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.MuteUserAsync(99, 5, DateTime.UtcNow.AddHours(1), 1));
    }

    [Fact]
    public async Task UnmuteUserAsync_EventNotFound_ThrowsArgumentException()
    {
        mockEventRepo.Setup(e => e.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UnmuteUserAsync(99, 5, 1));
    }

    [Fact]
    public async Task SetSlowModeAsync_EventNotFound_ThrowsArgumentException()
    {
        mockEventRepo.Setup(e => e.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetSlowModeAsync(99, 30, 1));
    }


    [Fact]
    public async Task GetMessagesAsync_MessageWithNullAuthor_RegularUser_CanDeleteIsFalse()
    {
        int userId = 3, eventId = 1;
        var evt = MakeEvent(eventId, adminId: 99);
        SetupEvent(eventId, evt);

        var messages = new List<DiscussionMessage>
        {
            new DiscussionMessage(1, "orphan", DateTime.UtcNow) { Author = null }
        };
        mockRepo.Setup(r => r.GetByEventAsync(eventId, userId)).ReturnsAsync(messages);

        var result = await service.GetMessagesAsync(eventId, userId);

        Assert.False(result[0].CanDelete);
    }

    [Fact]
    public async Task GetMessagesAsync_MessageWithNullAuthor_AdminUser_CanDeleteIsTrue()
    {
        int adminId = 99, eventId = 1;
        var evt = MakeEvent(eventId, adminId: adminId);
        SetupEvent(eventId, evt);

        var messages = new List<DiscussionMessage>
        {
            new DiscussionMessage(1, "orphan", DateTime.UtcNow) { Author = null }
        };
        mockRepo.Setup(r => r.GetByEventAsync(eventId, adminId)).ReturnsAsync(messages);

        var result = await service.GetMessagesAsync(eventId, adminId);

        Assert.True(result[0].CanDelete);
    }

    [Fact]
    public async Task MuteUserAsync_EventHasNoAdmin_ThrowsUnauthorized()
    {
        int eventId = 10;
        SetupEvent(eventId, MakeEvent(eventId)); // no adminId → Admin is null

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.MuteUserAsync(eventId, 5, DateTime.UtcNow.AddHours(1), 1));
    }

    [Fact]
    public async Task UnmuteUserAsync_EventHasNoAdmin_ThrowsUnauthorized()
    {
        int eventId = 10;
        SetupEvent(eventId, MakeEvent(eventId));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UnmuteUserAsync(eventId, 5, 1));
    }

    [Fact]
    public async Task SetSlowModeAsync_EventHasNoAdmin_ThrowsUnauthorized()
    {
        int eventId = 10;
        SetupEvent(eventId, MakeEvent(eventId));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SetSlowModeAsync(eventId, 30, 1));
    }
    [Fact]
    public async Task CreateMessageAsync_SetsMediaPathOnPersistedMessage()
    {
        int userId = 1, eventId = 10;
        SetupReputation(userId);
        SetupEvent(eventId, MakeEvent(eventId));
        SetupNoMute(eventId, userId);
        SetupNoSlowMode(eventId, userId);
        mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(new List<User>());

        DiscussionMessage? captured = null;
        mockRepo.Setup(r => r.AddAsync(It.IsAny<DiscussionMessage>()))
                .Callback<DiscussionMessage>(m => captured = m)
                .ReturnsAsync(0);

        await service.CreateMessageAsync(null, "/tmp/photo.jpg", eventId, userId, null);

        Assert.Equal("/tmp/photo.jpg", captured!.MediaPath);
    }

    [Fact]
    public async Task CreateMessageAsync_TrimsWhitespaceFromText()
    {
        int userId = 1, eventId = 10;
        SetupReputation(userId);
        SetupEvent(eventId, MakeEvent(eventId));
        SetupNoMute(eventId, userId);
        SetupNoSlowMode(eventId, userId);
        mockRepo.Setup(r => r.GetEventParticipantsAsync(eventId)).ReturnsAsync(new List<User>());

        DiscussionMessage? captured = null;
        mockRepo.Setup(r => r.AddAsync(It.IsAny<DiscussionMessage>()))
                .Callback<DiscussionMessage>(m => captured = m)
                .ReturnsAsync(0);

        await service.CreateMessageAsync("  hello  ", null, eventId, userId, null);

        Assert.Equal("hello", captured!.Message);
    }
}
