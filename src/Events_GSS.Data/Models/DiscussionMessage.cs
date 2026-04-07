using System;

namespace Events_GSS.Data.Models;

public class DiscussionMessage
{
    public DiscussionMessage(int id, string? message, DateTime date)
    {
        Id = id;
        Message = message;
        DateCreated = date;
        Reactions = new List<DiscussionReaction>();
    }

    public int Id { get; set; }
    public string? Message { get; set; }
    public string? MediaPath { get; set; }
    public DateTime DateCreated { get; set; }
    public bool IsEdited { get; set; }

    // Navigation
    public Event? AssociatedEvent { get; set; }
    public User? Author { get; set; }
    public DiscussionMessage? ReplyTo { get; set; }

    public List<DiscussionReaction> Reactions { get; set; }

    // Non-persisted, computed at the service
    public bool CanDelete { get; set; }
}
