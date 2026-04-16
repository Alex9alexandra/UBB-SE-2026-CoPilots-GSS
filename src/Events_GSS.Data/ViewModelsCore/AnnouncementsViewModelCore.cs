using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.ViewModelsCore;

public sealed class AnnouncementsViewModelCore
{
    private const double PercentageMultiplier = 100.0;

    public static string GetReadReceiptSummary(int numberOfReaders, int totalParticipants)
    {
        if (totalParticipants == 0)
        {
            return "No participants";
        }

        var percentage = (int)Math.Round(PercentageMultiplier * numberOfReaders / totalParticipants);
        return $"{numberOfReaders} / {totalParticipants} read ({percentage}%)";
    }

    public static int CalculateUnreadCount(IEnumerable<Announcement> announcements)
    {
        return announcements.Count(announcement => !announcement.IsRead);
    }

    public static bool CanSubmit(string message)
    {
        return !string.IsNullOrWhiteSpace(message);
    }

    /// <summary>
    /// Represents the mode of submission for an announcement.
    /// </summary>
    public enum SubmitMode
    {
        /// <summary>
        /// Create a new announcement.
        /// </summary>
        Create,

        /// <summary>
        /// Edit an existing announcement.
        /// </summary>
        Edit
    }

    public static SubmitMode GetSubmitMode(bool isEditing)
    {
        return isEditing ? SubmitMode.Edit : SubmitMode.Create;
    }

    public static string NormalizeMessage(string message)
    {
        return message.Trim();
    }

    public static string GetEditableMessage(Announcement announcement)
    {
        return announcement.Message;
    }

    public static bool Toggle(bool value)
    {
        return !value;
    }

    public static (List<AnnouncementReadReceipt> readers, int readCount) ProcessReadReceipts(
    IEnumerable<AnnouncementReadReceipt> readers)
    {
        var list = readers.ToList();
        return (list, list.Count);
    }
}
