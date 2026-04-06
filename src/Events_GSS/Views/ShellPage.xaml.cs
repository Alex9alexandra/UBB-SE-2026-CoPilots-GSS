using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using Microsoft.Extensions.DependencyInjection;

using Events_GSS.Services;

namespace Events_GSS.Views;

public sealed partial class ShellPage : Page
{
    private readonly INavigationService _nav;

    public bool CanGoBack => _nav.CanGoBack;

    public ShellPage()
    {
        InitializeComponent();

        _nav = App.Services.GetRequiredService<INavigationService>();

        if (_nav is NavigationService concreteNav)
        {
            concreteNav.SetFrame(ContentFrame);
        }

        _nav.NavigateTo(PageKeys.EventListing);

        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void OnNavItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is string pageKey)
        {
            _nav.NavigateTo(pageKey);
        }
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        _nav.GoBack();
    }
}
