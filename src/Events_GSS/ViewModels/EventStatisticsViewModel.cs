using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.eventStatisticsServices;

namespace Events_GSS.ViewModels;

public class EventStatisticsViewModel : INotifyPropertyChanged
{
    private readonly IEventStatisticsService _statisticsService;
    private readonly Event _event;

    private bool _isLoading;
    private string? _errorMessage;

    private ParticipantOverview _participantOverview = new();
    private EngagementBreakdown _engagementBreakdown = new();
    private ObservableCollection<LeaderboardEntry> _leaderboard = new();
    private ObservableCollection<QuestAnalyticsEntry> _questAnalytics = new();

    public EventStatisticsViewModel(IEventStatisticsService statisticsService, Event currentEvent)
    {
        _statisticsService = statisticsService;
        _event = currentEvent;
    }

    public string EventName => _event.Name;

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    public ParticipantOverview ParticipantOverview
    {
        get => _participantOverview;
        private set { _participantOverview = value; OnPropertyChanged(); }
    }

    public EngagementBreakdown EngagementBreakdown
    {
        get => _engagementBreakdown;
        private set { _engagementBreakdown = value; OnPropertyChanged(); }
    }

    public ObservableCollection<LeaderboardEntry> Leaderboard
    {
        get => _leaderboard;
        private set { _leaderboard = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLeaderboardEntries)); }
    }

    public bool HasLeaderboardEntries => _leaderboard.Count > 0;

    public ObservableCollection<QuestAnalyticsEntry> QuestAnalytics
    {
        get => _questAnalytics;
        private set { _questAnalytics = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasQuestAnalytics)); }
    }

    public bool HasQuestAnalytics => _questAnalytics.Count > 0;

    public async Task InitializeAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var overviewTask = _statisticsService.GetParticipantOverviewAsync(_event.EventId);
            var breakdownTask = _statisticsService.GetEngagementBreakdownAsync(_event.EventId);
            var leaderboardTask = _statisticsService.GetLeaderboardAsync(_event.EventId);
            var questAnalyticsTask = _statisticsService.GetQuestAnalyticsAsync(_event.EventId);

            await Task.WhenAll(overviewTask, breakdownTask, leaderboardTask, questAnalyticsTask);

            ParticipantOverview = overviewTask.Result;
            EngagementBreakdown = breakdownTask.Result;
            Leaderboard = new ObservableCollection<LeaderboardEntry>(leaderboardTask.Result);
            QuestAnalytics = new ObservableCollection<QuestAnalyticsEntry>(questAnalyticsTask.Result);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load statistics: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
