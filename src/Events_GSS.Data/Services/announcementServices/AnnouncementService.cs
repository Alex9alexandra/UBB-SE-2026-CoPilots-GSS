// <copyright file="AnnouncementService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Services.announcementServices;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents the service for managing announcements.
/// </summary>
public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository announcementRepository;
    private readonly IEventRepository eventRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnnouncementService"/> class.
    /// </summary>
    /// <param name="announcementRepository">The repository used to manage announcements.</param>
    /// <param name="eventRepository">The repository used to access event data.</param>
    public AnnouncementService(
        IAnnouncementRepository announcementRepository,
        IEventRepository eventRepository)
    {
        this.announcementRepository = announcementRepository ?? throw new ArgumentNullException(nameof(announcementRepository));
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
    }

    /// <summary>
    /// Gets all announcements for a specific event and user.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A <see cref="Task"/> representing all announcements for the specified event and user.</returns>
    public async Task<List<Announcement>> GetAnnouncementsAsync(int eventId, int userId)
    {
        var announcements = await this.announcementRepository.GetAnnouncementsByEventAsync(eventId, userId);

        var reactions = await this.announcementRepository.GetReactionsAsync(
            announcements.Select(a => a.Id).ToList());

        this.AttachReactions(announcements, reactions);

        return announcements;
    }

    /// <summary>
    /// Creates a new announcement for the specified event as the specified user.
    /// </summary>
    /// <param name="announcementMessage">The message content of the announcement to create. Cannot be null, empty, or consist only of white-space
    /// characters.</param>
    /// <param name="eventId">The identifier of the event for which the announcement is being created.</param>
    /// <param name="userId">The identifier of the user creating the announcement. The user must have administrative privileges for the
    /// event.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="announcementMessage"/> is null, empty, or consists only of white-space characters.</exception>
    public async Task CreateAnnouncementAsync(string announcementMessage, int eventId, int userId)
    {
        await this.EnsureAdminAsync(eventId, userId);

        if (string.IsNullOrWhiteSpace(announcementMessage))
        {
            throw new ArgumentException("Announcement message cannot be empty.");
        }

        var announcement = new Announcement(0, announcementMessage.Trim(), DateTime.UtcNow);
        await this.announcementRepository.AddAnnouncementAsync(announcement, eventId, userId);
    }

    /// <summary>
    /// Asynchronously updates the message of an existing announcement for a specified event.
    /// </summary>
    /// <param name="announcementId">The unique identifier of the announcement to update.</param>
    /// <param name="newAnnouncementMessage">The new message content for the announcement. Cannot be null, empty, or whitespace.</param>
    /// <param name="userId">The unique identifier of the user performing the update. The user must have administrative privileges for the
    /// event.</param>
    /// <param name="eventId">The unique identifier of the event associated with the announcement.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="newAnnouncementMessage"/> is null, empty, or consists only of white-space characters.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if an announcement with the specified <paramref name="announcementId"/> does not exist.</exception>
    public async Task UpdateAnnouncementAsync(int announcementId, string newAnnouncementMessage, int userId, int eventId)
    {
        await this.EnsureAdminAsync(eventId, userId);

        if (string.IsNullOrWhiteSpace(newAnnouncementMessage))
        {
            throw new ArgumentException("Announcement message cannot be empty.");
        }

        var existingAnnouncement = await this.announcementRepository.GetAnnouncementByIdAsync(announcementId);

        if (existingAnnouncement is null)
        {
            throw new KeyNotFoundException($"Announcement with ID {announcementId} does not exist.");
        }

        await this.announcementRepository.UpdateAnnouncementAsync(announcementId, newAnnouncementMessage.Trim());
    }

    /// <summary>
    /// Deletes the specified announcement from the event asynchronously.
    /// </summary>
    /// <param name="announcementId">The unique identifier of the announcement to delete.</param>
    /// <param name="userId">The unique identifier of the user performing the deletion. The user must have administrative privileges for the
    /// event.</param>
    /// <param name="eventId">The unique identifier of the event containing the announcement.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if an announcement with the specified announcementId does not exist.</exception>
    public async Task DeleteAnnouncementAsync(int announcementId, int userId, int eventId)
    {
        await this.EnsureAdminAsync(eventId, userId);

        var existingAnnouncement = await this.announcementRepository.GetAnnouncementByIdAsync(announcementId);
        if (existingAnnouncement is null)
        {
            throw new KeyNotFoundException($"Announcement with ID {announcementId} does not exist.");
        }

        await this.announcementRepository.DeleteAnnouncementAsync(announcementId);
    }

    /// <summary>
    /// Pins the specified announcement for the given event, ensuring only one announcement is pinned at a time.
    /// </summary>
    /// <remarks>If another announcement is already pinned for the event, it will be unpinned before pinning
    /// the specified announcement. The method requires the user to have administrative rights for the event.</remarks>
    /// <param name="announcementId">The unique identifier of the announcement to pin.</param>
    /// <param name="eventId">The unique identifier of the event for which the announcement will be pinned.</param>
    /// <param name="userId">The unique identifier of the user performing the operation. The user must have administrative privileges for the
    /// event.</param>
    /// <returns>A task that represents the asynchronous pin operation.</returns>
    public async Task PinAnnouncementAsync(int announcementId, int eventId, int userId)
    {
        await this.EnsureAdminAsync(eventId, userId);

        // business rule: only one pinned per event
        await this.announcementRepository.UnpinAnnouncementAsync(eventId);

        // pin selected announcement
        await this.announcementRepository.PinAsync(announcementId);
    }

    /// <summary>
    /// Marks the specified announcement as read for the given user asynchronously, if it has not already been marked as
    /// read.
    /// </summary>
    /// <param name="announcementId">The unique identifier of the announcement to mark as read.</param>
    /// <param name="userId">The unique identifier of the user for whom the announcement is being marked as read.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the announcement
    /// was newly marked as read for the user; otherwise, <see langword="false"/> if it was already marked as read.</returns>
    public async Task<bool> MarkAsReadAsync(int announcementId, int userId)
    {
        var alreadyRead = await this.announcementRepository.HasUserReadAsync(announcementId, userId);

        if (alreadyRead)
        {
            return false;
        }

        await this.announcementRepository.InsertReadReceiptAsync(announcementId, userId);
        return true;
    }

    /// <summary>
    /// Asynchronously retrieves the list of users who have read a specific announcement and the total number of
    /// participants for the associated event.
    /// </summary>
    /// <remarks>The caller must have administrative rights for the specified event; otherwise, an exception
    /// may be thrown. The returned list contains only users who have read the announcement, while the total participant
    /// count includes all event participants.</remarks>
    /// <param name="announcementId">The unique identifier of the announcement for which to retrieve read receipts.</param>
    /// <param name="eventId">The unique identifier of the event associated with the announcement.</param>
    /// <param name="userId">The unique identifier of the user requesting the read receipts. The user must have administrative privileges for
    /// the event.</param>
    /// <returns>A tuple containing a list of read receipts for the announcement and the total number of participants in the
    /// event.</returns>
    public async Task<(List<AnnouncementReadReceipt> Readers, int TotalParticipants)> GetReadReceiptsAsync(
        int announcementId, int eventId, int userId)
    {
        await this.EnsureAdminAsync(eventId, userId);

        var readers = await this.announcementRepository.GetReadReceiptsAsync(announcementId);
        var totalParticipants = await this.announcementRepository.GetTotalParticipantsAsync(eventId);

        return (readers, totalParticipants);
    }

    /// <summary>
    /// Adds a new reaction or updates an existing reaction for the specified announcement and user using the provided
    /// emoji.
    /// </summary>
    /// <remarks>If the user has not previously reacted, a new reaction is added. If the user reacts with the
    /// same emoji as before, the reaction is removed. Otherwise, the existing reaction is updated to the new
    /// emoji.</remarks>
    /// <param name="announcementId">The identifier of the announcement to which the reaction is associated.</param>
    /// <param name="userId">The identifier of the user adding or updating the reaction.</param>
    /// <param name="emoji">The emoji representing the reaction to add or update. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task AddOrUpdateReactAsync(int announcementId, int userId, string emoji)
    {
        var existingEmoji = await this.announcementRepository.GetUserReactionAsync(announcementId, userId);

        if (existingEmoji == null)
        {
            await this.announcementRepository.InsertReactionAsync(announcementId, userId, emoji);
            return;
        }

        if (existingEmoji == emoji)
        {
            await this.announcementRepository.RemoveReactionAsync(announcementId, userId);
            return;
        }

        await this.announcementRepository.UpdateReactionAsync(announcementId, userId, emoji);
    }

    /// <summary>
    /// Removes a user's reaction from the specified announcement asynchronously.
    /// </summary>
    /// <param name="announcementId">The unique identifier of the announcement from which the reaction will be removed.</param>
    /// <param name="userId">The unique identifier of the user whose reaction is to be removed.</param>
    /// <returns>A task that represents the asynchronous remove operation.</returns>
    public async Task RemoveReactionAsync(int announcementId, int userId)
    {
        await this.announcementRepository.RemoveReactionAsync(announcementId, userId);
    }

    /// <summary>
    /// Asynchronously retrieves the number of unread items for each conversation associated with the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user for whom to retrieve unread counts. Must be a valid user ID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary mapping conversation
    /// IDs to the number of unread items for each conversation. If the user has no conversations, the dictionary will
    /// be empty.</returns>
    public async Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(int userId)
    {
        return await this.announcementRepository.GetUnreadCountsForUserAsync(userId);
    }

    /// <summary>
    /// Asynchronously retrieves all participants associated with the specified event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event for which to retrieve participants.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of users participating in the
    /// specified event. The list will be empty if no participants are found.</returns>
    [ExcludeFromCodeCoverage]
    public async Task<List<User>> GetAllParticipantsAsync(int eventId)
    {
        return await this.announcementRepository.GetAllParticipantsAsync(eventId);
    }

    /// <summary>
    /// Toggles the specified emoji reaction for a user on an announcement. Adds the reaction if it does not exist, or
    /// removes it if it is already present.
    /// </summary>
    /// <remarks>If the user has already reacted with the specified emoji, the reaction is removed; otherwise,
    /// the reaction is added or updated. This method does not throw if the reaction does not exist.</remarks>
    /// <param name="announcementId">The unique identifier of the announcement to which the reaction is applied.</param>
    /// <param name="userId">The unique identifier of the user toggling the reaction.</param>
    /// <param name="emoji">The emoji representing the reaction to toggle. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous toggle operation.</returns>
    public async Task ToggleReactionAsync(int announcementId, int userId, string emoji)
    {
        var existingEmoji = await this.announcementRepository.GetUserReactionAsync(announcementId, userId);

        if (existingEmoji == emoji)
        {
            await this.announcementRepository.RemoveReactionAsync(announcementId, userId);
        }
        else
        {
            await this.AddOrUpdateReactAsync(announcementId, userId, emoji);
        }
    }

    /// <summary>
    /// Marks the specified announcement as read for the given user if it has not already been marked as read.
    /// </summary>
    /// <param name="announcementId">The unique identifier of the announcement to mark as read.</param>
    /// <param name="userId">The unique identifier of the user for whom the announcement is being marked as read.</param>
    /// <param name="isAlreadyRead">A value indicating whether the announcement has already been marked as read for the user. If <see
    /// langword="true"/>, the method does not perform any action.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the announcement
    /// was marked as read; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> MarkAsReadIfNeededAsync(int announcementId, int userId, bool isAlreadyRead)
    {
        if (isAlreadyRead)
        {
            return false;
        }

        await this.MarkAsReadAsync(announcementId, userId);
        return true;
    }

    /// <summary>
    /// Asynchronously retrieves a list of users who have not read the specified announcement within the given event.
    /// </summary>
    /// <param name="announcementId">The unique identifier of the announcement for which to check read status.</param>
    /// <param name="eventId">The unique identifier of the event containing the participants to evaluate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of users who are participants
    /// in the event but have not read the specified announcement. The list will be empty if all participants have read
    /// the announcement.</returns>
    public async Task<List<User>> GetNonReadersAsync(int announcementId, int eventId)
    {
        var readers = await this.announcementRepository.GetReadReceiptsAsync(announcementId);
        var participants = await this.announcementRepository.GetAllParticipantsAsync(eventId);

        var readerIds = readers
            .Select(r => r.User.UserId)
            .ToHashSet();

        var nonReaders = participants
            .Where(p => !readerIds.Contains(p.UserId))
            .ToList();

        return nonReaders;
    }

    /// <summary>
    /// Associates reaction data with their corresponding announcements by updating each announcement's reaction list.
    /// </summary>
    /// <remarks>Only announcements with matching reactions in the provided list will have their Reactions
    /// property updated. Announcements without corresponding reactions remain unchanged.</remarks>
    /// <param name="announcements">The list of announcements to which reactions will be attached. Each announcement in this list may have its
    /// Reactions property updated if matching reactions are found.</param>
    /// <param name="reactions">A list of tuples containing an announcement identifier and its associated reaction. Each tuple specifies which
    /// announcement the reaction belongs to.</param>
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

    /// <summary>
    /// Validates that the specified user is the administrator of the given event. Throws an exception if the event does
    /// not exist or if the user is not authorized as the event administrator.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to check for administrative access.</param>
    /// <param name="userId">The unique identifier of the user whose administrative rights are being validated.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the event with the specified eventId does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the administrator of the specified event.</exception>
    private async Task EnsureAdminAsync(int eventId, int userId)
    {
        var selectedEvent = await this.eventRepository.GetByIdAsync(eventId);
        if (selectedEvent is null)
        {
            throw new ArgumentException($"Event with ID {eventId} does not exist.");
        }

        if (selectedEvent.Admin?.UserId != userId)
        {
            throw new UnauthorizedAccessException("Only the EventAdmin can perform this action.");
        }
    }
}
