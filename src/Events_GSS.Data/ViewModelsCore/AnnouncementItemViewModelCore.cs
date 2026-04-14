// <copyright file="AnnouncementItemViewModelCore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.ViewModelsCore;

using Events_GSS.Data.Models;

/// <summary>
/// Represents the core logic for an announcement item view model, providing properties and methods.
/// </summary>
public sealed class AnnouncementItemViewModelCore
{
    private readonly Announcement announcementModel;
    private readonly int currentUserId;

    public AnnouncementItemViewModelCore(Announcement model, int currentUserId)
    {
        announcementModel = model;
        this.currentUserId = currentUserId;
    }

    public string PreviewText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(announcementModel.Message))
            {
                return string.Empty;
            }

            var firstLine = announcementModel.Message.Split('\n', 2)[0];
            return firstLine.Length > 120 ? firstLine[..120] + "…" : firstLine;
        }
    }

    public bool HasFullContent =>
        announcementModel.Message.Contains('\n') || announcementModel.Message.Length > 120;

    public List<ReactionGroup> ReactionGroups =>
        announcementModel.Reactions
            .GroupBy(r => r.Emoji)
            .Select(group => new ReactionGroup
            {
                Emoji = group.Key,
                Count = group.Count(),
                CurrentUserReacted =
                    group.Any(r => r.Author.UserId == currentUserId),
            })
            .ToList();

    public string? CurrentUserEmoji =>
        announcementModel.Reactions
            .FirstOrDefault(r => r.Author.UserId == currentUserId)?
            .Emoji;
}
