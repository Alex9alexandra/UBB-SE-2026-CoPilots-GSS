using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories;

public interface IAnnouncementRepository
{
    // ── Announcements ─────────────────────────────────────────
    Task<List<Announcement>> GetAnnouncementsByEventAsync(int eventId, int userId);
    Task<int> AddAnnouncementAsync(Announcement announcement, int eventId, int userId);
    Task UpdateAnnouncementAsync(int announcementId, string newMessage);
    Task DeleteAnnouncementAsync(int selectedEvent);
    Task<Announcement?> GetAnnouncementByIdAsync(int announcementId);

    // ── Pinning ─────────────────────────────────────────
    Task PinAsync(int announcementId, int eventId);
    Task UnpinAnnouncementAsync(int eventId);
    
    // ── Read Receipts ─────────────────────────────────────────

    Task MarkAsReadAsync(int announcementId, int userId);
    Task<List<AnnouncementReadReceipt>> GetReadReceiptsAsync(int announcementId);
    Task<int> GetTotalParticipantsAsync(int eventId);
    Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId);
    Task<List<User>> GetAllParticipantsAsync(int eventId);

    // ── Reactions ─────────────────────────────────────────

    Task AddOrUpdateReactionAsync(int announcementId, int userId, string emoji);
    Task RemoveReactionAsync(int announcementId, int userId);


}
