using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

using Events_GSS.ViewModels;
using Events_GSS.Services;

using Microsoft.Extensions.DependencyInjection;
using Events_GSS.Data.Services.eventServices;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.Views;

public sealed partial class CreateEventControl : UserControl
{
    public CreateEventViewModel ViewModel { get; }

    public CreateEventControl()
    {
        var userService = App.Services.GetRequiredService<IUserService>();
        var eventService = App.Services.GetRequiredService<IEventService>();
        ViewModel = new CreateEventViewModel(userService, eventService);
        this.InitializeComponent();

        Step1View.ViewModel = ViewModel;
        Step2View.ViewModel = ViewModel;
        Step3View.ViewModel = ViewModel;

        ViewModel.CloseRequested += _ =>
        {
            this.Visibility = Visibility.Collapsed;
        };

        // Dummy repo for preset quests (not used by GetPresetQuestsAsync)
        var dummyRepo = new DummyQuestRepository();
        var questService = new Events_GSS.Data.Services.QuestService(dummyRepo);
        _ = ViewModel.LoadPresetQuestsAsync(questService);
    }

    // Minimal dummy repo implementation
    private class DummyQuestRepository : Events_GSS.Data.Repositories.IQuestRepository
    {
        public System.Threading.Tasks.Task<int> AddQuestAsync(Events_GSS.Data.Models.Event toEvent, Events_GSS.Data.Models.Quest quest) => System.Threading.Tasks.Task.FromResult(0);
        public System.Threading.Tasks.Task DeleteQuestAsync(Events_GSS.Data.Models.Quest quest) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task<System.Collections.Generic.List<Events_GSS.Data.Models.Quest>> GetQuestsAsync(Events_GSS.Data.Models.Event fromEvent) => System.Threading.Tasks.Task.FromResult(new System.Collections.Generic.List<Events_GSS.Data.Models.Quest>());
        public System.Threading.Tasks.Task<Events_GSS.Data.Models.Quest> GetQuestByIdAsync(int questId) => System.Threading.Tasks.Task.FromResult<Events_GSS.Data.Models.Quest>(null);
    }
}
