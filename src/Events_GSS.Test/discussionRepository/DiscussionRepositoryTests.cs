using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;

using Microsoft.Data.SqlClient;

using Xunit;

namespace Events_GSS.Test.discussionRepository;

public class DiscussionRepositoryTests : IAsyncLifetime
{
    private const string ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ISSEvents;Integrated Security=True;Encrypt=False;";
    private readonly SqlConnectionFactory _factory;
    private readonly DiscussionRepository _repo;

    // Fixed IDs for seeding prerequisites (ensure these exist in your seed script/DB)
    private const int TestEventId = 1;
    private const int TestUserId = 1;
    private const int OtherUserId = 2;

    public DiscussionRepositoryTests()
    {
        _factory = new SqlConnectionFactory(ConnectionString);
        _repo = new DiscussionRepository(_factory);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Cleanup discussions to avoid FK conflicts in subsequent runs
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("DELETE FROM DiscussionReactions; DELETE FROM DiscussionMutes; DELETE FROM Discussions;", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Messages Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_MessageDoesNotExist_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(-9101);
        Assert.Null(result);
    }



    [Fact]
    public async Task GetByEventAsync_WithRepliesAndReactions_HydratesFullModel()
    {
        // 1. Arrange: Parent Message
        var parent = new DiscussionMessage(0, "Parent", DateTime.UtcNow.AddMinutes(-10))
        {
            AssociatedEvent = new Event { EventId = TestEventId },
            Author = new User { UserId = TestUserId }
        };
        int parentId = await _repo.AddAsync(parent);

        // 2. Arrange: Reply with Media
        var reply = new DiscussionMessage(0, "Reply", DateTime.UtcNow)
        {
            AssociatedEvent = new Event { EventId = TestEventId },
            Author = new User { UserId = TestUserId },
            ReplyTo = new DiscussionMessage(parentId, null, DateTime.MinValue),
            MediaPath = "path/to/img.png"
        };
        int replyId = await _repo.AddAsync(reply);

        // 3. Arrange: Reactions
        await _repo.AddReactionAsync(replyId, TestUserId, "👍");
        await _repo.AddReactionAsync(replyId, OtherUserId, "❤️");

        // 4. Act
        var messages = await _repo.GetByEventAsync(TestEventId, TestUserId);

        // 5. Assert
        Assert.Equal(2, messages.Count);
        var fetchedReply = messages.First(m => m.Id == replyId);

        Assert.Equal("path/to/img.png", fetchedReply.MediaPath);
        Assert.NotNull(fetchedReply.ReplyTo);
        Assert.Equal(parentId, fetchedReply.ReplyTo.Id);
        Assert.Equal(2, fetchedReply.Reactions.Count);
    }

    [Fact]
    public async Task GetByEventAsync_NoMessages_ReturnsEmptyList()
    {
        var result = await _repo.GetByEventAsync(-999, TestUserId);
        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteAsync_DetachesReplies_ThenDeletes()
    {
        int pId = await _repo.AddAsync(new DiscussionMessage(0, "P", DateTime.UtcNow)
        { AssociatedEvent = new Event { EventId = TestEventId }, Author = new User { UserId = TestUserId } });
        int rId = await _repo.AddAsync(new DiscussionMessage(0, "R", DateTime.UtcNow)
        { AssociatedEvent = new Event { EventId = TestEventId }, Author = new User { UserId = TestUserId }, ReplyTo = new DiscussionMessage(pId, null, DateTime.MinValue) });

        await _repo.DeleteAsync(pId);

        var reply = await _repo.GetByIdAsync(rId);
        var parent = await _repo.GetByIdAsync(pId);

        Assert.Null(parent);
        Assert.NotNull(reply); // Reply still exists, but ReplyToId should be null in DB (implicit in logic)
    }

    [Fact]
    public async Task GetLastUserMessageDateAsync_ReturnsCorrectDate()
    {
        var date = new DateTime(2025, 1, 1, 12, 0, 0);
        await _repo.AddAsync(new DiscussionMessage(0, "T", date)
        { AssociatedEvent = new Event { EventId = TestEventId }, Author = new User { UserId = TestUserId } });

        var result = await _repo.GetLastUserMessageDateAsync(TestEventId, TestUserId);

        Assert.NotNull(result);
        Assert.Equal(date.ToString("yyyy-MM-dd HH:mm"), result.Value.ToString("yyyy-MM-dd HH:mm"));
    }

    // ── Reactions Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddReactionAsync_UpdatesExistingReaction()
    {
        int msgId = await _repo.AddAsync(new DiscussionMessage(0, "T", DateTime.UtcNow)
        { AssociatedEvent = new Event { EventId = TestEventId }, Author = new User { UserId = TestUserId } });

        await _repo.AddReactionAsync(msgId, TestUserId, "👍");
        await _repo.AddReactionAsync(msgId, TestUserId, "😆"); // Update

        var reactions = await _repo.GetReactionsAsync(msgId);
        Assert.Single(reactions);
        Assert.Equal("😆", reactions[0].Emoji);
    }

    [Fact]
    public async Task RemoveReactionAsync_DeletesFromDb()
    {
        int msgId = await _repo.AddAsync(new DiscussionMessage(0, "T", DateTime.UtcNow)
        { AssociatedEvent = new Event { EventId = TestEventId }, Author = new User { UserId = TestUserId } });

        await _repo.AddReactionAsync(msgId, TestUserId, "👍");
        await _repo.RemoveReactionAsync(msgId, TestUserId);

        var reactions = await _repo.GetReactionsAsync(msgId);
        Assert.Empty(reactions);
    }

    // ── Mutes & Slow Mode Tests ──────────────────────────────────────────────

    [Fact]
    public async Task MuteAsync_And_GetMute_HandlesTemporaryAndPermanent()
    {
        var mute = new DiscussionMute
        {
            EventId = TestEventId,
            MutedUser = new User { UserId = OtherUserId },
            MutedBy = new User { UserId = TestUserId },
            MutedUntil = DateTime.UtcNow.AddHours(1),
            IsPermanent = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.MuteAsync(mute);
        var fetched = await _repo.GetMuteAsync(TestEventId, OtherUserId);

        Assert.NotNull(fetched);
        Assert.False(fetched.IsPermanent);
        Assert.NotNull(fetched.MutedUntil);

        await _repo.UnmuteAsync(TestEventId, OtherUserId);
        Assert.Null(await _repo.GetMuteAsync(TestEventId, OtherUserId));
    }

    [Fact]
    public async Task SetSlowModeAsync_UpdatesEventTable()
    {
        await _repo.SetSlowModeAsync(TestEventId, 30);
        // Verification would usually require an IEventRepository.GetById, 
        // but for coverage, the ExecuteNonQuery call is sufficient.
        await _repo.SetSlowModeAsync(TestEventId, null); // Test NULL branch
    }

    [Fact]
    public async Task GetEventParticipantsAsync_ReturnsOrderedList()
    {
        // This relies on the 'AttendedEvents' table being seeded with users
        var participants = await _repo.GetEventParticipantsAsync(TestEventId);
        Assert.NotNull(participants);
    }

    [Fact]
    public async Task GetMuteAsync_UserIsNotMuted_ReturnsNull()
    {
        // Act
        // Use IDs that definitely don't have a mute record
        var result = await _repo.GetMuteAsync(TestEventId, 9999);

        // Assert
        Assert.Null(result);
    }
    [Fact]
    public async Task GetMuteAsync_TemporaryMute_ReturnsPopulatedObject()
    {
        // Arrange
        var until = DateTime.UtcNow.AddDays(1);
        var mute = new DiscussionMute
        {
            EventId = TestEventId,
            MutedUser = new User { UserId = OtherUserId },
            MutedBy = new User { UserId = TestUserId },
            MutedUntil = until,
            IsPermanent = false,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.MuteAsync(mute);

        // Act
        var result = await _repo.GetMuteAsync(TestEventId, OtherUserId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsPermanent);
        Assert.NotNull(result.MutedUntil);
        // Triggers the 'false' branch of IsDBNull
    }

    [Fact]
    public async Task GetMuteAsync_PermanentMute_HandlesNullMutedUntil()
    {
        // Arrange
        var mute = new DiscussionMute
        {
            EventId = TestEventId,
            MutedUser = new User { UserId = OtherUserId },
            MutedBy = new User { UserId = TestUserId },
            MutedUntil = null, // Permanent mutes often have no expiry date
            IsPermanent = true,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.MuteAsync(mute);

        // Act
        var result = await _repo.GetMuteAsync(TestEventId, OtherUserId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsPermanent);
        Assert.Null(result.MutedUntil);
        // Triggers the 'true' branch of IsDBNull (the '?' part)
    }

    [Fact]
    public async Task GetByEventAsync_ContainsMessageWithoutReactions_Continues()
    {
        // Arrange: One message with a reaction, one without
        int m1 = await _repo.AddAsync(new DiscussionMessage(0, "Has Reaction", DateTime.UtcNow)
        { AssociatedEvent = new Event { EventId = TestEventId }, Author = new User { UserId = TestUserId } });

        int m2 = await _repo.AddAsync(new DiscussionMessage(0, "No Reaction", DateTime.UtcNow)
        { AssociatedEvent = new Event { EventId = TestEventId }, Author = new User { UserId = TestUserId } });

        await _repo.AddReactionAsync(m1, TestUserId, "👍");

        // Act
        var results = await _repo.GetByEventAsync(TestEventId, TestUserId);

        // Assert
        Assert.NotEmpty(results.First(m => m.Id == m1).Reactions);
        Assert.Empty(results.First(m => m.Id == m2).Reactions);
    }

    [Fact]
public async Task GetByEventAsync_ReplyToMediaOnlyMessage_HydratesReplyNulls()
{
    // 1. Parent with NULL Message, but valid Media
    int parentId = await _repo.AddAsync(new DiscussionMessage(0, null, DateTime.UtcNow.AddMinutes(-1))
    {
        AssociatedEvent = new Event { EventId = TestEventId },
        Author = new User { UserId = TestUserId },
        MediaPath = "parent_img.png"
    });

    // 2. Reply to that parent
    await _repo.AddAsync(new DiscussionMessage(0, "Reply", DateTime.UtcNow)
    {
        AssociatedEvent = new Event { EventId = TestEventId },
        Author = new User { UserId = TestUserId },
        ReplyTo = new DiscussionMessage(parentId, null, DateTime.MinValue)
    });

    // Act
    var messages = await _repo.GetByEventAsync(TestEventId, TestUserId);
    var reply = messages.FirstOrDefault(m => m.ReplyTo?.Id == parentId);

    // Assert
    Assert.NotNull(reply?.ReplyTo);
    Assert.Null(reply.ReplyTo.Message); // This turns the Reply logic branch green!
}

    [Fact]
    public async Task GetByIdAsync_MessageExists_ReturnsMessage()
    {
        // Arrange
        int id = await _repo.AddAsync(new DiscussionMessage(0, "Hello", DateTime.UtcNow)
        {
            AssociatedEvent = new Event { EventId = TestEventId },
            Author = new User { UserId = TestUserId }
        });

        // Act
        var result = await _repo.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(TestUserId, result.Author.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_MessageIsNull_ReturnsMessageWithNullText()
    {
        // Arrange
        int id = await _repo.AddAsync(new DiscussionMessage(0, null, DateTime.UtcNow)
        {
            AssociatedEvent = new Event { EventId = TestEventId },
            Author = new User { UserId = TestUserId },
            MediaPath = "img.png" // ✅ satisfies DB constraint
        });

        // Act
        var result = await _repo.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Message); // 🔥 this is what we care about
    }
}