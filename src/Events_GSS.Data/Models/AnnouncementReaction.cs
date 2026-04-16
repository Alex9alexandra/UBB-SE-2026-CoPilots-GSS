// <copyright file="AnnouncementReaction.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models;

/// <summary>
/// Represents a reaction from a user on a specific announcement.
/// </summary>
public class AnnouncementReaction
{
    /// <summary>
    /// Gets or sets the id of the reaction.
    /// </summary>
    public int Id { get; set; } = 0;

    /// <summary>
    /// Gets or sets the emoji of the reaction.
    /// </summary>
    required public string Emoji { get; set; }

    /// <summary>
    /// Gets or sets the id of the announcement associated with the reaction.
    /// </summary>
    public int AnnouncementId { get; set; }

    /// <summary>
    /// Gets or sets the author of the reaction.
    /// </summary>
    required public User Author { get; set; }
}
