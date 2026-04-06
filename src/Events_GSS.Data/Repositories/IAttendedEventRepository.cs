using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories
{
    public interface IAttendedEventRepository
    {
        Task AddAsync(AttendedEvent attendedEvent);
        Task DeleteAsync(int eventId, int userId);
        Task UpdateIsArchivedAsync(int eventId, int userId, bool isArchived);
        Task UpdateIsFavouriteAsync(int eventId, int userId, bool isFavourite);
        Task<AttendedEvent?> GetAsync(int eventId, int userId);
        Task<List<AttendedEvent>> GetByUserIdAsync(int userId);
        Task<List<AttendedEvent>> GetCommonEventsAsync(int userId, int friendId);
        Task<int> GetAttendeeCountAsync(int eventId);
    }
}
