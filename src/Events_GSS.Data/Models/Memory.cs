using System;

namespace Events_GSS.Data.Models
{
    public class Memory
    {
        public int MemoryId { get; set; }
        public string? PhotoPath { get; set; }
        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public Event Event { get; set; } = null!;
        public User Author { get; set; } = null!;
        public int LikesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }


        public Memory(string? photoPath, string? text, DateTime createdAt)
        {
            PhotoPath = photoPath;
            Text = text;
            CreatedAt = createdAt;
            LikesCount = 0;
            IsLikedByCurrentUser = false;
        }
        public Memory() { }

    }
}


