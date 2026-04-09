using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Events_GSS.Data.Models;

namespace Events_GSS.ViewModelsCore;

public class MessageSegment
{
    public string Text { get; set; } = string.Empty;
    public bool IsMention { get; set; }
}

public static class DiscussionMessageItemViewModelCore
{
    public static bool ShowMuteButton(
        bool isCurrentUserAdmin,
        int? messageAuthorId,
        int currentUserId) =>
        isCurrentUserAdmin && messageAuthorId != currentUserId;

    public static bool HasReactions(ICollection<DiscussionReaction> reactions) =>
        reactions.Count > 0;

    public static bool HasMessageText(string? message) =>
        !string.IsNullOrWhiteSpace(message);

    public static string? CurrentUserEmoji(
        IEnumerable<DiscussionReaction> reactions,
        int currentUserId) =>
        reactions
            .FirstOrDefault(r => r.Author.UserId == currentUserId)?
            .Emoji;

    public static List<ReactionGroup> BuildReactionGroups(
        IEnumerable<DiscussionReaction> reactions,
        int currentUserId) =>
        reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new ReactionGroup
            {
                Emoji = g.Key,
                Count = g.Count(),
                CurrentUserReacted = g.Any(r => r.Author.UserId == currentUserId)
            })
            .ToList();

    public static List<MessageSegment> ParseMessageIntoSegments(string? message)
    {
        var segments = new List<MessageSegment>();

        if (string.IsNullOrWhiteSpace(message))
        {
            return segments;
        }
        const string pattern = @"(@\w+(?:\s+\w+)?)";
        var parts = Regex.Split(message, pattern);

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }
            segments.Add(new MessageSegment
            {
                Text = part,
                IsMention = part.StartsWith("@")
            });
        }

        return segments;
    }
}