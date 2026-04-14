using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.ViewModelsCore;

public sealed class AnnouncementItemViewModelCore
{
    private readonly Announcement _model;
    private readonly int _currentUserId;

    public AnnouncementItemViewModelCore(Announcement model, int currentUserId)
    {
        _model = model;
        _currentUserId = currentUserId;
    }

    public string PreviewText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_model.Message))
                return string.Empty;

            var firstLine = _model.Message.Split('\n', 2)[0];
            return firstLine.Length > 120 ? firstLine[..120] + "…" : firstLine;
        }
    }

    public bool HasFullContent =>
        _model.Message.Contains('\n') || _model.Message.Length > 120;

    public List<ReactionGroup> ReactionGroups =>
        _model.Reactions
            .GroupBy(r => r.Emoji)
            .Select(group => new ReactionGroup
            {
                Emoji = group.Key,
                Count = group.Count(),
                CurrentUserReacted =
                    group.Any(r => r.Author.UserId == _currentUserId),
            })
            .ToList();

    public string? CurrentUserEmoji =>
        _model.Reactions
            .FirstOrDefault(r => r.Author.UserId == _currentUserId)?
            .Emoji;
}
