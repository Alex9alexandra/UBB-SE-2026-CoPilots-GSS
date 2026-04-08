// <copyright file="AnnouncementReaction.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models;

/// <summary>
/// Represents a reaction from a user on a specific announcement.
/// </summary>
public class AnnouncementReaction
{
    public int Id { get; set; } = 0;

    required public string Emoji { get; set; }

    public int AnnouncementId { get; set; }

    required public User Author { get; set; }
}
