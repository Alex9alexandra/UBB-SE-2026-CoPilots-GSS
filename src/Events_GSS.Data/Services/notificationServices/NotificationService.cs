using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.notificationRepository;

namespace Events_GSS.Data.Services.notificationServices
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }
        public async Task NotifyAsync(int userId, string title, string description)
        {
            User user = new User();
            user.UserId = userId;
            Notification notification = new Notification(user, title, description);
            await _notificationRepository.AddAsync(notification);
        }
        public async Task<List<Notification>> GetNotificationsAsync(int userId)
        {
            return await _notificationRepository.GetByUserIdAsync(userId);
        }
        
        public async Task DeleteAsync(int notificationId)
        {
            await _notificationRepository.DeleteAsync(notificationId);
        }
    }
}
