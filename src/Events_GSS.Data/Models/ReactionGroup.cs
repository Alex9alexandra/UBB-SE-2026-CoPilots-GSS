namespace Events_GSS.Data.Models;

/// <summary>
/// Represents a group of identical reactions on a message.
/// Used for display: "👍 3"
/// </summary>
public class ReactionGroup
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool CurrentUserReacted { get; set; }
}
