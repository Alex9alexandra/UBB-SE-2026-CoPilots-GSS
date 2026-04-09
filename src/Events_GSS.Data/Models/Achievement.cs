// <copyright file="Achievement.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models;

/// <summary>
/// Represents an achievement that a user can unlock in the system.
/// </summary>
public class Achievement
{
    /// <summary>
    /// Gets or sets the unique identifier for the achievement.
    /// </summary>
    public int AchievementId { get; set; }

    /// <summary>
    /// Gets or sets the name of the achievement.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the achievement.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the achievement is unlocked.
    /// </summary>
    public bool IsUnlocked { get; set; }
}
