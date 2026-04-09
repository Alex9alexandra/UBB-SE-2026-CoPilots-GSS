using Events_GSS.Data.Models;
using Events_GSS.Data.Services.announcementServices;
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
    /// <summary>
    /// A page view for displaying and managing attended events.
    /// </summary>
    public sealed partial class AttendedEventView : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttendedEventView"/> class.
        /// </summary>
        public AttendedEventView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendedEventView"/> class with the specified view model.
        /// </summary>
        /// <param name="viewModel">The view model for the attended event view.</param>
        public AttendedEventView(AttendedEventViewModel viewModel)
            : this()
        {
            this.ViewModel = viewModel;
            this.DataContext = this.ViewModel;
        }

        /// <summary>
        /// Gets the view model for the attended event view.
        /// </summary>
        public AttendedEventViewModel ViewModel { get; private set; } = null!;

        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (this.ViewModel is null)
            {
                var attendedEventService = App.Services.GetRequiredService<IAttendedEventService>();
                var userService = App.Services.GetRequiredService<IUserService>();
                var reputationService = App.Services.GetRequiredService<IReputationService>();
                var announcementService = App.Services.GetRequiredService<IAnnouncementService>();
                this.ViewModel = new AttendedEventViewModel(attendedEventService, userService, announcementService, reputationService);
                this.DataContext = this.ViewModel;
            }

            await this.ViewModel.LoadAsync();
        }

        /// <summary>
        /// Handles the click event for the archive button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data for the routed event.</param>
        private async void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AttendedEvent attendedEvent)
            {
                await this.ViewModel.SetArchivedAsync(attendedEvent);
            }
        }

        /// <summary>
        /// Handles the click event for the favourite button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data for the routed event.</param>
        private async void FavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AttendedEvent attendedEvent)
            {
                await this.ViewModel.SetFavouriteAsync(attendedEvent);
            }
        }

        /// <summary>
        /// Handles the click event for the leave button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data for the routed event.</param>
        private async void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AttendedEvent attendedEvent)
            {
                await this.ViewModel.LeaveAsync(attendedEvent);
            }
        }

        /// <summary>
        /// Handles the click event for the details button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data for the routed event.</param>
        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Event @event)
            {
                var navigationService = App.Services.GetRequiredService<INavigationService>();
                navigationService.NavigateTo(PageKeys.EventDetail, @event);
            }
        }

        /// <summary>
        /// Handles the suggestion chosen event for the friend AutoSuggestBox.
        /// </summary>
        /// <param name="sender">The AutoSuggestBox that fired the event.</param>
        /// <param name="args">Event data that describes the selected suggestion.</param>
        private async void FriendBox_Chosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is User friend)
            {
                sender.Text = friend.Name;
                await this.ViewModel.LoadCommonEventsAsync(friend);
            }
        }
    }
}
