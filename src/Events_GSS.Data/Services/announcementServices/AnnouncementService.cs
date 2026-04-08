// <copyright file="AnnouncementService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services.announcementServices;

using System.Diagnostics.CodeAnalysis;

public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository _repo;
    private readonly IEventRepository _eventRepo;

    public AnnouncementService(
        IAnnouncementRepository repo,
        IEventRepository eventRepo)
    {
        this._repo = repo;
        this._eventRepo = eventRepo;
    }

    public async Task<List<Announcement>> GetAnnouncementsAsync(int eventId, int userId)
    {
        var announcements = await _repo.GetAnnouncementsByEventAsync(eventId, userId);

        var reactions = await _repo.GetReactionsAsync(
            announcements.Select(a => a.Id).ToList());

        AttachReactions(announcements, reactions);

        return announcements;
    }

    public async Task CreateAnnouncementAsync(string announcementMessage, int eventId, int userId)
    {
        await this.EnsureAdminAsync(eventId, userId);

        if (string.IsNullOrWhiteSpace(announcementMessage))
        {
            throw new ArgumentException("Announcement message cannot be empty.");
        }

        var announcement = new Announcement(0, announcementMessage.Trim(), DateTime.UtcNow);
        await this._repo.AddAnnouncementAsync(announcement, eventId, userId);
    }

    public async Task UpdateAnnouncementAsync(int announcementId, string newAnnouncementMessage, int userId, int eventId)
    {
        await this.EnsureAdminAsync(eventId, userId);

        if (string.IsNullOrWhiteSpace(newAnnouncementMessage))
        {
            throw new ArgumentException("Announcement message cannot be empty.");
        }

        var existingAnnouncement = await this._repo.GetAnnouncementByIdAsync(announcementId);

        if (existingAnnouncement is null)
        {
            throw new KeyNotFoundException($"Announcement with ID {announcementId} does not exist.");
        }

        await this._repo.UpdateAnnouncementAsync(announcementId, newAnnouncementMessage.Trim());
    }

    public async Task DeleteAnnouncementAsync(int announcementId, int userId, int eventId)
    {
        await this.EnsureAdminAsync(eventId, userId);

        var existingAnnouncement = await this._repo.GetAnnouncementByIdAsync(announcementId);
        if (existingAnnouncement is null)
        {
            throw new KeyNotFoundException($"Announcement with ID {announcementId} does not exist.");
        }

        await this._repo.DeleteAnnouncementAsync(announcementId);
    }

    public async Task PinAnnouncementAsync(int announcementId, int eventId, int userId)
    {
        await EnsureAdminAsync(eventId, userId);

        // business rule: only one pinned per event
        await _repo.UnpinAnnouncementAsync(eventId);

        // pin selected announcement
        await _repo.PinAsync(announcementId);
    }

    // Marks announcements as read
    public async Task<bool> MarkAsReadAsync(int announcementId, int userId)
    {
        var alreadyRead = await _repo.HasUserReadAsync(announcementId, userId);

        if (alreadyRead)
            return false;

        await _repo.InsertReadReceiptAsync(announcementId, userId);
        return true;
    }

    public async Task<(List<AnnouncementReadReceipt> Readers, int TotalParticipants)> GetReadReceiptsAsync(
        int announcementId, int eventId, int userId)
    {
        await this.EnsureAdminAsync(eventId, userId);

        var readers = await this._repo.GetReadReceiptsAsync(announcementId);
        var totalParticipants = await this._repo.GetTotalParticipantsAsync(eventId);

        return (readers, totalParticipants);
    }

    public async Task AddOrUpdateReactAsync(int announcementId, int userId, string emoji)
    {
        var existingEmoji = await _repo.GetUserReactionAsync(announcementId, userId);

        if (existingEmoji == null)
        {
            await _repo.InsertReactionAsync(announcementId, userId, emoji);
            return;
        }

        if (existingEmoji == emoji)
        {
            await _repo.RemoveReactionAsync(announcementId, userId);
            return;
        }

        await _repo.UpdateReactionAsync(announcementId, userId, emoji);
    }

    public async Task RemoveReactionAsync(int announcementId, int userId)
    {
        await this._repo.RemoveReactionAsync(announcementId, userId);
    }

    // Gets number of unread announcements for the current user (only for the events he's participating in)
    public async Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId)
    {
        return await this._repo.GetUnreadCountsForUserAsync(userId);
    }

    [ExcludeFromCodeCoverage]
    public async Task<List<User>> GetAllParticipantsAsync(int eventId)
    {
        return await this._repo.GetAllParticipantsAsync(eventId);
    }

    // Ensures the current user is the admin of the event before allowing restricted operations
    private async Task EnsureAdminAsync(int eventId, int userId)
    {
        var selectedEvent = await this._eventRepo.GetByIdAsync(eventId);
        if (selectedEvent is null)
        {
            throw new ArgumentException($"Event with ID {eventId} does not exist.");
        }

        if (selectedEvent.Admin?.UserId != userId)
        {
            throw new UnauthorizedAccessException("Only the EventAdmin can perform this action.");
        }
    }

    // Updates or adds reaction emoji and, in the case the user selects the same one as before, it removes it entirely
    public async Task ToggleReactionAsync(int announcementId, int userId, string emoji)
    {
        var existingEmoji = await _repo.GetUserReactionAsync(announcementId, userId);

        if (existingEmoji == emoji)
        {
            await this._repo.RemoveReactionAsync(announcementId, userId);
        }
        else
        {
            await this.AddOrUpdateReactAsync(announcementId, userId, emoji);
        }
    }

    public async Task<bool> MarkAsReadIfNeededAsync(int announcementId, int userId, bool isAlreadyRead)
    {
        if (isAlreadyRead)
        {
            return false;
        }

        await this.MarkAsReadAsync(announcementId, userId);
        return true;
    }

    public async Task<List<User>> GetNonReadersAsync(int announcementId, int eventId)
    {
        var readers = await this._repo.GetReadReceiptsAsync(announcementId);
        var participants = await this._repo.GetAllParticipantsAsync(eventId);

        var readerIds = readers
            .Select(r => r.User.UserId)
            .ToHashSet();

        var nonReaders = participants
            .Where(p => !readerIds.Contains(p.UserId))
            .ToList();

        return nonReaders;
    }

    public void AttachReactions(
    List<Announcement> announcements,
    List<(int AnnouncementId, AnnouncementReaction Reaction)> reactions)
    {
        var grouped = reactions
            .GroupBy(r => r.AnnouncementId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Reaction).ToList());

        foreach (var announcement in announcements)
        {
            if (grouped.TryGetValue(announcement.Id, out var listOfReactions))
            {
                announcement.Reactions = listOfReactions;
            }
        }
    }
}
