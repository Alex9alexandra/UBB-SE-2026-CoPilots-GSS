using System;
using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.eventServices;

/// <summary>
/// Defines the contract for event-related operations.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Gets all public and active events.
    /// </summary>
    /// <returns>A list of public active events.</returns>
    Task<List<Event>> GetAllPublicActiveEventsAsync();

    /// <summary>
    /// Gets an event by its identifier.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>The event if found; otherwise, null.</returns>
    Task<Event?> GetEventByIdAsync(int eventId);

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="eventEntity">The event to create.</param>
    /// <returns>The identifier of the created event.</returns>
    Task<int> CreateEventAsync(Event eventEntity);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventEntity">The event to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateEventAsync(Event eventEntity);

    /// <summary>
    /// Deletes an event by its identifier.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteEventAsync(int eventId);

    /// <summary>
    /// Searches for events by title.
    /// </summary>
    /// <param name="title">The title to search for.</param>
    /// <returns>A list of events matching the title.</returns>
    Task<List<Event>> SearchByTitleAsync(string title);

    /// <summary>
    /// Filters events by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>A list of events in the specified category.</returns>
    Task<List<Event>> FilterByCategoryAsync(string category);

    /// <summary>
    /// Filters events by location.
    /// </summary>
    /// <param name="location">The location to filter by.</param>
    /// <returns>A list of events at the specified location.</returns>
    Task<List<Event>> FilterByLocationAsync(string location);

    /// <summary>
    /// Filters events by date.
    /// </summary>
    /// <param name="date">The date to filter by.</param>
    /// <returns>A list of events on the specified date.</returns>
    Task<List<Event>> FilterByDateAsync(DateTime date);

    /// <summary>
    /// Filters events by date range.
    /// </summary>
    /// <param name="from">The start date of the range.</param>
    /// <param name="to">The end date of the range.</param>
    /// <returns>A list of events within the specified date range.</returns>
    Task<List<Event>> FilterByDateRangeAsync(DateTime from, DateTime to);
}
