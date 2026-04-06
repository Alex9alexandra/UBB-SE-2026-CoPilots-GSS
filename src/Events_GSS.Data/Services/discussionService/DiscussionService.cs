using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;
using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services.discussionService;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.Services.notificationServices;
using Events_GSS.Data.Services.reputationService;

namespace Events_GSS.Data.Services;

public class DiscussionService : IDiscussionService
{
    private readonly IDiscussionRepository _repo;
    private readonly IEventRepository _eventRepo;
    private readonly IReputationService _reputationService;
    private readonly INotificationService _notificationService;

    public DiscussionService(
        IDiscussionRepository repo,
        IEventRepository eventRepo,
        IReputationService reputationService,
        INotificationService notificationService)
    {
        _repo = repo;
        _eventRepo = eventRepo;
        _reputationService = reputationService;
        _notificationService = notificationService;
    }

    // ── Messages ──────────────────────────────────────────────────────────────

    public async Task<List<DiscussionMessage>> GetMessagesAsync(int eventId, int userId)
    {
        var ev = await GetEventOrThrowAsync(eventId);

        var messages = await _repo.GetByEventAsync(eventId, userId);

        bool isAdmin = ev.Admin?.UserId == userId;
        foreach (var m in messages)
        {
            m.CanDelete = m.Author?.UserId == userId || isAdmin;
        }

        return messages;
    }

    public async Task CreateMessageAsync(
        string? text,
        string? mediaPath,
        int eventId,
        int userId,
        int? replyToId)
    {
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(mediaPath))
            throw new ArgumentException("A message must contain text, a media attachment, or both.");

        if (!await _reputationService.CanPostMessagesAsync(userId))
            throw new InvalidOperationException("Your reputation is too low to post messages (below -500 RP).");

        var ev = await GetEventOrThrowAsync(eventId);
        bool isAdmin = ev.Admin?.UserId == userId;

        // ── Mute check ───────────────────────────────────────
        if (!isAdmin)
        {
            var mute = await _repo.GetMuteAsync(eventId, userId);
            if (mute is not null)
            {
                if (mute.IsPermanent)
                    throw new InvalidOperationException("You are permanently muted in this event.");

                if (mute.MutedUntil.HasValue && mute.MutedUntil.Value > DateTime.UtcNow)
                {
                    var remaining = mute.MutedUntil.Value - DateTime.UtcNow;
                    throw new InvalidOperationException(
                        $"You are muted. Time remaining: {FormatDuration(remaining)}");
                }

                await _repo.UnmuteAsync(eventId, userId);
            }
        }

        // ── Slow mode check ──────────────────────────────────
        if (!isAdmin && ev.SlowModeSeconds.HasValue)
        {
            var lastDate = await _repo.GetLastUserMessageDateAsync(eventId, userId);
            if (lastDate.HasValue)
            {
                var elapsed = DateTime.UtcNow - lastDate.Value;
                var required = TimeSpan.FromSeconds(ev.SlowModeSeconds.Value);
                if (elapsed < required)
                {
                    var remaining = required - elapsed;
                    throw new InvalidOperationException(
                        $"Slow mode active. Wait {(int)remaining.TotalSeconds} seconds.");
                }
            }
        }

        // ── Persist ──────────────────────────────────────────
        var message = new DiscussionMessage(0, text?.Trim(), DateTime.UtcNow)
        {
            MediaPath = mediaPath,
            Event = ev,
            Author = new User { UserId = userId },
            ReplyTo = replyToId.HasValue
                ? new DiscussionMessage(replyToId.Value, null, DateTime.MinValue)
                : null
        };

        await _repo.AddAsync(message);

        WeakReferenceMessenger.Default.Send(
            new ReputationMessage(userId, ReputationAction.DiscussionMessagePosted));

