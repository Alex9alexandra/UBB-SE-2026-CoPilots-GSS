// <copyright file="Notification.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a notification in the system.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        /// <param name="user">The user associated with the notification.</param>
        /// <param name="title">The title of the notification.</param>
        /// <param name="description">The description of the notification.</param>
        public Notification(User user, string title, string description)
        {
            this.User = user;
            this.Title = title;
            this.Description = description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        public Notification()
        {
        }

        /// <summary>
        /// Gets or sets the unique identifier for the notification.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user associated with the notification.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the title of the notification.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the notification.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation date and time of the notification.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
