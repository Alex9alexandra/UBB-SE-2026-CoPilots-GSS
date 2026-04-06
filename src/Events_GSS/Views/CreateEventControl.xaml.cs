using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

using Events_GSS.ViewModels;
using Events_GSS.Services;

using Events_GSS.Data.Services.eventServices;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.Views;

public sealed partial class CreateEventControl : UserControl
{
    public CreateEventViewModel ViewModel { get; }

    public CreateEventControl()
    {
        var userService = App.Services.GetRequiredService<IUserService>();
        var eventService = App.Services.GetRequiredService<IEventService>();
        var questService = App.Services.GetRequiredService<IQuestService>();
        var attendedEventService = App.Services.GetRequiredService<IAttendedEventService>();
        ViewModel = new CreateEventViewModel(userService, eventService, questService, attendedEventService);
        this.InitializeComponent();

        Step1View.ViewModel = ViewModel;
        Step2View.ViewModel = ViewModel;
        Step3View.ViewModel = ViewModel;

        ViewModel.CloseRequested += _ =>
        {
            this.Visibility = Visibility.Collapsed;
        };

        _ = ViewModel.LoadPresetQuestsAsync(questService);
    }
}
