using System;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.eventRepository;

namespace Events_GSS.Data.Services.eventServices;

public class EventService: IEventService
{
	private readonly IEventRepository _eventRepository;

    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<List<Event>> GetAllPublicActiveEventsAsync()
        => await _eventRepository.GetAllPublicActiveAsync();

    public async Task<Event?> GetEventByIdAsync(int eventId)
        => await _eventRepository.GetByIdAsync(eventId);

    public async Task CreateEventAsync(Event eventEntity)
       => await _eventRepository.AddAsync(eventEntity);

    public async Task UpdateEventAsync(Event eventEntity)
        => await _eventRepository.UpdateAsync(eventEntity);

    public async Task DeleteEventAsync(int eventId)
        => await _eventRepository.DeleteAsync(eventId);


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
