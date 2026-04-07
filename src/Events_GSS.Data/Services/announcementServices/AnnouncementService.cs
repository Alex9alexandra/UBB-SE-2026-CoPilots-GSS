using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services.announcementServices;
using Events_GSS.Data.Services.Interfaces;

namespace Events_GSS.Data.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository _repo;
    private readonly IEventRepository _eventRepo;

    public AnnouncementService(
        IAnnouncementRepository repo,
        IEventRepository eventRepo)
    {
        _repo = repo;
        _eventRepo = eventRepo;
    }

    public async Task<List<Announcement>> GetAnnouncementsAsync(int eventId, int userId)
    {
        return await _repo.GetAnnouncementsByEventAsync(eventId, userId);
    }

    public async Task CreateAnnouncementAsync(string message, int eventId, int userId)
    {
        await EnsureAdminAsync(eventId, userId);

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Announcement message cannot be empty.");

        var announcement = new Announcement(0, message.Trim(), DateTime.UtcNow);
        await _repo.AddAnnouncementAsync(announcement, eventId, userId);
    }

    public async Task UpdateAnnouncementAsync(int annId, string newMessage, int userId, int eventId)
    {
        await EnsureAdminAsync(eventId, userId);

        if (string.IsNullOrWhiteSpace(newMessage))
            throw new ArgumentException("Announcement message cannot be empty.");

        var existing = await _repo.GetAnnouncementByIdAsync(annId);
        if (existing is null)
            throw new KeyNotFoundException($"Announcement with ID {annId} does not exist.");

        await _repo.UpdateAnnouncementAsync(annId, newMessage.Trim());
    }

    public async Task DeleteAnnouncementAsync(int annId, int userId, int eventId)
    {
        await EnsureAdminAsync(eventId, userId);

        var existing = await _repo.GetAnnouncementByIdAsync(annId);
        if (existing is null)
            throw new KeyNotFoundException($"Announcement with ID {annId} does not exist.");

        await _repo.DeleteAnnouncementAsync(annId);
    }

    public async Task PinAnnouncementAsync(int annId, int eventId, int userId)
    {
        await EnsureAdminAsync(eventId, userId);

        // Unpin any currently pinned announcement for this event
        await _repo.UnpinAnnouncementAsync(eventId);
        // Pin the new one
        await _repo.PinAsync(annId, eventId);
    }

    public async Task MarkAsReadAsync(int annId, int userId)
    {
        await _repo.MarkAsReadAsync(annId, userId);
    }

    public async Task<(List<AnnouncementReadReceipt> Readers, int TotalParticipants)> GetReadReceiptsAsync(
        int annId, int eventId, int userId)
    {
        await EnsureAdminAsync(eventId, userId);

        var readers = await _repo.GetReadReceiptsAsync(annId);
        var total = await _repo.GetTotalParticipantsAsync(eventId);

        return (readers, total);
    }

    public async Task ReactAsync(int annId, int userId, string emoji)
    {
        await _repo.AddOrUpdateReactionAsync(annId, userId, emoji);
    }

    public async Task RemoveReactionAsync(int annId, int userId)
    {
        await _repo.RemoveReactionAsync(annId, userId);
    }
    public async Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId)
    {
        return await _repo.GetUnreadCountsForUserAsync(userId);
    }

    public async Task<List<User>> GetAllParticipantsAsync(int eventId)
    {
        return await _repo.GetAllParticipantsAsync(eventId);
    }

    private async Task EnsureAdminAsync(int eventId, int userId)
    {
        var ev = await _eventRepo.GetByIdAsync(eventId);
        if (ev is null)
            throw new ArgumentException($"Event with ID {eventId} does not exist.");
        if (ev.Admin?.UserId != userId)
            throw new UnauthorizedAccessException("Only the EventAdmin can perform this action.");
    }
}
