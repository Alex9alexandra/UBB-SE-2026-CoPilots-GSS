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
    private readonly IReputationRepository _reputationRepo;
    private readonly IAttendedEventRepository _attendedEventRepo;
    private readonly IEventRepository _eventRepo;
    private readonly IAchievementRepository _achievementRepo;

    private static readonly Dictionary<ReputationAction, int> RpDeltas = new()
    {
        { ReputationAction.EventCreated, 5 },
        { ReputationAction.EventCancelled, -20 },
        { ReputationAction.DiscussionMessagePosted, 1 },
        { ReputationAction.DiscussionMessageRemovedByAdmin, -10 },
        { ReputationAction.MemoryAddedWithPhoto, 4 },
        { ReputationAction.MemoryAddedTextOnly, 1 },
        { ReputationAction.QuestSubmitted, 6 },
        { ReputationAction.QuestApproved, 10 },
        { ReputationAction.QuestDenied, -4 },
    };

    public ReputationService(
        IReputationRepository reputationRepo,
        IAttendedEventRepository attendedEventRepo,
        IEventRepository eventRepo,
        IAchievementRepository achievementRepo)
    {
        _reputationRepo = reputationRepo;
        _attendedEventRepo = attendedEventRepo;
        _eventRepo = eventRepo;
        _achievementRepo = achievementRepo;

        WeakReferenceMessenger.Default.Register<ReputationMessage>(this, (_, msg) =>
        {
            _ = HandleReputationChangeAsync(msg);
        });
    }

    public async Task<int> GetReputationPointsAsync(int userId)
    {
        return await _reputationRepo.GetReputationPointsAsync(userId);
    }

    public async Task<string> GetTierAsync(int userId)
    {
        return await _reputationRepo.GetTierAsync(userId);
    }

    public async Task<List<Achievement>> GetAchievementsAsync(int userId)
    {
        return await _achievementRepo.GetUserAchievementsAsync(userId);
    }

    public async Task<bool> CanPostMemoriesAsync(int userId)
    {
        var rp = await GetReputationPointsAsync(userId);
        return rp > -300;
    }

    public async Task<bool> CanPostMessagesAsync(int userId)
    {
        var rp = await GetReputationPointsAsync(userId);
        return rp > -500;
    }

    public async Task<bool> CanCreateEventsAsync(int userId)
    {
        var rp = await GetReputationPointsAsync(userId);
        return rp > -700;
    }

    public async Task<bool> CanAttendEventsAsync(int userId)
    {
        var rp = await GetReputationPointsAsync(userId);
        return rp > -1000;
    }

    private async Task HandleReputationChangeAsync(ReputationMessage message)
    {
        try
        {
            if (message.Value == ReputationAction.EventAttended)
            {
                await HandleEventAttendedAsync(message.EventId!.Value);
                await _achievementRepo.CheckAndAwardAchievementsAsync(message.UserId);
                return;
            }
            //TODO Quest approval, denied, submitted handling, messages are sent

            if (RpDeltas.TryGetValue(message.Value, out int delta))
            {
                await _reputationRepo.UpdateReputationAsync(message.UserId, delta);
                await _achievementRepo.CheckAndAwardAchievementsAsync(message.UserId);
            }
        }
        catch (Exception)
        {
            // RP updates are best-effort; don't crash the app
        }
    }

    private async Task HandleEventAttendedAsync(int eventId)
    {
        var count = await _attendedEventRepo.GetAttendeeCountAsync(eventId);
        if (count == 10)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev?.Admin != null)
            {
                await _reputationRepo.UpdateReputationAsync(ev.Admin.UserId, 20);
            }
        }
    }
}
