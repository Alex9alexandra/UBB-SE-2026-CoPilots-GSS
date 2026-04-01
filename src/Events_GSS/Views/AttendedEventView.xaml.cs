using Events_GSS.Data.Models;
using Events_GSS.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Events_GSS.Views
{
    public sealed partial class AttendedEventView : Page
    {
        public AttendedEventViewModel ViewModel { get; }

        public AttendedEventView(AttendedEventViewModel viewModel)
        {
            this.InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;

            Loaded += async (s, e) => await ViewModel.LoadAsync();
        }

        private async void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AttendedEvent ae)
                await ViewModel.SetArchivedAsync(ae);
        }

        private async void FavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AttendedEvent ae)
                await ViewModel.SetFavouriteAsync(ae);
        }

        private async void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AttendedEvent ae)
                await ViewModel.LeaveAsync(ae);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.LoadAsync();
        }
    }
}