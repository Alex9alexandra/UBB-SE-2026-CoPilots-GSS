using System;
using System.Collections.Generic;
using System.Text;

namespace Events_GSS.Data.Models
{
    public class Announcement
    {
        //private int _id;
        //private string _message;
        //private DateTime _date;
        //private bool _isPinned;
        //private bool _isEdited; no need for these fields as they are already implemented as properties with getters and setters
        //private Event _event;
        //private User _author;

        public Announcement( int id, string message, DateTime date )
        {
            Id = id;
            Message = message;
            Date = date;
            Reactions = new List<AnnouncementReaction>();

        }

        public int Id { get; set; }
        public string Message { get; set; } 
        public DateTime Date { get; set; }

        public bool IsPinned { get; set; }

        public bool IsEdited { get; set; } 

        public bool IsRead { get; set; }

        public bool IsExpanded { get; set; } 

        public Event Event { get; set; }

        public User? Author { get; set; }
        public List<AnnouncementReaction> Reactions { get; set; }

    }
}
