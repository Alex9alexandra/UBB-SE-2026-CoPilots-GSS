using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using Microsoft.Extensions.DependencyInjection;

using Events_GSS.Services;

namespace Events_GSS.Views;

/// <summary>
/// Represents the page for creating new events.
/// </summary>
public sealed partial class CreateEventPage : Page
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventPage"/> class.
    /// </summary>
    public CreateEventPage()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the page is navigated to.
    /// </summary>
    /// <param name="e">Event data describing how this page was reached.</param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        this.CreateEventView.ViewModel.CloseRequested += _ =>
        {
            var nav = App.Services.GetRequiredService<INavigationService>();
            nav.GoBack();
        };
    }
}
