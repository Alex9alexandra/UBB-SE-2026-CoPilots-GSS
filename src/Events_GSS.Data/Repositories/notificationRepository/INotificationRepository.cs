using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories.notificationRepository
{
    public interface INotificationRepository
    {
        Task AddAsync(int userId, string title, string description, DateTime createdAt);
        Task<List<Notification>> GetByUserIdAsync(int userId);

        Task DeleteAsync(int notificationId);

    }
}
