using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories;

public interface IDiscussionRepository
{
    // ── Messages ──────────────────────────────────────────────
    Task<List<DiscussionMessage>> GetByEventAsync(int eventId, int currentUserId);
    Task<DiscussionMessage?> GetByIdAsync(int messageId);
    Task<int> AddAsync(DiscussionMessage message);
    Task DeleteAsync(int messageId);
    Task<DateTime?> GetLastUserMessageDateAsync(int eventId, int userId);

    // ── Reactions ─────────────────────────────────────────────
    Task AddReactionAsync(int messageId, int userId, string emoji);
    Task RemoveReactionAsync(int messageId, int userId);
    Task<List<DiscussionReaction>> GetReactionsAsync(int messageId);

    // ── Mutes ─────────────────────────────────────────────────
    Task<DiscussionMute?> GetMuteAsync(int eventId, int userId);
    Task MuteAsync(DiscussionMute mute);
    Task UnmuteAsync(int eventId, int userId);

    // -─ Slow Mode ───────────────────────────────────────────────-
    Task SetSlowModeAsync(int eventId, int? seconds);

    // ── Participants ─────────────────────────────────────────────────────
    //<summary>
    //Used for the @mention lookup when posting messages. Returns all users who have participated in the discussion (posted a message or reaction).
    //</summary>
    Task<List<User>> GetEventParticipantsAsync(int eventId);
}

