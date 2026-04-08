using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

using Microsoft.Extensions.DependencyInjection;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Services;

namespace Events_GSS.Views;

/// <summary>
/// Represents a page that displays a listing of public events.
/// </summary>
public sealed partial class EventListingPage : Page
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventListingPage"/> class.
    /// </summary>
    public EventListingPage()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the page is navigated to.
    /// </summary>
    /// <param name="e">The navigation event arguments.</param>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        try
        {
            var eventRepository = App.Services.GetRequiredService<IEventRepository>();
            var events = await eventRepository.GetAllPublicActiveAsync();
            this.EventsListView.ItemsSource = events;
        }
        catch (System.Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load events: {exception}");
        }
        finally
        {
            this.LoadingRing.IsActive = false;
            this.LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Handles the tapped event for an event item.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void OnEventTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement frameworkElement && frameworkElement.Tag is Event @event)
        {
            var navigationService = App.Services.GetRequiredService<INavigationService>();
            navigationService.NavigateTo(PageKeys.EventDetail, @event);
        }
    }

    /// <summary>
    /// Handles the click event for the create event button.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void OnCreateEventClicked(object sender, RoutedEventArgs e)
    {
        var navigationService = App.Services.GetRequiredService<INavigationService>();
        navigationService.NavigateTo(PageKeys.CreateEvent);
    }
}
