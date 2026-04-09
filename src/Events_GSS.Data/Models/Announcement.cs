// <copyright file="Announcement.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents an announcement within an event.
    /// </summary>
    public class Announcement(int id, string message, DateTime date)
    {
        public int Id { get; set; } = id;

        public string Message { get; set; } = message;

        public DateTime Date { get; set; } = date;

        public bool IsPinned { get; set; }

        public bool IsEdited { get; set; }

        public bool IsRead { get; set; }

        public bool IsExpanded { get; set; }

        public Event Event { get; set; }

        public User? Author { get; set; }

        public List<AnnouncementReaction> Reactions { get; set; } = new List<AnnouncementReaction>();
    }
}