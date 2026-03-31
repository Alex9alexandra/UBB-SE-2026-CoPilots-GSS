using Events_GSS.Data.Database;

using Microsoft.UI.Xaml;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.Interfaces;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.Services;
using Events_GSS.ViewModels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Events_GSS;

public sealed partial class MainWindow : Window
{
 //   public QuestAdminViewModel QuestViewModel { get; }
    public MemoryViewModel MemoriesViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

     //   IQuestService qs = App.Services.GetRequiredService<IQuestService>();
        IMemoryService memoryService = App.Services.GetRequiredService<IMemoryService>();


        // TODO: PlaceHolder for event data, replace when navigation is implemented
        var currentEvent = new Event { EventId = 1 };
        var currentUser = new User { UserId = 1 };

     //   QuestViewModel = new QuestAdminViewModel(currentEvent, qs);
        MemoriesViewModel = new MemoryViewModel(memoryService);
        this.Activated += async (s, e) =>
        {
            await MemoriesView.LoadAsync(currentEvent, currentUser);
        };

    }
}