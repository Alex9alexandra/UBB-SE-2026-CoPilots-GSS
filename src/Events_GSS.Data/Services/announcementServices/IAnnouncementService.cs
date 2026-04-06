using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.announcementServices;

public interface IAnnouncementService
{
    Task<List<Announcement>> GetAnnouncementsAsync(int eventId, int userId);
    Task CreateAnnouncementAsync(string message, int eventId, int userId);
    Task UpdateAnnouncementAsync(int announcementId, string newMessage, int userId, int eventId);
    Task DeleteAnnouncementAsync(int annId, int userId, int eventId);
    Task PinAnnouncementAsync(int annId, int eventId, int userId);
    Task MarkAsReadAsync(int annId, int userId);
    Task<(List<AnnouncementReadReceipt>Readers, int TotalParticipants)> GetReadReceiptsAsync(int annId, int eventId, int userId);
    Task ReactAsync(int annId, int userId, string emoji);
    Task RemoveReactionAsync(int annId, int userId);
    Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId);
    Task<List<User>> GetAllParticipantsAsync(int eventId);

}
