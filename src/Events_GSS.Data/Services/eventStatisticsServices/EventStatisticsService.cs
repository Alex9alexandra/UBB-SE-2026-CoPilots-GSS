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

    public async Task<ParticipantOverview> GetParticipantOverviewAsync(int eventId)
    {
        var result = await _repository.GetParticipantOverviewAsync(eventId);

        if (result.TotalParticipants > 0)
        {
            result.EngagementRate = Math.Round(
                (double)result.ActiveParticipants / result.TotalParticipants * 100,
                2);
        }
        else
        {
            result.EngagementRate = 0;
        }

        return result;
    }

    public async Task<EngagementBreakdown> GetEngagementBreakdownAsync(int eventId)
    {
        var result = await _repository.GetEngagementBreakdownAsync(eventId);

        if (result.TotalQuestSubmissions > 0)
        {
            result.ApprovedQuestsRate = Math.Round(
                (double)result.ApprovedQuests / result.TotalQuestSubmissions * 100,
                2);

            result.DeniedQuestsRate = Math.Round(
                100 - result.ApprovedQuestsRate,
                2);
        }
        else
        {
            result.ApprovedQuestsRate = 0;
            result.DeniedQuestsRate = 0;
        }

        return result;
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
