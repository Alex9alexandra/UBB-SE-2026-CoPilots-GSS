using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.notificationServices;
using Events_GSS.Services.Interfaces;
using Events_GSS.ViewModels;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
using Events_GSS.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
namespace Events_GSS.Views
{
    public sealed partial class NotificationView : Page
    {
        public NotificationViewModel ViewModel { get; set; }

        public NotificationView()
        {
            InitializeComponent();
            ViewModel = new NotificationViewModel(
                App.Services.GetRequiredService<INotificationService>(),
                App.Services.GetRequiredService<IUserService>()
            );
            DataContext = ViewModel;

            Loaded += async (s, e) => await ViewModel.LoadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Notification notification)
                await ViewModel.DeleteAsync(notification);
        }
    }
}
