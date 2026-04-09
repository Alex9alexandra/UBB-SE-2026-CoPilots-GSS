// <copyright file="User.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Models
{
    using System;

    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the reputation points of the user.
        /// </summary>
        public int ReputationPoints { get; set; } = UserDefaults.DefaultReputationPoints;
    }
}