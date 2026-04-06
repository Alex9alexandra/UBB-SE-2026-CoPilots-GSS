using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.eventStatisticsRepository;

namespace Events_GSS.Data.Services.eventStatisticsServices;

public class EventStatisticsService : IEventStatisticsService
{
    private readonly IEventStatisticsRepository _repository;

    public EventStatisticsService(IEventStatisticsRepository repository)
    {
        _repository = repository;
    }

    public Task<ParticipantOverview> GetParticipantOverviewAsync(int eventId)
    {
        return _repository.GetParticipantOverviewAsync(eventId);
    }

    public Task<EngagementBreakdown> GetEngagementBreakdownAsync(int eventId)
    {
        return _repository.GetEngagementBreakdownAsync(eventId);
    }

    public Task<List<LeaderboardEntry>> GetLeaderboardAsync(int eventId)
    {
        return _repository.GetLeaderboardAsync(eventId);
    }

    public Task<List<QuestAnalyticsEntry>> GetQuestAnalyticsAsync(int eventId)
    {
        return _repository.GetQuestAnalyticsAsync(eventId);
    }
}
