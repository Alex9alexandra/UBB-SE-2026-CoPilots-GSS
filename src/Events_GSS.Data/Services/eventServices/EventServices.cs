using System;
using CommunityToolkit.Mvvm.Messaging;
using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services.reputationService;

namespace Events_GSS.Data.Services.eventServices;

public class EventService: IEventService
{
	private readonly IEventRepository _eventRepository;
	private readonly IReputationService _reputationService;

	public EventService(IEventRepository eventRepository, IReputationService reputationService)
	{
		_eventRepository = eventRepository;
		_reputationService = reputationService;
	}

    public async Task<List<Event>> GetAllPublicActiveEventsAsync()
        => await _eventRepository.GetAllPublicActiveAsync();

    public async Task<Event?> GetEventByIdAsync(int eventId)
        => await _eventRepository.GetByIdAsync(eventId);

    public async Task CreateEventAsync(Event eventEntity)
    {
        if (!await _reputationService.CanCreateEventsAsync(eventEntity.Admin.UserId))
            throw new InvalidOperationException("Your reputation is too low to create events (below -700 RP).");

        await _eventRepository.AddAsync(eventEntity);
        WeakReferenceMessenger.Default.Send(
            new ReputationMessage(eventEntity.Admin.UserId, ReputationAction.EventCreated));
    }

    public async Task UpdateEventAsync(Event eventEntity)
        => await _eventRepository.UpdateAsync(eventEntity);

    public async Task DeleteEventAsync(int eventId)
    {
        var ev = await _eventRepository.GetByIdAsync(eventId);
        await _eventRepository.DeleteAsync(eventId);
        if (ev?.Admin != null)
        {
            WeakReferenceMessenger.Default.Send(
                new ReputationMessage(ev.Admin.UserId, ReputationAction.EventCancelled));
        }
    }


    public async Task<List<Event>> FilterByCategoryAsync(string category)
    {
        var all = await _eventRepository.GetAllPublicActiveAsync();
        return all.Where(e => e.Category != null &&
            e.Category.Title.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<Event>> FilterByLocationAsync(string location)
    {
        var all = await _eventRepository.GetAllPublicActiveAsync();
        return all.Where(e => e.Name.Contains(location, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<Event>> FilterByDateAsync(DateTime date)
    {
        var all = await _eventRepository.GetAllPublicActiveAsync();
        return all.Where(e => e.StartDateTime.Date == date.Date).ToList();
    }

    public async Task<List<Event>> FilterByDateRangeAsync(DateTime from, DateTime to)
    {
        var all = await _eventRepository.GetAllPublicActiveAsync();
        return all.Where(e => e.StartDateTime.Date >= from.Date &&
            e.StartDateTime.Date <= to.Date).ToList();
    }

    public async Task<List<Event>> SearchByTitleAsync(string title)
    {
        var all = await _eventRepository.GetAllPublicActiveAsync();
        return all.Where(e => e.Name.Contains(title, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
