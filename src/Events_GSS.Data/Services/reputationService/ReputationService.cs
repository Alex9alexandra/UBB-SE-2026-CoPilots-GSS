using System.Diagnostics;

using CommunityToolkit.Mvvm.Messaging;

using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.achievementRepository;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Repositories.reputationRepository;

namespace Events_GSS.Data.Services.reputationService;

public class ReputationService : IReputationService
{
    private readonly IReputationRepository _reputationRepository;
    private readonly IAttendedEventRepository _attendedEventRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IAchievementRepository _achievementRepository;

    private static class ReputationDeltas
    {
        public const int EventCreated = 5;
        public const int EventCancelled = -20;
        public const int DiscussionMessagePosted = 1;
        public const int DiscussionMessageRemovedByAdmin = -10;
        public const int MemoryAddedWithPhoto = 4;
        public const int MemoryAddedTextOnly = 1;
        public const int QuestSubmitted = 6;
        public const int QuestApproved = 10;
        public const int QuestDenied = -4;
        public const int EventAttendedAdminBonus = 20;
    }

    private static class ReputationThresholds
    {
        public const int PostMemories = -300;
        public const int PostMessages = -500;
        public const int CreateEvents = -700;
        public const int AttendEvents = -1000;
    }

    private static readonly Dictionary<ReputationAction, int> reputationDeltasMap = new()
    {
        { ReputationAction.EventCreated, ReputationDeltas.EventCreated },
        { ReputationAction.EventCancelled, ReputationDeltas.EventCancelled },
        { ReputationAction.DiscussionMessagePosted, ReputationDeltas.DiscussionMessagePosted },
        { ReputationAction.DiscussionMessageRemovedByAdmin, ReputationDeltas.DiscussionMessageRemovedByAdmin },
        { ReputationAction.MemoryAddedWithPhoto, ReputationDeltas.MemoryAddedWithPhoto },
        { ReputationAction.MemoryAddedTextOnly, ReputationDeltas.MemoryAddedTextOnly },
        { ReputationAction.QuestSubmitted, ReputationDeltas.QuestSubmitted },
        { ReputationAction.QuestApproved, ReputationDeltas.QuestApproved },
        { ReputationAction.QuestDenied, ReputationDeltas.QuestDenied }
    };

    public ReputationService(
        IReputationRepository reputationRepository,
        IAttendedEventRepository attendedEventRepository,
        IEventRepository eventRepository,
        IAchievementRepository achievementRepository)
    {
        _reputationRepository = reputationRepository;
        _attendedEventRepository = attendedEventRepository;
        _eventRepository = eventRepository;
        _achievementRepository = achievementRepository;

        WeakReferenceMessenger.Default.Register<ReputationMessage>(this, (_, msg) =>
        {
            _ = HandleReputationChangeAsync(msg);
        });
    }

    public async Task<int> GetReputationPointsAsync(int userId)
    {
        return await _reputationRepository.GetReputationPointsAsync(userId);
    }

    public async Task<string> GetTierAsync(int userId)
    {
        return await _reputationRepository.GetTierAsync(userId);
    }

    public async Task<List<Achievement>> GetAchievementsAsync(int userId)
    {
        return await _achievementRepository.GetUserAchievementsAsync(userId);
    }

    public async Task<bool> CanPostMemoriesAsync(int userId)
    {
        var reputationPoints = await GetReputationPointsAsync(userId);
        return reputationPoints > ReputationThresholds.PostMessages;
    }

    public async Task<bool> CanPostMessagesAsync(int userId)
    {
        var reputationPoints = await GetReputationPointsAsync(userId);
        return reputationPoints > ReputationThresholds.PostMessages;
    }

    public async Task<bool> CanCreateEventsAsync(int userId)
    {
        var reputationPoints = await GetReputationPointsAsync(userId);
        return reputationPoints > ReputationThresholds.CreateEvents;
    }

    public async Task<bool> CanAttendEventsAsync(int userId)
    {
        var reputationPoints = await GetReputationPointsAsync(userId);
        return reputationPoints > ReputationThresholds.AttendEvents;
    }

    private async Task HandleReputationChangeAsync(ReputationMessage message)
    {
        try
        {
            if (message.Value == ReputationAction.EventAttended)
            {
                await HandleEventAttendedAsync(message.EventId!.Value);
                await _achievementRepository.CheckAndAwardAchievementsAsync(message.UserId);
                return;
            }
            //TODO Quest approval, denied, submitted handling, messages are sent

            if (reputationDeltasMap.TryGetValue(message.Value, out int delta))
            {
                await _reputationRepository.UpdateReputationAsync(message.UserId, delta);
                await _achievementRepository.CheckAndAwardAchievementsAsync(message.UserId);
            }
        }
        catch (Exception exception)
        {
            // RP updates are best-effort; don't crash the app
            Debug.WriteLine($"Reputation update failed: {exception}");
        }
    }

    private async Task HandleEventAttendedAsync(int eventId)
    {
        var attendeeCount = await _attendedEventRepository.GetAttendeeCountAsync(eventId);
        if (attendeeCount == 10)
        {
            var ev = await _eventRepository.GetByIdAsync(eventId);
            if (ev?.Admin != null)
            {
                await _reputationRepository.UpdateReputationAsync(ev.Admin.UserId, ReputationDeltas.EventAttendedAdminBonus);
            }
        }
    }
}
