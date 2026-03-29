using System;
using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories.eventRepository;


public interface IEventRepository
{
    Task<List<Event>> GetAllPublicActiveAsync();
    Task<Event?> GetByIdAsync(int eventId);
    Task AddAsync(Event eventEntity);
    Task UpdateAsync(Event eventEntity);
    Task DeleteAsync(int eventId);
};