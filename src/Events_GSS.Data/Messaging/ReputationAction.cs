namespace Events_GSS.Data.Messaging;

public enum ReputationAction
{
    EventCreated,
    EventCancelled,
    EventAttended,
    DiscussionMessagePosted,
    DiscussionMessageRemovedByAdmin,
    MemoryAddedWithPhoto,
    MemoryAddedTextOnly,
    QuestSubmitted,
    QuestApproved,
    QuestDenied,
}
