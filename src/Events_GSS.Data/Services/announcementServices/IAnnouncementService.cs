using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.announcementServices;

public interface IAnnouncementService
{
    Task<List<Announcement>> GetAnnouncementsAsync(int eventId, int userId);
    Task CreateAnnouncementAsync(string announcementMessage, int eventId, int userId);
    Task UpdateAnnouncementAsync(int announcementId, string newAnnouncementMessage, int userId, int eventId);
    Task DeleteAnnouncementAsync(int announcementId, int userId, int eventId);
    Task PinAnnouncementAsync(int announcementId, int eventId, int userId);
    Task MarkAsReadAsync(int announcementId, int userId);
    Task<(List<AnnouncementReadReceipt>Readers, int TotalParticipants)> GetReadReceiptsAsync(int announcementId, int eventId, int userId);
    Task AddOrUpdateReactAsync(int announcementId, int userId, string emoji);
    Task RemoveReactionAsync(int announcementId, int userId);
    Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId);
    Task<List<User>> GetAllParticipantsAsync(int eventId);
    Task ToggleReactionAsync(int announcementId, int userId, string emoji);
    Task<bool> MarkAsReadIfNeededAsync(int announcementId, int userId, bool isAlreadyRead);
    Task<List<User>> GetNonReadersAsync(int announcementId, int eventId);

}
