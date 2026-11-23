using Keyhaven.Web.Models;

namespace Keyhaven.Web.Services;

public record TriageResult(Urgency Urgency, MaintenanceCategory Category, string Summary, string Reason, string SuggestedTrade);

public static class MaintenanceTriageService
{
    private static readonly (string Phrase, Urgency Urgency, string Reason)[] Signals =
    [
        ("water coming through", Urgency.Emergency, "Active water ingress with risk to structure and electrics."),
        ("water is coming through", Urgency.Emergency, "Active water ingress with risk to structure and electrics."),
        ("coming through the ceiling", Urgency.Emergency, "Active water ingress with risk to structure and electrics."),
        ("burst pipe", Urgency.Emergency, "Burst pipe with active water escape."),
        ("flood", Urgency.Emergency, "Active flooding reported."),
        ("gas leak", Urgency.Emergency, "Suspected gas leak — immediate attendance required."),
        ("smell of gas", Urgency.Emergency, "Suspected gas leak — immediate attendance required."),
        ("smoke", Urgency.Emergency, "Smoke reported — fire risk."),
        ("sparks", Urgency.Emergency, "Electrical sparking — fire and shock risk."),
        ("exposed wiring", Urgency.Emergency, "Exposed wiring — immediate safety risk."),
        ("ceiling collapse", Urgency.Emergency, "Structural collapse — immediate safety risk."),
        ("sewage", Urgency.Emergency, "Sewage escape — health hazard."),
        ("no heating", Urgency.Urgent, "Loss of heating; urgent during cold weather."),
        ("no hot water", Urgency.Urgent, "Loss of hot water for the household."),
        ("leak", Urgency.Urgent, "Active leak that may cause further damage if left."),
        ("leaking", Urgency.Urgent, "Active leak that may cause further damage if left."),
        ("damp", Urgency.Urgent, "Damp or mould with a possible health and fabric impact."),
        ("mould", Urgency.Urgent, "Damp or mould with a possible health and fabric impact."),
        ("not working", Urgency.Urgent, "Essential fixture or appliance not functioning."),
        ("broken", Urgency.Urgent, "Broken fixture or fitting affecting normal use."),
        ("fault", Urgency.Urgent, "Reported fault needs diagnosis."),
        ("cannot lock", Urgency.Urgent, "The property cannot be secured."),
        ("can't lock", Urgency.Urgent, "The property cannot be secured."),
        ("locked out", Urgency.Urgent, "Tenant cannot gain access."),
        ("lock broken", Urgency.Urgent, "Security or access issue at the property."),
        ("condensation", Urgency.Routine, "Condensation reported; monitor ventilation and damp risk."),
        ("stiff", Urgency.Routine, "Wear and tear affecting ease of use."),
        ("drip", Urgency.Routine, "Minor drip that can be contained."),
        ("paint", Urgency.Routine, "Cosmetic or turnaround work.")
    ];

    public static TriageResult Triage(string title, string description, MaintenanceCategory category)
    {
        var text = $"{title} {description}".ToLowerInvariant();

        var matched = Signals
            .Where(s => text.Contains(s.Phrase, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Urgency)
            .ToList();

        var urgency = matched.Count > 0 ? matched[0].Urgency : Urgency.Routine;
        var reason = matched.Count > 0 ? matched[0].Reason : "No emergency indicators found in the report.";

        if (category == MaintenanceCategory.Other)
        {
            category = InferCategory(text);
        }

        var trade = category switch
        {
            MaintenanceCategory.Plumbing => "Plumbing & heating",
            MaintenanceCategory.Heating => "Heating engineer",
            MaintenanceCategory.Electrical => "Electrical",
            MaintenanceCategory.Damp => "Damp & remedial",
            MaintenanceCategory.Structural => "General builder",
            MaintenanceCategory.Locks => "Locks & security",
            MaintenanceCategory.Appliance => "Appliance engineer",
            _ => "General maintenance"
        };

        var firstSentence = description.Trim();
        var fullStop = firstSentence.IndexOf('.');
        if (fullStop > 0)
        {
            firstSentence = firstSentence[..(fullStop + 1)];
        }
        if (firstSentence.Length > 240)
        {
            firstSentence = firstSentence[..237] + "…";
        }

        var summary = $"{title.TrimEnd('.')}. Reported as a {Label(category).ToLowerInvariant()} issue. {firstSentence}";

        return new TriageResult(urgency, category, summary, reason, trade);
    }

    private static MaintenanceCategory InferCategory(string text)
    {
        if (ContainsAny(text, "boiler", "radiator", "heating", "hot water")) return MaintenanceCategory.Heating;
        if (ContainsAny(text, "leak", "tap", "pipe", "water", "toilet", "shower", "bath", "sink")) return MaintenanceCategory.Plumbing;
        if (ContainsAny(text, "electric", "socket", "light", "fan", "fuse", "trip")) return MaintenanceCategory.Electrical;
        if (ContainsAny(text, "damp", "mould", "condensation")) return MaintenanceCategory.Damp;
        if (ContainsAny(text, "roof", "wall", "ceiling", "floor", "window")) return MaintenanceCategory.Structural;
        if (ContainsAny(text, "lock", "door", "key", "handle")) return MaintenanceCategory.Locks;
        if (ContainsAny(text, "oven", "fridge", "washer", "appliance")) return MaintenanceCategory.Appliance;
        return MaintenanceCategory.Other;
    }

    private static bool ContainsAny(string text, params string[] phrases) =>
        phrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static string Label(MaintenanceCategory category) => category switch
    {
        MaintenanceCategory.Damp => "damp & mould",
        MaintenanceCategory.Locks => "locks & security",
        _ => category.ToString()
    };
}
