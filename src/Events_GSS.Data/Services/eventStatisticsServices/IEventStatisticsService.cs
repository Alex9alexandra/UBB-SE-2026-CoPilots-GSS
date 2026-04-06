using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.eventStatisticsServices;

public interface IEventStatisticsService
{
    Task<ParticipantOverview> GetParticipantOverviewAsync(int eventId);
    Task<EngagementBreakdown> GetEngagementBreakdownAsync(int eventId);
    Task<List<LeaderboardEntry>> GetLeaderboardAsync(int eventId);
    Task<List<QuestAnalyticsEntry>> GetQuestAnalyticsAsync(int eventId);
}
