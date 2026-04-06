using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Events_GSS.Data.Messaging;

public class ReputationMessage : ValueChangedMessage<ReputationAction>
{
    public int UserId { get; }
    public int? EventId { get; }

    public ReputationMessage(int userId, ReputationAction action, int? eventId = null)
        : base(action)
    {
        UserId = userId;
        EventId = eventId;
    }
}
