// <copyright file="AnnouncementItemViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

using Events_GSS.Data.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
/// ViewModel representing a single announcement item with user-specific state
/// such as read status, reactions, and admin permissions.
/// </summary>
public partial class AnnouncementItemViewModel : ObservableObject
{
    public Announcement Model { get; }

    private readonly int _currentUserId;
    private readonly bool _isCurrentUserAdmin;
    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isRead;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnnouncementItemViewModel"/> class.
    /// </summary>
    /// <param name="model">The announcement model.</param>
    /// <param name="currentUserId">The ID of the current user.</param>
    /// <param name="isAdmin">Whether the current user is an admin.</param>
    public AnnouncementItemViewModel(Announcement model, int currentUserId, bool isAdmin)
    {
        this.Model = model;
        this._currentUserId = currentUserId;
        this._isCurrentUserAdmin = isAdmin;
        this._isRead = model.IsRead;
        this._isExpanded = false;
    }

    public int Id => this.Model.Id;

    public string Message => this.Model.Message;

    [ExcludeFromCodeCoverage]
    public DateTime Date => this.Model.Date;

    [ExcludeFromCodeCoverage]
    public bool IsPinned => this.Model.IsPinned;

    [ExcludeFromCodeCoverage]
    public bool IsEdited => this.Model.IsEdited;

    [ExcludeFromCodeCoverage]
    public User? Author => this.Model.Author;

    /// <summary>
    /// Gets first line of the message, used as the collapsed preview (REQ-ANN-02).
    /// </summary>
    public string PreviewText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(this.Message))
            {
                return string.Empty;
            }

            var firstLine = this.Message.Split('\n', 2)[0];
            return firstLine.Length > 120 ? firstLine[..120] + "…" : firstLine;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the message has more content beyond the first line.
    /// </summary>
    public bool HasFullContent => this.Message.Contains('\n') || this.Message.Length > 120;

    /// <summary>
    /// Gets reactions from all users on a specific announcement.
    /// </summary>
    public List<ReactionGroup> ReactionGroups =>
        this.Model.Reactions
            .GroupBy(reaction => reaction.Emoji)
            .Select(group => new ReactionGroup
            {
                Emoji = group.Key,
                Count = group.Count(),
                CurrentUserReacted = group.Any(reaction => reaction.Author.UserId == this._currentUserId),
            })
            .ToList();

    public bool HasReactions => this.Model.Reactions.Count > 0;

    public string? CurrentUserEmoji =>
        this.Model.Reactions
            .FirstOrDefault(reaction => reaction.Author.UserId == this._currentUserId)?
            .Emoji;

    public bool IsUnread => !this.IsRead;

    [ExcludeFromCodeCoverage]
    public bool IsCurrentUserAdmin => this._isCurrentUserAdmin;
}
