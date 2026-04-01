using System;
using System.Collections.Generic;
using System.Text;

namespace Events_GSS.Data.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public User User { get; set; }
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Notification(int id, User user, string title, string description)
        {
            Id = id;
            User = user;
            Title = title;
            Description = description;
        }

        public Notification() { }
    }
}
