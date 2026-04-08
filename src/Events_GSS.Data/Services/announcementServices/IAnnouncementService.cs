namespace Events_GSS.Data.Services.announcementServices;

using Events_GSS.Data.Models;

/// <summary>
/// Provides business logic operations for managing announcements,
/// including creation, updates, reactions, read tracking, and participant queries.
/// </summary>
public interface IAnnouncementService
{
    /// <summary>
    /// Retrieves all announcements for a given event and user context.
    /// </summary>
    Task<List<Announcement>> GetAnnouncementsAsync(int eventId, int userId);

    /// <summary>
    /// Creates a new announcement in the specified event.
    /// </summary>
    Task CreateAnnouncementAsync(string announcementMessage, int eventId, int userId);

    /// <summary>
    /// Updates an existing announcement's content.
    /// </summary>
    Task UpdateAnnouncementAsync(int announcementId, string newAnnouncementMessage, int userId, int eventId);

    /// <summary>
    /// Deletes an announcement from an event.
    /// </summary>
    Task DeleteAnnouncementAsync(int announcementId, int userId, int eventId);

    /// <summary>
    /// Pins an announcement within an event.
    /// </summary>
    Task PinAnnouncementAsync(int announcementId, int eventId, int userId);

    /// <summary>
    /// Marks an announcement as read for a user.
    /// </summary>
    Task MarkAsReadAsync(int announcementId, int userId);

    /// <summary>
    /// Retrieves read receipts and total participant count for an announcement.
    /// </summary>
    Task<(List<AnnouncementReadReceipt> Readers, int TotalParticipants)> GetReadReceiptsAsync(
        int announcementId,
        int eventId,
        int userId);

    /// <summary>
    /// Adds or updates a reaction for an announcement.
    /// </summary>
    Task AddOrUpdateReactAsync(int announcementId, int userId, string emoji);

    /// <summary>
    /// Removes a user's reaction from an announcement.
    /// </summary>
    Task RemoveReactionAsync(int announcementId, int userId);

    /// <summary>
    /// Gets unread announcement counts grouped by event for a user.
    /// </summary>
    Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId);

    /// <summary>
    /// Retrieves all users participating in an event.
    /// </summary>
    Task<List<User>> GetAllParticipantsAsync(int eventId);

    /// <summary>
    /// Toggles a reaction for an announcement (adds or removes based on current state).
    /// </summary>
    Task ToggleReactionAsync(int announcementId, int userId, string emoji);

    /// <summary>
    /// Marks an announcement as read only if it has not already been marked.
    /// </summary>
    Task<bool> MarkAsReadIfNeededAsync(int announcementId, int userId, bool isAlreadyRead);

    /// <summary>
    /// Retrieves all users who have not read a specific announcement.
    /// </summary>
    Task<List<User>> GetNonReadersAsync(int announcementId, int eventId);
}