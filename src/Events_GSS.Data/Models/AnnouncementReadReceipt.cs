// <copyright file="AnnouncementReadReceipt.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models;

/// <summary>
/// Refers to the users that have read a specific announcement.
/// </summary>
public class AnnouncementReadReceipt
{
    public int AnnouncementId { get; set; }

    public User User { get; set; }

    public DateTime ReadAt { get; set; }
}
