using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Keyhaven.Web.Helpers;

public static class DisplayExtensions
{
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    public static string Label(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
    }

    public static string Money(this decimal value) => value.ToString("C0", Uk);

    public static string Money2(this decimal value) => value.ToString("C2", Uk);

    public static string Percent(this decimal value) => $"{value:0.#}%";

    public static string ShortDate(this DateOnly value) => value.ToString("d MMM yyyy", Uk);

    public static string ShortDate(this DateTime value) => value.ToString("d MMM yyyy", Uk);

    public static string DateTimeLabel(this DateTime value) => value.ToString("d MMM yyyy, h:mmtt", Uk).ToLowerInvariant().Replace("am", "am").Replace("pm", "pm");

    public static string TimeLabel(this DateTime value) => value.ToString("h:mmtt", Uk).ToLowerInvariant();

    public static string MonthLabel(this DateOnly value) => value.ToString("MMMM yyyy", Uk);

    public static string Relative(this DateTime value)
    {
        var delta = DateTime.Now - value;
        if (delta.TotalMinutes < 1) return "just now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} min ago";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} hr ago";
        if (delta.TotalDays < 7) return $"{(int)delta.TotalDays} d ago";
        return value.ToString("d MMM", Uk);
    }

    /// <summary>Semantic colour tone used by the badge and status components.</summary>
    public static string Tone(this Enum value)
    {
        var name = value.ToString();
        return name switch
        {
            "Active" or "Paid" or "Matched" or "Valid" or "Completed" or "Passed" or "Confirmed"
                or "AutoReplied" or "Replied" or "Approved" or "Let" or "Available" => "positive",
            "Due" or "ExpiringSoon" or "Pending" or "Requested" or "DraftReady" or "Quoted" or "UnderOffer"
                or "Onboarding" or "Invited" or "AwaitingApproval" or "Ending" or "Prospect"
                or "Upcoming" => "warning",
            "Expired" or "Overdue" or "Emergency" or "Failed" or "NeedsHuman" or "NoShow" or "Missing"
                or "Rejected" or "Arrears" => "danger",
            "New" or "Triaged" or "InProgress" or "Drafted" or "Unreconciled" or "Shortfall" or "Maintenance"
                or "InProgress" or "Booked" => "info",
            _ => "muted"
        };
    }
}
