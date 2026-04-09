using System;
using System.Text.RegularExpressions;

namespace Events_GSS.ViewModelsCore;

public static class DiscussionViewModelCore
{
    public static bool CanSend(
        string? newMessage,
        string? mediaPath,
        bool isLoading,
        bool isMuted) =>
        (!string.IsNullOrWhiteSpace(newMessage) || !string.IsNullOrWhiteSpace(mediaPath))
        && !isLoading
        && !isMuted;

    public static string InsertMention(string currentMessage, string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return currentMessage;
        }
        var mention = $"@{userName} ";

        if (!string.IsNullOrEmpty(currentMessage) && !currentMessage.EndsWith(" "))
        {
            return currentMessage + " " + mention;
        }
        return currentMessage + mention;
    }

    public static DateTime? CalculateMuteExpiry(
        string selection,
        double customHours,
        double customMinutes,
        DateTime now) =>
        selection switch
        {
            "1 hour" => now.AddHours(1),
            "24 hours" => now.AddDays(1),
            "Custom" => now.AddHours(customHours).AddMinutes(customMinutes),
            "Permanent" => (DateTime?)null,
            _ => now.AddMinutes(30) // default fallback
        };

    public static int? NormaliseSlowModeSeconds(double? seconds) =>
        seconds.HasValue ? (int)Math.Round(seconds.Value) : null;

    public static int? TryParseSlowModeSeconds(string exceptionMessage)
    {
        var match = Regex.Match(exceptionMessage, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int secs))
        {
            return secs;
        }
        return null;
    }

    public static bool IsMuteException(string exceptionMessage) =>
        exceptionMessage.Contains("muted", StringComparison.OrdinalIgnoreCase);

    public static bool IsSlowModeException(string exceptionMessage) =>
        exceptionMessage.Contains("Slow mode", StringComparison.OrdinalIgnoreCase);
}
