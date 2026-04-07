using CommunityToolkit.Mvvm.Messaging;
using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.reputationService;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.Services
{
    /// <summary>
    /// Service for managing user attended events, including enrollment, archiving, and marking favorites.
    /// </summary>
    public class AttendedEventService : IAttendedEventService
    {
        private readonly IAttendedEventRepository attendedEventRepository;
        private readonly IReputationService reputationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendedEventService"/> class.
        /// </summary>
        /// <param name="attendedEventRepository">The attended event repository.</param>
        /// <param name="reputationService">The reputation service.</param>
        public AttendedEventService(IAttendedEventRepository attendedEventRepository, IReputationService reputationService)
        {
            this.attendedEventRepository = attendedEventRepository;
            this.reputationService = reputationService;
        }

        /// <summary>
        /// Gets all attended events for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A list of attended events.</returns>
        public async Task<List<AttendedEvent>> GetAttendedEventsAsync(int userId)
        {
            return await this.attendedEventRepository.GetByUserIdAsync(userId);
        }

        /// <summary>
        /// Gets events by archive status for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="isArchived">True to get archived events, false to get unarchived events.</param>
        /// <returns>A list of attended events matching the archive status.</returns>
        public async Task<List<AttendedEvent>> GetEventsByArchiveStatusAsync(int userId, bool isArchived)
        {
            var attendedEvents = await this.attendedEventRepository.GetByUserIdAsync(userId);
            return isArchived ? attendedEvents.Where(ae => ae.IsArchived).ToList() : attendedEvents.Where(ae => !ae.IsArchived).ToList();
        }

        /// <summary>
        /// Enrolls a user in an event with the current UTC time as the enrollment date.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user's reputation is too low to attend events.</exception>
        public async Task AttendEventAsync(int eventId, int userId)
        {
            if (!await this.reputationService.CanAttendEventsAsync(userId))
            {
                throw new InvalidOperationException("Your reputation is too low to attend events (at -1000 RP).");
            }

            // Check if already enrolled to avoid duplicate entries.
            var existingAttendedEvent = await this.attendedEventRepository.GetAsync(eventId, userId);
            if (existingAttendedEvent != null)
            {
                return;
            }

            // The Event and User objects here are lightweight stubs —
            // only their IDs matter for the INSERT query.
            var attendedEvent = new AttendedEvent
            {
                Event = new Event { EventId = eventId },
                User = new User { UserId = userId },
                EnrollmentDate = DateTime.UtcNow,
                IsArchived = false,
                IsFavourite = false,
            };

            await this.attendedEventRepository.AddAsync(attendedEvent);

            WeakReferenceMessenger.Default.Send(
                new ReputationMessage(userId, ReputationAction.EventAttended, eventId));
        }

        /// <summary>
        /// Gets a specific attended event for a user.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The attended event, or null if not found.</returns>
        public async Task<AttendedEvent?> GetAsync(int eventId, int userId)
        {
            return await this.attendedEventRepository.GetAsync(eventId, userId);
        }

        /// <summary>
        /// Removes a user's enrollment from an event.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LeaveEventAsync(int eventId, int userId)
        {
            await this.attendedEventRepository.DeleteAsync(eventId, userId);
        }

        /// <summary>
        /// Sets the archive status for a user's attended event.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="isArchived">True to archive the event, false to unarchive it.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SetArchivedAsync(int eventId, int userId, bool isArchived)
        {
            await this.attendedEventRepository.UpdateIsArchivedAsync(eventId, userId, isArchived);
        }

        /// <summary>
        /// Sets the favourite status for a user's attended event.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="isFavourite">True to mark as favourite, false to unmark it.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SetFavouriteAsync(int eventId, int userId, bool isFavourite)
        {
            await this.attendedEventRepository.UpdateIsFavouriteAsync(eventId, userId, isFavourite);
        }

        /// <summary>
        /// Gets all events that both a user and their friend are enrolled in.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="friendId">The friend's user ID.</param>
        /// <returns>A list of attended events common to both users.</returns>
        public async Task<List<AttendedEvent>> GetCommonEventsAsync(int userId, int friendId)
        {
            return await this.attendedEventRepository.GetCommonEventsAsync(userId, friendId);
        }
    }
}