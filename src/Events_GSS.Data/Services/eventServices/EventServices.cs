using System;
using CommunityToolkit.Mvvm.Messaging;
using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services.reputationService;

namespace Events_GSS.Data.Services.eventServices;

/// <summary>
/// Provides event management services including CRUD operations and filtering.
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository eventRepository;

    private readonly IReputationService reputationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventService"/> class.
    /// </summary>
    /// <param name="eventRepository">The event repository.</param>
    /// <param name="reputationService">The reputation service.</param>
    public EventService(IEventRepository eventRepository, IReputationService reputationService)
    {
        this.eventRepository = eventRepository;
        this.reputationService = reputationService;
    }

    /// <summary>
    /// Gets all public active events.
    /// </summary>
    /// <returns>A list of public active events.</returns>
    public async Task<List<Event>> GetAllPublicActiveEventsAsync()
        => await this.eventRepository.GetAllPublicActiveAsync();

    /// <summary>
    /// Gets an event by its identifier.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>The event if found; otherwise, null.</returns>
    public async Task<Event?> GetEventByIdAsync(int eventId)
        => await this.eventRepository.GetByIdAsync(eventId);

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="eventEntity">The event to create.</param>
    /// <returns>The created event identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user's reputation is too low to create events.</exception>
    public async Task<int> CreateEventAsync(Event eventEntity)
    {
        if (!await this.reputationService.CanCreateEventsAsync(eventEntity.Admin.UserId))
        {
            throw new InvalidOperationException("Your reputation is too low to create events (below -700 RP).");
        }

        int eventId = await this.eventRepository.AddAsync(eventEntity);
        WeakReferenceMessenger.Default.Send(
            new ReputationMessage(eventEntity.Admin.UserId, ReputationAction.EventCreated));
        return eventId;
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventEntity">The event to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateEventAsync(Event eventEntity)
        => await this.eventRepository.UpdateAsync(eventEntity);

    /// <summary>
    /// Deletes an event by its identifier.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteEventAsync(int eventId)
    {
        var @event = await this.eventRepository.GetByIdAsync(eventId);
        await this.eventRepository.DeleteAsync(eventId);
        if (@event?.Admin != null)
        {
            WeakReferenceMessenger.Default.Send(
                new ReputationMessage(@event.Admin.UserId, ReputationAction.EventCancelled));
        }
    }

    /// <summary>
    /// Filters events by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>A list of events matching the category.</returns>
    public async Task<List<Event>> FilterByCategoryAsync(string category)
    {
        var events = await this.eventRepository.GetAllPublicActiveAsync();
        return events.Where(@event => @event.Category != null &&
            @event.Category.Title.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Filters events by location.
    /// </summary>
    /// <param name="location">The location to filter by.</param>
    /// <returns>A list of events matching the location.</returns>
    public async Task<List<Event>> FilterByLocationAsync(string location)
    {
        var events = await this.eventRepository.GetAllPublicActiveAsync();
        return events.Where(@event => @event.Name.Contains(location, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Filters events by a specific date.
    /// </summary>
    /// <param name="date">The date to filter by.</param>
    /// <returns>A list of events on the specified date.</returns>
    public async Task<List<Event>> FilterByDateAsync(DateTime date)
    {
        var events = await this.eventRepository.GetAllPublicActiveAsync();
        return events.Where(@event => @event.StartDateTime.Date == date.Date).ToList();
    }

    /// <summary>
    /// Filters events by a date range.
    /// </summary>
    /// <param name="from">The start date of the range.</param>
    /// <param name="to">The end date of the range.</param>
    /// <returns>A list of events within the specified date range.</returns>
    public async Task<List<Event>> FilterByDateRangeAsync(DateTime from, DateTime to)
    {
        var events = await this.eventRepository.GetAllPublicActiveAsync();
        return events.Where(@event => @event.StartDateTime.Date >= from.Date &&
            @event.StartDateTime.Date <= to.Date).ToList();
    }

    /// <summary>
    /// Searches events by title.
    /// </summary>
    /// <param name="title">The title to search for.</param>
    /// <returns>A list of events matching the title.</returns>
    public async Task<List<Event>> SearchByTitleAsync(string title)
    {
        var events = await this.eventRepository.GetAllPublicActiveAsync();
        return events.Where(@event => @event.Name.Contains(title, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
