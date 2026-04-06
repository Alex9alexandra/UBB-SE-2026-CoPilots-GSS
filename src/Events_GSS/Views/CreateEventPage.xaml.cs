using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using Microsoft.Extensions.DependencyInjection;

using Events_GSS.Services;

namespace Events_GSS.Views;

public sealed partial class CreateEventPage : Page
{
    public CreateEventPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        CreateEventView.ViewModel.CloseRequested += _ =>
        {
            var nav = App.Services.GetRequiredService<INavigationService>();
            nav.GoBack();
        };
    }
}
