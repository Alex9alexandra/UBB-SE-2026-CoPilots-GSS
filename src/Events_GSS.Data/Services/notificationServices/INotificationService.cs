using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;
namespace Events_GSS.Data.Services.notificationServices
{
    public interface INotificationService
    {
        Task NotifyAsync(int userId, string title, string description);
        Task<List<Notification>> GetNotificationsAsync(int userId);
        Task DeleteAsync(int notificationId);
    }
}
