// <copyright file="ReactionCounter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models;

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Represents a counter for a specific reaction (emoji) on a message.
/// </summary>
public class ReactionCounterDefault
{
    /// <summary>
    /// Represents the default value for the total, typically used when no specific total is provided.
    /// </summary>
    public const int DefaultTotal = 0;
}

/// <summary>
/// Represents a reaction and its associated count, typically used to track the number of times a specific emoji
/// reaction has been applied.
/// </summary>
public class ReactionCounter
{
    /// <summary>
    /// Gets or sets the count of reactions.
    /// </summary>
    public int Count { get; set; } = ReactionCounterDefault.DefaultTotal;

    /// <summary>
    /// Gets or sets the emoji representing the reaction.
    /// </summary>
    public string Emoji { get; set; } = string.Empty;
}
