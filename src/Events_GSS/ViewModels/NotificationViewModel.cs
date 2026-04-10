using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.notificationServices;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.ViewModels
{
    public class NotificationViewModel : INotifyPropertyChanged
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set { _isLoading = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Notification> _notifications = new();
        public ObservableCollection<Notification> Notifications
        {
            get { return _notifications; }
            set { _notifications = value; OnPropertyChanged(); }
        }

        public NotificationViewModel(INotificationService notificationService, IUserService userService)
        {
            _userService = userService;
            _notificationService = notificationService;
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            var currentUser = _userService.GetCurrentUser();
            var notifications = await _notificationService.GetNotificationsAsync(currentUser.UserId);
            Notifications = new ObservableCollection<Notification>(notifications);
            IsLoading = false;
        }

        public async Task DeleteAsync(Notification notification)
        {
            await _notificationService.DeleteAsync(notification.Id);
            Notifications.Remove(notification);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
