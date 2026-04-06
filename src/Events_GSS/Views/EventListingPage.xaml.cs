using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

using Microsoft.Extensions.DependencyInjection;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Services;

namespace Events_GSS.Views;

public sealed partial class EventListingPage : Page
{
    public EventListingPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        try
        {
            var eventRepo = App.Services.GetRequiredService<IEventRepository>();
            var events = await eventRepo.GetAllPublicActiveAsync();
            EventsListView.ItemsSource = events;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load events: {ex}");
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private void OnEventTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is Event ev)
        {
            var nav = App.Services.GetRequiredService<INavigationService>();
            nav.NavigateTo(PageKeys.EventDetail, ev);
        }
    }

    private void OnCreateEventClicked(object sender, RoutedEventArgs e)
    {
        var nav = App.Services.GetRequiredService<INavigationService>();
        nav.NavigateTo(PageKeys.CreateEvent);
    }
}
