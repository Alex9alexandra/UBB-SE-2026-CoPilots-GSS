using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.discussionService;

public interface IDiscussionService
{
    // ── Messages ──────────────────────────────────────────────
    Task<List<DiscussionMessage>> GetMessagesAsync(int eventId, int userId);
    Task CreateMessageAsync(string? text, string? mediaPath, int eventId, int userId, int? replyToId);
    Task DeleteMessageAsync(int messageId, int userId, int eventId);

    // ── Reactions ─────────────────────────────────────────────
    Task ReactAsync(int messageId, int userId, string emoji);
    Task RemoveReactionAsync(int messageId, int userId);

    // ── Mutes ─────────────────────────────────────────────────
    Task MuteUserAsync(int eventId, int targetUserId, DateTime? muteUntil, int adminUserId);
    Task UnmuteUserAsync(int eventId, int targetUserId, int adminUserId);

    // ── Slow Mode ───────────────────────────────────────────────
    Task SetSlowModeAsync(int eventId, int? seconds, int adminUserId);
    Task<int?> GetSlowModeSecondsAsync(int eventId);

    // ── Participants ─────────────────────────────────────────────────────
    Task<List<User>> GetEventParticipantsAsync(int eventId);
}
