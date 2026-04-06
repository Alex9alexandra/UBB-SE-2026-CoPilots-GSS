using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories.notificationRepository
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task<List<Notification>> GetByUserIdAsync(int userId);

        Task DeleteAsync(int notificationId);

    }
}
