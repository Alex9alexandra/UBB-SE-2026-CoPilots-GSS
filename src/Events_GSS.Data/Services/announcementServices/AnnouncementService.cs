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

    public async Task CreateAnnouncementAsync(string announcementMessage, int eventId, int userId)
    {
        await EnsureAdminAsync(eventId, userId);

        if (string.IsNullOrWhiteSpace(announcementMessage))
            throw new ArgumentException("Announcement message cannot be empty.");

        var announcement = new Announcement(0, announcementMessage.Trim(), DateTime.UtcNow);
        await _repo.AddAnnouncementAsync(announcement, eventId, userId);
    }

    public async Task UpdateAnnouncementAsync(int announcementId, string newAnnouncementMessage, int userId, int eventId)
    {
        await EnsureAdminAsync(eventId, userId);

        if (string.IsNullOrWhiteSpace(newAnnouncementMessage))
            throw new ArgumentException("Announcement message cannot be empty.");

        var existingAnnouncement = await _repo.GetAnnouncementByIdAsync(announcementId);

        if (existingAnnouncement is null)
            throw new KeyNotFoundException($"Announcement with ID {announcementId} does not exist.");

        await _repo.UpdateAnnouncementAsync(announcementId, newAnnouncementMessage.Trim());
    }

    public async Task DeleteAnnouncementAsync(int announcementId, int userId, int eventId)
    {
        await EnsureAdminAsync(eventId, userId);

        var existingAnnouncement = await _repo.GetAnnouncementByIdAsync(announcementId);
        if (existingAnnouncement is null)
            throw new KeyNotFoundException($"Announcement with ID {announcementId} does not exist.");

        await _repo.DeleteAnnouncementAsync(announcementId);
    }

    public async Task PinAnnouncementAsync(int announcementId, int eventId, int userId)
    {
        await EnsureAdminAsync(eventId, userId);

        // Unpin any currently pinned announcement for this event
        await _repo.UnpinAnnouncementAsync(eventId);
        // Pin the new one
        await _repo.PinAsync(announcementId, eventId);
    }

    // Marks announcements as read
    public async Task MarkAsReadAsync(int announcementId, int userId)
    {
        await _repo.MarkAsReadAsync(announcementId, userId);
    }

    public async Task<(List<AnnouncementReadReceipt> Readers, int TotalParticipants)> GetReadReceiptsAsync(
        int announcementId, int eventId, int userId)
    {
        await EnsureAdminAsync(eventId, userId);

        var readers = await _repo.GetReadReceiptsAsync(announcementId);
        var totalParticipants = await _repo.GetTotalParticipantsAsync(eventId);

        return (readers, totalParticipants);
    }

    public async Task AddOrUpdateReactAsync(int announcementId, int userId, string emoji)
    {
        await _repo.AddOrUpdateReactionAsync(announcementId, userId, emoji);
    }

    public async Task RemoveReactionAsync(int announcementId, int userId)
    {
        await _repo.RemoveReactionAsync(announcementId, userId);
    }

    // Gets number of unread announcements for the current user (only for the events he's participating in)
    public async Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId)
    {
        return await _repo.GetUnreadCountsForUserAsync(userId);
    }

    public async Task<List<User>> GetAllParticipantsAsync(int eventId)
    {
        return await _repo.GetAllParticipantsAsync(eventId);
    }

    // Ensures the current user is the admin of the event before allowing restricted operations
    private async Task EnsureAdminAsync(int eventId, int userId)
    {
        var selectedEvent = await _eventRepo.GetByIdAsync(eventId); 
        if (selectedEvent is null)
            throw new ArgumentException($"Event with ID {eventId} does not exist.");
        if (selectedEvent.Admin?.UserId != userId)
            throw new UnauthorizedAccessException("Only the EventAdmin can perform this action.");
    }
}
