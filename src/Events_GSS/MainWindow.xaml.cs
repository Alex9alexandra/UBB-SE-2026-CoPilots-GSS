using System.Diagnostics;

using Events_GSS.Data.Database;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.Services;
using Events_GSS.ViewModels;
using Events_GSS.Data.Services.discussionService;
using Events_GSS.Views;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Events_GSS.Data.Services.announcementServices;
using Events_GSS.Services.Interfaces;

namespace Events_GSS;

public sealed partial class MainWindow : Window
{
    public QuestAdminViewModel QuestViewModel { get; }
    public DiscussionViewModel DiscussionViewModel { get; }
    public MemoryViewModel MemoriesViewModel { get; }

    public AnnouncementViewModel AnnouncementViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        //    var services = App.Services.GetRequiredService<IDiscussionService>();
        //    var announcementService = App.Services.GetRequiredService<IAnnouncementService>();
        //    IQuestService qs = App.Services.GetRequiredService<IQuestService>();
        //    IMemoryService memoryService = App.Services.GetRequiredService<IMemoryService>();
        //    IUserService userService = App.Services.GetRequiredService<IUserService>();
        //    IAttendedEventService attendedEventService = App.Services.GetRequiredService<IAttendedEventService>();

        //    // TODO: PlaceHolder for event data, replace when navigation is implemented
        //    var currentEvent = new Event { EventId = 1 };
        //    var currentUserId = 1; // Placeholder for current user ID, replace with actual user context
        //    bool isAdmin = false; // Placeholder for admin check, replace with actual logic
        //    var currentUser = new User { UserId = 1 };
        //    var currentUser2 = new User { UserId = 2 };
        //    var currentUser3 = new User { UserId = 3 };

        //    DiscussionViewModel = new DiscussionViewModel(currentEvent, services, currentUser3.UserId, isAdmin);
        //    QuestViewModel = new QuestAdminViewModel(currentEvent, qs);
        //    MemoriesViewModel = new MemoryViewModel(memoryService);
        //    AnnouncementViewModel = new AnnouncementViewModel(currentEvent, announcementService, currentUserId: 1, isAdmin: true);
        //    //this.Activated += async (s, e) =>
        //    //{
        //    //    await MemoriesView.LoadAsync(currentEvent, currentUser);
        //    //};

        //    //for the discussion view
        //    // in the xaml file
        //    //        <views:DiscussionControl 
        //    //ViewModel = "{x:Bind DiscussionViewModel}" />
        //    AttendedEventViewModel attendedEventViewModel = new AttendedEventViewModel(attendedEventService, userService);
        //    //AttendedEventView view = new AttendedEventView(attendedEventViewModel);
        //    //Content = view;

        //    /*
        //    this.Activated += async (s, e) =>
        //    {
        //        //await MemoriesView.LoadAsync(currentEvent, currentUser);
        //    };
        //    */


        ////for the announcement view
        //_ = AnnouncementViewModel.InitializeAsync();
        /*
         * <?xml version="1.0" encoding="utf-8"?>
        <Window
        x:Class="Events_GSS.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:views="using:Events_GSS.Views"
        mc:Ignorable="d"
        Title="Events_GSS">

        <Window.SystemBackdrop>
            <MicaBackdrop />
        </Window.SystemBackdrop>

        <Grid>
            <views:AnnouncementControl ViewModel="{x:Bind AnnouncementViewModel}" />

        </Grid>
        </Window>
        */

        RootFrame.Navigate(typeof(ShellPage));
    }
}