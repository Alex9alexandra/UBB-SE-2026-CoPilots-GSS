// <copyright file="ReactionGroup.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models;

/// <summary>
/// Represents a group of identical reactions on a message.
/// Used for display: "👍 3".
/// </summary>
public class ReactionGroup
{
    /// <summary>
    /// Gets or sets the emoji representing the reaction.
    /// </summary>
    public string Emoji { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of reactions.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user has reacted.
    /// </summary>
    public bool CurrentUserReacted { get; set; }
}