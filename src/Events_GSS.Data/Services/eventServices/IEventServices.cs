using System;
using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.eventServices;

public interface IEventService
{
    Task<List<Event>> GetAllPublicActiveEventsAsync();
    Task<Event?> GetEventByIdAsync(int eventId);
    Task<int> CreateEventAsync(Event eventEntity);
    Task UpdateEventAsync(Event eventEntity);
    Task DeleteEventAsync(int eventId);

    Task<List<Event>> SearchByTitleAsync(string title);
    Task<List<Event>> FilterByCategoryAsync(string category);
    Task<List<Event>> FilterByLocationAsync(string location);
    Task<List<Event>> FilterByDateAsync(DateTime date);
    Task<List<Event>> FilterByDateRangeAsync(DateTime from, DateTime to);
}
