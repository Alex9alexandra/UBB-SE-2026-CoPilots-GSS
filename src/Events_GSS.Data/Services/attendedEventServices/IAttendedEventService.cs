using Events_GSS.Data.Models;

namespace Events_GSS.Services.Interfaces
{
    public interface IAttendedEventService
    {
        Task<List<AttendedEvent>> GetAttendedEventsAsync(int userId);

        Task<List<AttendedEvent>> GetEventsByArchiveStatusAsync(int userId, bool isArchived);

        Task AttendEventAsync(int eventId, int userId);

        Task LeaveEventAsync(int eventId, int userId);

        Task SetArchivedAsync(int eventId, int userId, bool isArchived);

        Task SetFavouriteAsync(int eventId, int userId, bool isFavourite);

        // Returns events both the user and a friend are enrolled in.
        Task<List<AttendedEvent>> GetCommonEventsAsync(int userId, int friendId);
    }
}