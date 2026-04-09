using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories
{
    /// <summary>
    /// Defines repository operations for attended events.
    /// </summary>
    public interface IAttendedEventRepository
    {
        /// <summary>
        /// Adds a new attended event.
        /// </summary>
        /// <param name="attendedEvent">The attended event to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(AttendedEvent attendedEvent);

        /// <summary>
        /// Deletes an attended event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(int eventId, int userId);

        /// <summary>
        /// Updates the archived status of an attended event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="isArchived">The archived status to set.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateIsArchivedAsync(int eventId, int userId, bool isArchived);

        /// <summary>
        /// Updates the favourite status of an attended event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="isFavourite">The favourite status to set.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateIsFavouriteAsync(int eventId, int userId, bool isFavourite);

        /// <summary>
        /// Gets an attended event by event and user identifiers.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The attended event if found; otherwise, null.</returns>
        Task<AttendedEvent?> GetAsync(int eventId, int userId);

        /// <summary>
        /// Gets all attended events for a specific user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>A list of attended events for the user.</returns>
        Task<List<AttendedEvent>> GetByUserIdAsync(int userId);

        /// <summary>
        /// Gets common events attended by two users.
        /// </summary>
        /// <param name="userId">The first user identifier.</param>
        /// <param name="friendId">The second user identifier.</param>
        /// <returns>A list of common attended events.</returns>
        Task<List<AttendedEvent>> GetCommonEventsAsync(int userId, int friendId);

        /// <summary>
        /// Gets the count of attendees for a specific event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <returns>The number of attendees.</returns>
        Task<int> GetAttendeeCountAsync(int eventId);
    }
}
