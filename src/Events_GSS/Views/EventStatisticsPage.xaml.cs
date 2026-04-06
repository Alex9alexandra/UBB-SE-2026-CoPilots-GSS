using Events_GSS.Data.Models;
using Events_GSS.Data.Services.eventStatisticsServices;
using Events_GSS.Services;
using Events_GSS.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Events_GSS.Views;

public sealed partial class EventStatisticsPage : Page
{
    public EventStatisticsViewModel ViewModel { get; private set; } = null!;

    private INavigationService? _nav;

    public EventStatisticsPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _nav = App.Services.GetRequiredService<INavigationService>();

        if (e.Parameter is not Event ev) return;

        EventNameText.Text = ev.Name;

        var statsService = App.Services.GetRequiredService<IEventStatisticsService>();
        ViewModel = new EventStatisticsViewModel(statsService, ev);
        Bindings.Update();

        await ViewModel.InitializeAsync();

        EngagementRateText.Text = $"{ViewModel.ParticipantOverview.EngagementRate}%";
        ApprovedRateText.Text = $"{ViewModel.EngagementBreakdown.ApprovedQuestsRate}%";
        DeniedRateText.Text = $"{ViewModel.EngagementBreakdown.DeniedQuestsRate}%";
        ApprovedCountText.Text = ViewModel.EngagementBreakdown.ApprovedQuests.ToString();
        DeniedCountText.Text = ViewModel.EngagementBreakdown.DeniedQuests.ToString();
    }

    private void OnBackClicked(object sender, RoutedEventArgs e)
    {
        _nav?.GoBack();
    }
}
