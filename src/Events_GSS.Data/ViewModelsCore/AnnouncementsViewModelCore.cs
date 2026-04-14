using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.ViewModelsCore;

public sealed class AnnouncementsViewModelCore
{
    private const double PercentageMultiplier = 100.0;

    public static string GetReadReceiptSummary(int read, int total)
    {
        if (total == 0)
            return "No participants";

        var percentage = (int)Math.Round(PercentageMultiplier * read / total);
        return $"{read} / {total} read ({percentage}%)";
    }

    public static int CalculateUnreadCount(IEnumerable<Announcement> announcements)
    {
        return announcements.Count(a => !a.IsRead);
    }

    public static bool CanSubmit(string message)
    {
        return !string.IsNullOrWhiteSpace(message);
    }

    public enum SubmitMode
    {
        Create,
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
}