        // ── Parse @mentions ──────────────────────────────────
        if (!string.IsNullOrWhiteSpace(text) && text.Contains('@'))
        {
            var participants = await _repo.GetEventParticipantsAsync(eventId);
            var mentionedUsers = FindMentionedUsers(text, participants)
                .Where(p => p.UserId != userId)
                .GroupBy(p => p.UserId)
                .Select(g => g.First())
                .ToList();

            if (mentionedUsers.Count > 0)
            {
                var mentioner = participants.FirstOrDefault(p => p.UserId == userId);
                string mentionerName = mentioner?.Name ?? "Someone";

                foreach (var u in mentionedUsers)
                {
                    await _notificationService.NotifyAsync(
                        u.UserId,
                        "You were mentioned!",
                        $"{mentionerName} mentioned you in the discussion.");
                }
            }
        }
    }

    public async Task DeleteMessageAsync(int messageId, int userId, int eventId)
    {
        var ev = await GetEventOrThrowAsync(eventId);
        bool isAdmin = ev.Admin?.UserId == userId;

        var message = await _repo.GetByIdAsync(messageId);
        if (message is null)
            throw new KeyNotFoundException($"Message with ID {messageId} does not exist.");

        if (message.Author?.UserId != userId && !isAdmin)
            throw new UnauthorizedAccessException("You can only delete your own messages.");

        bool isAdminDeletingOther = isAdmin && message.Author?.UserId != userId;
        await _repo.DeleteAsync(messageId);

        if (isAdminDeletingOther && message.Author != null)
        {
            WeakReferenceMessenger.Default.Send(
                new ReputationMessage(message.Author.UserId, ReputationAction.DiscussionMessageRemovedByAdmin));
        }
    }

    // ── Reactions ─────────────────────────────────────────────────────────────

    public async Task ReactAsync(int messageId, int userId, string emoji)
    {
        await _repo.AddReactionAsync(messageId, userId, emoji);
    }

    public async Task RemoveReactionAsync(int messageId, int userId)
    {
        await _repo.RemoveReactionAsync(messageId, userId);
    }

    // ── Mutes ─────────────────────────────────────────────────────────────────

    public async Task MuteUserAsync(int eventId, int targetUserId, DateTime? muteUntil, int adminUserId)
    {
        await EnsureAdminAsync(eventId, adminUserId);

        bool isPermanent = muteUntil is null;

        var mute = new DiscussionMute
        {
            EventId = eventId,
            MutedUser = new User { UserId = targetUserId },
            MutedBy = new User { UserId = adminUserId },
            MutedUntil = muteUntil,
            IsPermanent = isPermanent,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.MuteAsync(mute);
    }

    public async Task UnmuteUserAsync(int eventId, int targetUserId, int adminUserId)
    {
        await EnsureAdminAsync(eventId, adminUserId);
        await _repo.UnmuteAsync(eventId, targetUserId);
    }

    // ── Slow Mode ─────────────────────────────────────────────────────────────

    public async Task SetSlowModeAsync(int eventId, int? seconds, int adminUserId)
    {
        await EnsureAdminAsync(eventId, adminUserId);

        if (seconds.HasValue && seconds.Value <= 0)
            throw new ArgumentException("Slow mode interval must be a positive number of seconds.");

        await _repo.SetSlowModeAsync(eventId, seconds);
    }

    public async Task<int?> GetSlowModeSecondsAsync(int eventId)
    {
        var ev = await GetEventOrThrowAsync(eventId);
        return ev.SlowModeSeconds;
    }

    // ── Participants ──────────────────────────────────────────────────────────

    public async Task<List<User>> GetEventParticipantsAsync(int eventId)
    {
        return await _repo.GetEventParticipantsAsync(eventId);
    }

    public static List<User> FindMentionedUsers(string text, List<User> participants)
    {
        var mentioned = new List<User>();
        foreach (var p in participants)
        {
            // Check for @FullName (e.g. @Bob User)
            if (text.Contains($"@{p.Name}", StringComparison.OrdinalIgnoreCase))
            {
                mentioned.Add(p);
                continue;
            }

            // Check for @FirstName (e.g. @Bob)
            var firstName = p.Name.Split(' ')[0];
            if (text.Contains($"@{firstName}", StringComparison.OrdinalIgnoreCase))
            {
                mentioned.Add(p);
            }
        }

        return mentioned;
    }
    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Event> GetEventOrThrowAsync(int eventId)
    {
        var ev = await _eventRepo.GetByIdAsync(eventId);
        if (ev is null)
            throw new ArgumentException($"Event with ID {eventId} does not exist.");
        return ev;
    }

    private async Task EnsureAdminAsync(int eventId, int userId)
    {
        var ev = await GetEventOrThrowAsync(eventId);
        if (ev.Admin?.UserId != userId)
            throw new UnauthorizedAccessException("Only the EventAdmin can perform this action.");
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
    }
}
