using System;
using System.Collections.Generic;
using System.Text;

namespace Events_GSS.Data.Models
{
    public class AttendedEvent
    {
        public Event Event { get; set; }
        public User User { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public Boolean IsArchived { get; set; }
        public Boolean IsFavourite {  get; set; }

        public int UnreadAnnouncementCount { get; set; }

        public AttendedEvent(Event @event, User user, DateTime enrollmentDate, bool isArchived, bool isFavourite)
        {
            Event = @event;
            User = user;
            EnrollmentDate = enrollmentDate;
            IsArchived = isArchived;
            IsFavourite = isFavourite;
        }

        public AttendedEvent() { }
    }
}
