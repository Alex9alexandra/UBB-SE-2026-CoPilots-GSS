using Events_GSS.Data.Models;
using Events_GSS.Data.Services.reputationService;
using Events_GSS.Services;
using Events_GSS.Services.Interfaces;
using Events_GSS.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Events_GSS.Views
{
    public sealed partial class AttendedEventView : Page
    {
        public AttendedEventViewModel ViewModel { get; private set; } = null!;
        public AttendedEventView()
        {
            this.InitializeComponent();
        }

        public AttendedEventView(AttendedEventViewModel viewModel) : this()
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (ViewModel is null)
            {
                var attendedEventService = App.Services.GetRequiredService<IAttendedEventService>();
                var userService = App.Services.GetRequiredService<IUserService>();
                var reputationService = App.Services.GetRequiredService<IReputationService>();
                ViewModel = new AttendedEventViewModel(attendedEventService, userService, reputationService);
                DataContext = ViewModel;
            }

            await ViewModel.LoadAsync();
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

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Event ev)
            {
                var nav = App.Services.GetRequiredService<INavigationService>();
                nav.NavigateTo(PageKeys.EventDetail, ev);
            }
        }

        private async void FriendBox_Chosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is User friend)
            {
                sender.Text = friend.Name;
                await ViewModel.LoadCommonEventsAsync(friend);
            }
        }

    }
}
