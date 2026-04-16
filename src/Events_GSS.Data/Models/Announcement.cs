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
        /// <summary>
        /// Gets or sets the id of the announcement.
        /// </summary>
        public int Id { get; set; } = id;

        /// <summary>
        /// Gets or sets the message of the announcement.
        /// </summary>
        public string Message { get; set; } = message;

        /// <summary>
        /// Gets or sets the date of the announcement.
        /// </summary>
        public DateTime Date { get; set; } = date;

        /// <summary>
        /// Gets or sets a value indicating whether the announcement has been pinned.
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the announcement has been edited.
        /// </summary>
        public bool IsEdited { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the announcement has been read.
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the announcement is expanded.
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Gets or sets the event associated with the announcement.
        /// </summary>
        public Event? Event { get; set; }

        /// <summary>
        /// Gets or sets the author of the announcement.
        /// </summary>
        public User? Author { get; set; }

        /// <summary>
        /// Gets or sets the reactions associated with the announcement.
        /// </summary>
        public List<AnnouncementReaction> Reactions { get; set; } = new List<AnnouncementReaction>();
    }
}