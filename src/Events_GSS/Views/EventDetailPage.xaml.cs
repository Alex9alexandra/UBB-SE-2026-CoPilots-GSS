using Events_GSS.Data.Models;
using Events_GSS.Data.Services.announcementServices;
using Events_GSS.Data.Services.discussionService;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Services;
using Events_GSS.Services.Interfaces;
using Events_GSS.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Events_GSS.Views;

/// <summary>
/// Event detail page that displays information about a specific event.
/// </summary>
public sealed partial class EventDetailPage : Page
{
    private INavigationService? navigationService;
    private Event? currentEvent;

    private IAttendedEventService? attendedEventService;
    private bool isEnrolled;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventDetailPage"/> class.
    /// </summary>
    public EventDetailPage()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Called when the page is navigated to. Initializes the page with event data and sets up view models.
    /// </summary>
    /// <param name="e">Navigation event arguments containing the event to display.</param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        this.navigationService = App.Services.GetRequiredService<INavigationService>();

        if (e.Parameter is not Event @event)
        {
            return;
        }

        this.currentEvent = @event;
        this.EventNameText.Text = @event.Name;
        this.EventInfoText.Text = $"{@event.StartDateTime:MMM dd, yyyy HH:mm} • {@event.Location}";

        var userService = App.Services.GetRequiredService<IUserService>();
        var currentUser = userService.GetCurrentUser();
        int userId = currentUser.UserId;
        bool isAdmin = @event.Admin?.UserId == userId;

        if (isAdmin)
        {
            this.StatisticsButton.Visibility = Visibility.Visible;
        }

        var announcementService = App.Services.GetRequiredService<IAnnouncementService>();
        var announcementViewModel = new AnnouncementViewModel(@event, announcementService, userId, isAdmin);
        this.AnnouncementTab.ViewModel = announcementViewModel;
        _ = announcementViewModel.InitializeAsync();

        var discussionService = App.Services.GetRequiredService<IDiscussionService>();
        var discussionViewModel = new DiscussionViewModel(@event, discussionService, userId, isAdmin);
        this.DiscussionTab.ViewModel = discussionViewModel;
        _ = discussionViewModel.InitializeAsync();

        this.QuestAdminTab.ViewModel = new QuestApprovalViewModel(new QuestAdminViewModel(@event));
        this.QuestUserTab.ViewModel = new QuestUserViewModel(@event);
        if (isAdmin)
        {
            this.QuestAdminTab.Visibility = Visibility.Visible;
            this.QuestUserTab.Visibility = Visibility.Collapsed;
        }

        var memoryService = App.Services.GetRequiredService<IMemoryService>();
        var memoryViewModel = new MemoryViewModel(memoryService);
        this.MemoryTab.ViewModel = memoryViewModel;
        _ = memoryViewModel.InitializeAsync(@event, currentUser);

        this.attendedEventService = App.Services.GetRequiredService<IAttendedEventService>();
        _ = this.LoadEnrollmentStatusAsync(@event, userId);
    }

    /// <summary>
    /// Handles the back button click event to navigate back to the previous page.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void OnBackClicked(object sender, RoutedEventArgs e)
    {
        this.navigationService?.GoBack();
    }

    /// <summary>
    /// Handles the statistics button click event to navigate to the event statistics page.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void OnStatisticsClicked(object sender, RoutedEventArgs e)
    {
        if (this.currentEvent != null)
        {
            this.navigationService?.NavigateTo(PageKeys.EventStatistics, this.currentEvent);
        }
    }

    /// <summary>
    /// Loads the enrollment status for the current user and updates the Join/Leave button.
    /// </summary>
    /// <param name="ev">The event to check enrollment for.</param>
    /// <param name="userId">The ID of the current user.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadEnrollmentStatusAsync(Event ev, int userId)
    {
        var attendedEvent = await this.attendedEventService!.GetAsync(ev.EventId, userId);
        this.isEnrolled = attendedEvent != null;
        this.JoinLeaveButton.Content = this.isEnrolled ? "Leave Event" : "Join Event";
    }

    /// <summary>
    /// Handles the Join/Leave button click event to enroll or unenroll the user from the event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private async void OnJoinLeaveClicked(object sender, RoutedEventArgs e)
    {
        if (this.currentEvent == null)
        {
            return;
        }

        var userService = App.Services.GetRequiredService<IUserService>();
        var userId = userService.GetCurrentUser().UserId;

        try
        {
            this.JoinLeaveButton.IsEnabled = false;

            if (this.isEnrolled)
            {
                await this.attendedEventService!.LeaveEventAsync(this.currentEvent.EventId, userId);
                this.isEnrolled = false;
                this.JoinLeaveButton.Content = "Join Event";
            }
            else
            {
                await this.attendedEventService!.AttendEventAsync(this.currentEvent.EventId, userId);
                this.isEnrolled = true;
                this.JoinLeaveButton.Content = "Leave Event";
            }
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Join/Leave failed: {exception.Message}");
        }
        finally
        {
            this.JoinLeaveButton.IsEnabled = true;
        }
    }
}
