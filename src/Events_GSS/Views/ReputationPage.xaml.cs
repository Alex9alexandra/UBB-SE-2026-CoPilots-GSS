using Events_GSS.Data.Services.reputationService;
using Events_GSS.Services.Interfaces;
using Events_GSS.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Events_GSS.Views;

public sealed partial class ReputationPage : Page
{
    public ReputationViewModel ViewModel { get; private set; } = null!;

    public ReputationPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (ViewModel is null)
        {
            var userService = App.Services.GetRequiredService<IUserService>();
            var reputationService = App.Services.GetRequiredService<IReputationService>();
            ViewModel = new ReputationViewModel(userService, reputationService);
            DataContext = ViewModel;
        }

        await ViewModel.LoadAsync();
    }
}
