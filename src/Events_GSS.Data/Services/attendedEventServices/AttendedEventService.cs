using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.Services
{
    public class AttendedEventService : IAttendedEventService
    {
        private readonly IAttendedEventRepository _repo;

        public AttendedEventService(IAttendedEventRepository repo)
        {
            _repo = repo;
        }

        // Return all attended events.
        public async Task<List<AttendedEvent>> GetAttendedEventsAsync(int userId)
        {
            return await _repo.GetByUserIdAsync(userId);
        }

        // Returns either all archived, or all unarchived events of a given user
        public async Task<List<AttendedEvent>> GetEventsByArchiveStatusAsync(int userId, bool isArchived)
        {
            var all = await _repo.GetByUserIdAsync(userId);
            if(isArchived)
                return all.Where(ae => ae.IsArchived).ToList();
            return all.Where(ae =>  !ae.IsArchived).ToList();
        }

        // Enrolls a user in an event with the current UTC time as the enrollment date.
        public async Task AttendEventAsync(int eventId, int userId)
        {
            // Check if already enrolled to avoid duplicate entries.
            var existing = await _repo.GetAsync(eventId, userId);
            if (existing != null)
                return;

            // The Event and User objects here are lightweight stubs —
            // only their IDs matter for the INSERT query.
            var attendedEvent = new AttendedEvent
            {
                Event = new Event { EventId = eventId },
                User = new User { UserId = userId },
                EnrollmentDate = DateTime.UtcNow,
                IsArchived = false,
                IsFavourite = false
            };

            await _repo.AddAsync(attendedEvent);
        }

        public async Task LeaveEventAsync(int eventId, int userId)
        {
            await _repo.DeleteAsync(eventId, userId);
        }

        public async Task SetArchivedAsync(int eventId, int userId, bool isArchived)
        {
            await _repo.UpdateIsArchivedAsync(eventId, userId, isArchived);
        }

        public async Task SetFavouriteAsync(int eventId, int userId, bool isFavourite)
        {
            await _repo.UpdateIsFavouriteAsync(eventId, userId, isFavourite);
        }

        // Returns all events both the user and a specified friend are enrolled in.
        public async Task<List<AttendedEvent>> GetCommonEventsAsync(int userId, int friendId)
        {
            return await _repo.GetCommonEventsAsync(userId, friendId);
        }
    }
}