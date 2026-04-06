using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories;

public interface IAnnouncementRepository
{
    // ── Announcements ─────────────────────────────────────────
    Task<List<Announcement>> GetByEventAsync(int eventId, int userId);
    Task<int> AddAsync(Announcement announcement, int eventId, int userId);
    Task UpdateAsync(int annId, string newMessage);
    Task DeleteAsync(int announcementId);
    Task<Announcement?> GetByIdAsync(int annId);

    // ── Pinning ─────────────────────────────────────────
    Task PinAsync(int announcementId, int eventId);
    Task UnpinAsync(int eventId);
    
    // ── Read Receipts ─────────────────────────────────────────

    Task MarkAsReadAsync(int announcementId, int userId);
    Task<List<AnnouncementReadReceipt>> GetReadReceiptsAsync(int announcementId);
    Task<int> GetTotalParticipantsAsync(int eventId);
    Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId);
    Task<List<User>> GetAllParticipantsAsync(int eventId);

    // ── Reactions ─────────────────────────────────────────

    Task AddReactionAsync(int announcementId, int userId, string emoji);
    Task RemoveReactionAsync(int announcementId, int userId);


}
