using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

using Events_GSS.ViewModels;
using Events_GSS.Services;

using Events_GSS.Data.Services.eventServices;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.Views;

/// <summary>
/// A user control that provides a multistep interface for creating new events.
/// </summary>
public sealed partial class CreateEventControl : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventControl"/> class.
    /// </summary>
    public CreateEventControl()
    {
        var userService = App.Services.GetRequiredService<IUserService>();
        var eventService = App.Services.GetRequiredService<IEventService>();
        var questService = App.Services.GetRequiredService<IQuestService>();
        var attendedEventService = App.Services.GetRequiredService<IAttendedEventService>();
        this.ViewModel = new CreateEventViewModel(userService, eventService, questService, attendedEventService);
        this.InitializeComponent();

        this.Step1View.ViewModel = this.ViewModel;
        this.Step2View.ViewModel = this.ViewModel;
        this.Step3View.ViewModel = this.ViewModel;

        this.ViewModel.CloseRequested += _ =>
        {
            this.Visibility = Visibility.Collapsed;
        };

        _ = this.ViewModel.LoadPresetQuestsAsync();
    }

    /// <summary>
    /// Gets the view model for the create event control.
    /// </summary>
    public CreateEventViewModel ViewModel { get; }
}
