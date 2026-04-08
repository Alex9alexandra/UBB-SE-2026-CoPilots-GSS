namespace Events_GSS.Data.Repositories;

using Events_GSS.Data.Models;

/// <summary>
/// Provides data access operations for announcements, including creation, updates,
/// deletion, reactions, pinning, and read receipt tracking.
/// </summary>
public interface IAnnouncementRepository
{
    // ── Announcements ─────────────────────────────────────────

    /// <summary>
    /// Retrieves all announcements for a specific event, including user read state.
    /// </summary>
    Task<List<Announcement>> GetAnnouncementsByEventAsync(int eventId, int userId);

    /// <summary>
    /// Adds a new announcement to an event and returns its generated identifier.
    /// </summary>
    Task<int> AddAnnouncementAsync(Announcement announcement, int eventId, int userId);

    /// <summary>
    /// Updates the message content of an existing announcement.
    /// </summary>
    Task UpdateAnnouncementAsync(int announcementId, string newMessage);

    /// <summary>
    /// Deletes an announcement from the system.
    /// </summary>
    Task DeleteAnnouncementAsync(int selectedEvent);

    /// <summary>
    /// Retrieves a single announcement by its identifier.
    /// </summary>
    Task<Announcement?> GetAnnouncementByIdAsync(int announcementId);

    // ── Pinning ─────────────────────────────────────────

    /// <summary>
    /// Pins a specific announcement within an event.
    /// </summary>
    Task PinAsync(int announcementId, int eventId);

    /// <summary>
    /// Removes the pinned status from all announcements in an event.
    /// </summary>
    Task UnpinAnnouncementAsync(int eventId);

    // ── Read Receipts ─────────────────────────────────────────

    /// <summary>
    /// Marks an announcement as read by a specific user.
    /// </summary>
    Task MarkAsReadAsync(int announcementId, int userId);

    /// <summary>
    /// Retrieves all read receipts for a given announcement.
    /// </summary>
    Task<List<AnnouncementReadReceipt>> GetReadReceiptsAsync(int announcementId);

    /// <summary>
    /// Gets the total number of participants for an event.
    /// </summary>
    Task<int> GetTotalParticipantsAsync(int eventId);

    /// <summary>
    /// Retrieves unread announcement counts grouped by event for a user.
    /// </summary>
    Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId);

    /// <summary>
    /// Retrieves all users participating in an event.
    /// </summary>
    Task<List<User>> GetAllParticipantsAsync(int eventId);

    // ── Reactions ─────────────────────────────────────────

    /// <summary>
    /// Adds a new reaction or updates an existing reaction for an announcement.
    /// </summary>
    Task AddOrUpdateReactionAsync(int announcementId, int userId, string emoji);

    /// <summary>
    /// Removes a user's reaction from an announcement.
    /// </summary>
    Task RemoveReactionAsync(int announcementId, int userId);

    /// <summary>
    /// Gets the reaction emoji a user has given to an announcement, if any.
    /// </summary>
    Task<string?> GetUserReactionAsync(int announcementId, int userId);
}