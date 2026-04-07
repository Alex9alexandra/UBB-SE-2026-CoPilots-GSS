using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using CommunityToolkit.Mvvm.ComponentModel;

using Events_GSS.Data.Models;

namespace Events_GSS.ViewModels;

/// <summary>
/// Represents a segment of a message — either plain text or a @mention.
/// Used by the view to render mentions with distinct styling.
/// </summary>
public class MessageSegment
{
    public string Text { get; set; } = string.Empty;
    public bool IsMention { get; set; }
}

public partial class DiscussionMessageItemViewModel : ObservableObject
{
    public DiscussionMessage Model { get; }
    private readonly int _currentUserId;
    private readonly bool _isCurrentUserAdmin;

    public DiscussionMessageItemViewModel(
        DiscussionMessage model,
        int currentUserId,
        bool isCurrentUserAdmin)
    {
        Model = model;
        _currentUserId = currentUserId;
        _isCurrentUserAdmin = isCurrentUserAdmin;
    }

    // Expose model properties
    public int Id => Model.Id;
    public string? Message => Model.Message;
    public string? MediaPath => Model.MediaPath;
    public DateTime Date => Model.DateCreated;
    public bool IsEdited => Model.IsEdited;
    public bool CanDelete => Model.CanDelete;
    public User? Author => Model.Author;
    public DiscussionMessage? ReplyTo => Model.ReplyTo;

    // Reactions grouped by emoji
    public List<ReactionGroup> ReactionGroups =>
        Model.Reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new ReactionGroup
            {
                Emoji = g.Key,
                Count = g.Count(),
                CurrentUserReacted = g.Any(r => r.Author.UserId == _currentUserId)
            })
            .ToList();

    public bool HasReactions => Model.Reactions.Count > 0;

    public string? CurrentUserEmoji =>
        Model.Reactions
            .FirstOrDefault(r => r.Author.UserId == _currentUserId)?
            .Emoji;

    /// <summary>
    /// Mute button visible only when: current user is admin AND message is from a different user.
    /// </summary>
    public bool ShowMuteButton =>
        _isCurrentUserAdmin && Model.Author?.UserId != _currentUserId;

    /// <summary>
    /// Parsed message segments for rendering @mentions highlighted.
    /// </summary>
    public List<MessageSegment> MessageSegments => ParseMessageIntoSegments();

    public bool HasMessageText => !string.IsNullOrWhiteSpace(Message);

    // UI-only state
    [ObservableProperty]
    private bool _isOriginalDeleted;

    private List<MessageSegment> ParseMessageIntoSegments()
    {
        var segments = new List<MessageSegment>();

        if (string.IsNullOrWhiteSpace(Message))
            return segments;

        // Match @Name or @First Last
        var pattern = @"(@\w+(?:\s+\w+)?)";
        var parts = Regex.Split(Message, pattern);

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            segments.Add(new MessageSegment
            {
                Text = part,
                IsMention = part.StartsWith("@")
            });
        }

        return segments;
    }
}
