using System;
using System.Collections.Generic;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using Events_GSS.Data.Models;

namespace Events_GSS.ViewModels;

public partial class AnnouncementItemViewModel : ObservableObject
{
    public Announcement Model { get; }
    private readonly int _currentUserId;
    private readonly bool _isCurrentUserAdmin;

    public AnnouncementItemViewModel(Announcement model, int currentUserId, bool isAdmin)
    {
        Model = model;
        _currentUserId = currentUserId;
        _isCurrentUserAdmin = isAdmin;
        _isRead = model.IsRead;
        _isExpanded = false;
    }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isRead;

    public int Id => Model.Id;
    public string Message => Model.Message;
    public DateTime Date => Model.Date;
    public bool IsPinned => Model.IsPinned;
    public bool IsEdited => Model.IsEdited;
    public User? Author => Model.Author;

    /// <summary>
    /// First line of the message, used as the collapsed preview (REQ-ANN-02).
    /// </summary>
    public string PreviewText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Message)) return string.Empty;
            var firstLine = Message.Split('\n', 2)[0];
            return firstLine.Length > 120 ? firstLine[..120] + "…" : firstLine;
        }
    }

    /// <summary>
    /// Whether the message has more content beyond the first line.
    /// </summary>
    public bool HasFullContent => Message.Contains('\n') || Message.Length > 120;

    // Reactions
    public List<ReactionGroup> ReactionGroups =>
        Model.Reactions
            .GroupBy(reaction => reaction.Emoji)
            .Select(group => new ReactionGroup
            {
                Emoji = group.Key,
                Count = group.Count(),
                CurrentUserReacted = group.Any(reaction => reaction.Author.UserId == _currentUserId)
            })
            .ToList();

    public bool HasReactions => Model.Reactions.Count > 0;

    public string? CurrentUserEmoji =>
        Model.Reactions
            .FirstOrDefault(reaction => reaction.Author.UserId == _currentUserId)?
            .Emoji;

    public bool IsUnread => !IsRead;

    public bool IsCurrentUserAdmin => _isCurrentUserAdmin;
}
