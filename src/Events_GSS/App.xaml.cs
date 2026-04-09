using System;

using Events_GSS.Data.Database;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.achievementRepository;
using Events_GSS.Data.Repositories.announcementRepository;
using Events_GSS.Data.Repositories.categoriesRepository;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Repositories.notificationRepository;
using Events_GSS.Data.Repositories.reputationRepository;
using Events_GSS.Data.Repositories.eventStatisticsRepository;

using Events_GSS.Data.Services;
using Events_GSS.Data.Services.announcementServices;
using Events_GSS.Data.Services.categoryServices;
using Events_GSS.Data.Services.discussionService;
using Events_GSS.Data.Services.eventServices;
using Events_GSS.Data.Services.eventStatisticsServices;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.Services.notificationServices;
using Events_GSS.Data.Services.reputationService;
using Events_GSS.Data.Services.achievementServices;
using Events_GSS.Services;
using Events_GSS.Services.Interfaces;
using Events_GSS.Views;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Events_GSS;

public partial class App : Application
{
    private Window? _window;

    public Window? MainWindow => _window;

    public new static App Current => (App)Application.Current;
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        string connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddSingleton(new SqlConnectionFactory(connectionString));

        services.AddTransient<IEventRepository, EventRepository>();
        services.AddTransient<ICategoryRepository, CategoryRepository>();
        services.AddTransient<IQuestRepository, QuestRepository>();
        services.AddTransient<IQuestMemoryRepository, QuestMemoryRepository>();
        services.AddTransient<IAnnouncementRepository, AnnouncementRepository>();
        services.AddTransient<IDiscussionRepository, DiscussionRepository>();
        services.AddTransient<IMemoryRepository, MemoryRepository>();
        services.AddTransient<IAttendedEventRepository, AttendedEventRepository>();
        services.AddTransient<INotificationRepository, NotificationRepository>();
        services.AddTransient<IReputationRepository, ReputationRepository>();
        services.AddTransient<IAchievementRepository, AchievementRepository>();
        services.AddTransient<IEventStatisticsRepository, EventStatisticsRepository>();

        services.AddTransient<IEventService, EventService>();
        services.AddTransient<ICategoryServices, CategoryServices>();
        services.AddTransient<IQuestService, QuestService>();
        services.AddTransient<IQuestApprovalService, QuestApprovalService>();
        services.AddTransient<IAnnouncementService, AnnouncementService>();
        services.AddTransient<IDiscussionService, DiscussionService>();
        services.AddTransient<IMemoryService, MemoryService>();
        services.AddTransient<IAttendedEventService, AttendedEventService>();
        services.AddTransient<IUserService, MockUserService>();
        services.AddTransient<INotificationService, NotificationService>();
        services.AddSingleton<IReputationService, ReputationService>();
        services.AddTransient<IAchievementService, AchievementService>();
        services.AddTransient<IEventStatisticsService, EventStatisticsService>();

        var navService = new NavigationService();
        navService.RegisterPage(PageKeys.EventListing, typeof(EventListingPage));
        navService.RegisterPage(PageKeys.MyEvents, typeof(AttendedEventView));
        navService.RegisterPage(PageKeys.Reputation, typeof(ReputationPage));
        navService.RegisterPage(PageKeys.EventDetail, typeof(EventDetailPage));
        navService.RegisterPage(PageKeys.CreateEvent, typeof(CreateEventPage));
        navService.RegisterPage(PageKeys.Notifications, typeof(NotificationView));
        navService.RegisterPage(PageKeys.EventStatistics, typeof(EventStatisticsPage));
        services.AddSingleton<INavigationService>(navService);

        Services = services.BuildServiceProvider();

        // Eagerly resolve so it subscribes to WeakReferenceMessenger immediately
        Services.GetRequiredService<IReputationService>();
    }
    

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}