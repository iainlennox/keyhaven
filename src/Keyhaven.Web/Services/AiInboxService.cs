using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Services;

/// <summary>
/// Record-grounded inbox assistant. It classifies an inbound message, finds the records
/// that can answer it, and either sends a routine reply, prepares a draft for a person,
/// or escalates when a supporting record is missing. It never invents facts.
/// </summary>
public class AiInboxService(KeyhavenDbContext db)
{
    public const string AssistantName = "Keyhaven AI";

    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        var pending = await db.InboxMessages
            .Where(m => m.AutomationMode == AutomationMode.Pending)
            .OrderBy(m => m.ReceivedAt)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            await AnalyseAsync(message, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task AnalyseAsync(InboxMessage message, CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var previouslyHandled = message.HandledAt is not null;

        await LinkRecordsAsync(message, ct);

        var category = Classify(message);
        message.Category = category;

        var grounding = new List<string>();
        InboxStatus status;
        AutomationMode mode;
        int confidence;
        string draft;

        switch (category)
        {
            case InboxCategory.Enquiry:
                (draft, mode, confidence, status) = await BuildEnquiryReplyAsync(message, grounding, ct);
                break;
            case InboxCategory.Repair:
                (draft, mode, confidence, status) = await BuildRepairReplyAsync(message, grounding, ct);
                break;
            case InboxCategory.Access:
                (draft, mode, confidence, status) = await BuildAccessReplyAsync(message, grounding, ct);
                break;
            case InboxCategory.Statement:
                (draft, mode, confidence, status) = await BuildStatementReplyAsync(message, grounding, today, ct);
                break;
            case InboxCategory.Rent:
                (draft, mode, confidence, status) = await BuildRentReplyAsync(message, grounding, today, ct);
                break;
            case InboxCategory.Viewing:
                (draft, mode, confidence, status) = await BuildViewingReplyAsync(message, grounding, now, ct);
                break;
            default:
                draft = "Thank you for your email. I can't find a property, tenancy, repair or viewing record that this message relates to, so I've passed it to a member of the team to pick up.";
                mode = AutomationMode.Escalated;
                confidence = 25;
                status = InboxStatus.NeedsHuman;
                grounding.Add("No matching record could be identified from the sender, subject or body.");
                break;
        }

        message.DraftReply = draft.Trim();
        message.Grounding = string.Join("\n", grounding).Trim();
        message.Confidence = confidence;
        message.AutomationMode = mode;
        message.Status = status;

        if (mode == AutomationMode.AutoSent)
        {
            message.HandledAt = now;
            message.HandledBy = AssistantName;

            if (!previouslyHandled)
            {
                db.ContactNotes.Add(new ContactNote
                {
                    ContactType = ContactTypeFor(message.Category),
                    ContactName = message.FromName,
                    ContactEmail = message.FromEmail,
                    Channel = ContactChannel.Email,
                    Summary = $"Auto-replied to “{message.Subject}”. {FirstLine(draft)}",
                    OccurredAt = now,
                    CreatedBy = AssistantName,
                    PropertyId = message.LinkedPropertyId
                });
            }
        }
        else
        {
            message.HandledAt = null;
            message.HandledBy = null;
        }
    }

    public async Task SendReplyAsync(InboxMessage message, string reply, string handledBy, CancellationToken ct = default)
    {
        var now = DateTime.Now;
        message.DraftReply = reply.Trim();
        message.Status = InboxStatus.Replied;
        message.HandledAt = now;
        message.HandledBy = handledBy;

        if (message.AutomationMode == AutomationMode.Pending)
        {
            message.AutomationMode = AutomationMode.Drafted;
        }

        db.ContactNotes.Add(new ContactNote
        {
            ContactType = ContactTypeFor(message.Category),
            ContactName = message.FromName,
            ContactEmail = message.FromEmail,
            Channel = ContactChannel.Email,
            Summary = $"Replied to “{message.Subject}”.",
            OccurredAt = now,
            CreatedBy = handledBy,
            PropertyId = message.LinkedPropertyId
        });

        await db.SaveChangesAsync(ct);
    }

    private async Task LinkRecordsAsync(InboxMessage message, CancellationToken ct)
    {
        var haystack = $"{message.Subject} {message.Body}".ToLowerInvariant();
        var properties = await db.Properties.AsNoTracking().ToListAsync(ct);

        if (message.LinkedPropertyId is null)
        {
            var match = properties.FirstOrDefault(p =>
                haystack.Contains(p.Postcode.ToLowerInvariant()) ||
                haystack.Contains(p.AddressLine1.ToLowerInvariant()));
            if (match is not null)
            {
                message.LinkedPropertyId = match.Id;
            }
        }

        if (message.LinkedPropertyId is not null)
        {
            var property = properties.First(p => p.Id == message.LinkedPropertyId);

            if (message.LinkedOwnerId is null)
            {
                message.LinkedOwnerId = property.OwnerId;
            }

            if (message.LinkedTenancyId is null)
            {
                message.LinkedTenancyId = await db.Tenancies
                    .Where(t => t.PropertyId == property.Id && t.Status == TenancyStatus.Active)
                    .OrderByDescending(t => t.StartDate)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefaultAsync(ct);
            }

            if (message.LinkedTicketId is null)
            {
                message.LinkedTicketId = await db.MaintenanceTickets
                    .Where(t => t.PropertyId == property.Id && t.Status != MaintenanceStatus.Closed)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefaultAsync(ct);
            }
        }
        else if (message.LinkedTenancyId is not null)
        {
            message.LinkedPropertyId = await db.Tenancies
                .Where(t => t.Id == message.LinkedTenancyId)
                .Select(t => t.PropertyId)
                .FirstOrDefaultAsync(ct);
        }
    }

    private static InboxCategory Classify(InboxMessage message)
    {
        var text = $"{message.Subject} {message.Body}".ToLowerInvariant();

        if (ContainsAny(text, "access", "keys", "key register", "let me in", "attend the property", "collect the keys"))
            return InboxCategory.Access;
        if (ContainsAny(text, "statement", "quarterly", "end of quarter", "owner statement"))
            return InboxCategory.Statement;
        if (ContainsAny(text, "rent balance", "rent arrears", "what do i owe", "payment plan", "behind with the rent", "rent account"))
            return InboxCategory.Rent;
        if (ContainsAny(text, "repair", "leak", "dripping", "boiler", "tap", "damp", "broken", "not working", "mould", "radiator", "extractor", "lock"))
            return InboxCategory.Repair;
        if (ContainsAny(text, "viewing", "reschedule", "appointment time", "move our viewing"))
            return InboxCategory.Viewing;
        if (ContainsAny(text, "available", "availability", "rent", "interested in the flat", "interested in the property", "enquiry"))
            return InboxCategory.Enquiry;

        return InboxCategory.Other;
    }

    private async Task<(string Draft, AutomationMode Mode, int Confidence, InboxStatus Status)> BuildEnquiryReplyAsync(
        InboxMessage message, List<string> grounding, CancellationToken ct)
    {
        var property = message.LinkedPropertyId is null
            ? null
            : await db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == message.LinkedPropertyId, ct);

        if (property is null)
        {
            grounding.Add("No property record is linked to this enquiry.");
            return (
                "Thank you for your email. I can't match it to a property record, so a member of the team will pick it up and come back to you.",
                AutomationMode.Escalated, 30, InboxStatus.NeedsHuman);
        }

        var subject = $"{property.AddressLine1}, {property.City} {property.Postcode}";
        grounding.Add($"Property record: {property.Reference} · {subject} · status {StatusLabel(property.Status)} · £{property.MonthlyRent:N0} pcm");
        if (property.ListingPublished)
        {
            grounding.Add($"Listing published{(property.ListedOn is null ? "" : $" on {property.ListedOn:d MMMM yyyy}")} to: {property.Channels}");
        }
        grounding.Add($"Enquiry received from {message.FromName} <{message.FromEmail}>.");

        if (property.Status == PropertyStatus.Available)
        {
            var draft = $"""
                Dear {FirstName(message.FromName)},

                Thank you for your enquiry about {subject}.

                The property record lists it as available to let at £{property.MonthlyRent:N0} per calendar month.

                If you would like to arrange a viewing, please reply with a couple of times that suit you and we'll check the diary.

                Kind regards,
                Keyhaven Lettings
                """;
            return (draft, AutomationMode.AutoSent, 96, InboxStatus.AutoReplied);
        }

        var availability = property.Status switch
        {
            PropertyStatus.Let => "currently recorded as let",
            PropertyStatus.UnderOffer => "currently recorded as under offer",
            PropertyStatus.Maintenance => "currently recorded as undergoing maintenance",
            _ => $"currently recorded as {StatusLabel(property.Status).ToLowerInvariant()}"
        };

        var heldDraft = $"""
            Dear {FirstName(message.FromName)},

            Thank you for your enquiry about {subject}.

            The property record shows it is {availability}, so it isn't available at the moment. If you'd like, I can keep your details on file for similar properties.

            Kind regards,
            Keyhaven Lettings
            """;
        return (heldDraft, AutomationMode.Drafted, 91, InboxStatus.DraftReady);
    }

    private async Task<(string Draft, AutomationMode Mode, int Confidence, InboxStatus Status)> BuildRepairReplyAsync(
        InboxMessage message, List<string> grounding, CancellationToken ct)
    {
        var ticket = await LoadTicketAsync(message, ct);
        if (ticket is null)
        {
            grounding.Add("No open repair record was found for this property and sender.");
            return (
                "Thank you for your email. I can't find a repair record that matches this message, so I've passed it to the team to check before replying.",
                AutomationMode.Escalated, 32, InboxStatus.NeedsHuman);
        }

        grounding.Add($"Repair record: {ticket.Reference} · {ticket.Title} · {StatusLabel(ticket.Status)} · {ticket.Urgency} priority");
        if (ticket.AssignedContractor is not null)
        {
            grounding.Add($"Contractor assigned: {ticket.AssignedContractor.Name} ({ticket.AssignedContractor.Trade}).");
        }
        else
        {
            grounding.Add("No contractor is recorded as assigned.");
        }
        grounding.Add("Appointment time: not recorded on the job.");

        string draft;
        if (ticket.Status is MaintenanceStatus.Completed or MaintenanceStatus.Closed)
        {
            grounding.Add($"Completed{(ticket.CompletedAt is null ? "" : $" on {ticket.CompletedAt:d MMMM yyyy}")}.");
            draft = $"""
                Dear {FirstName(message.FromName)},

                Thank you for your email about {ticket.Title.ToLowerInvariant()} at {TicketAddress(ticket)}.

                The repair record shows this job was completed{(ticket.CompletedAt is null ? "" : $" on {ticket.CompletedAt:d MMMM yyyy}")}. If the issue has returned, please let us know and we'll reopen it.

                Kind regards,
                Keyhaven Lettings
                """;
            return (draft, AutomationMode.Drafted, 93, InboxStatus.DraftReady);
        }

        var contractorLine = ticket.AssignedContractor is null
            ? "The repair is logged and is waiting for a contractor to be assigned."
            : $"A contractor ({ticket.AssignedContractor.Name}) has been assigned to the repair.";

        var appointmentLine = "I can't see an appointment time recorded yet, so I'll ask the team to confirm one with you.";

        draft = $"""
            Dear {FirstName(message.FromName)},

            Thank you for your email about the {ticket.Title.ToLowerInvariant()} at {TicketAddress(ticket)}.

            The repair record (reference {ticket.Reference}) shows the job is {StatusLabel(ticket.Status).ToLowerInvariant()}. {contractorLine} {appointmentLine}

            Kind regards,
            Keyhaven Lettings
            """;

        grounding.Add("Draft only: an appointment has not been agreed, so no time is promised.");
        return (draft, AutomationMode.Drafted, 94, InboxStatus.DraftReady);
    }

    private async Task<(string Draft, AutomationMode Mode, int Confidence, InboxStatus Status)> BuildAccessReplyAsync(
        InboxMessage message, List<string> grounding, CancellationToken ct)
    {
        var ticket = await LoadTicketAsync(message, ct);
        if (ticket is null)
        {
            grounding.Add("No job record was found for this property and sender.");
            return (
                "Thank you for your email. I can't find the job record for this property, so the team will confirm access arrangements with you directly.",
                AutomationMode.Escalated, 30, InboxStatus.NeedsHuman);
        }

        grounding.Add($"Job record: {ticket.Reference} · {ticket.Title} · {StatusLabel(ticket.Status)}");
        if (string.IsNullOrWhiteSpace(ticket.AccessInstructions))
        {
            grounding.Add("Access instructions: not recorded on the job.");
            return (
                $"Thank you for your email about {ticket.Reference}. I can't see access instructions recorded on the job yet, so the team will confirm arrangements with you before you attend.",
                AutomationMode.Drafted, 70, InboxStatus.DraftReady);
        }

        grounding.Add($"Access instructions recorded: “{ticket.AccessInstructions}”");

        var draft = $"""
            Dear {FirstName(message.FromName)},

            Thank you for your email about the job at {TicketAddress(ticket)} (reference {ticket.Reference}).

            The job record says: {ticket.AccessInstructions}

            Please let us know if you need anything else before attending.

            Kind regards,
            Keyhaven Lettings
            """;
        return (draft, AutomationMode.AutoSent, 95, InboxStatus.AutoReplied);
    }

    private async Task<(string Draft, AutomationMode Mode, int Confidence, InboxStatus Status)> BuildStatementReplyAsync(
        InboxMessage message, List<string> grounding, DateOnly today, CancellationToken ct)
    {
        var owner = message.LinkedOwnerId is null
            ? null
            : await db.Owners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == message.LinkedOwnerId, ct);

        if (owner is null)
        {
            grounding.Add("No owner record is linked to this message.");
            return (
                "Thank you for your email. I can't match it to an owner record, so a member of the team will pick it up.",
                AutomationMode.Escalated, 28, InboxStatus.NeedsHuman);
        }

        // The most recent complete calendar quarter.
        var quarterStart = new DateOnly(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1).AddMonths(-3);
        var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

        var chargeCount = await db.RentCharges
            .Where(c => c.Tenancy!.Property!.OwnerId == owner.Id && c.PeriodStart >= quarterStart && c.PeriodStart <= quarterEnd)
            .CountAsync(ct);

        if (chargeCount == 0)
        {
            grounding.Add($"Owner record: {owner.DisplayName}. No rent charges found for {quarterStart:d MMM yyyy}–{quarterEnd:d MMM yyyy}.");
            return (
                $"Dear {FirstName(message.FromName)},\n\nThank you for your email. I can't see a prepared statement for the last quarter on the owner record yet, so the team will check and come back to you.\n\nKind regards,\nKeyhaven Lettings",
                AutomationMode.Escalated, 40, InboxStatus.NeedsHuman);
        }

        grounding.Add($"Owner record: {owner.DisplayName} ({owner.CompanyName}) · {owner.Status} · management fee {owner.ManagementFeePercent:0.#}%");
        grounding.Add($"Statement period available: {quarterStart:d MMMM yyyy} to {quarterEnd:d MMMM yyyy} · {chargeCount} rent charge(s) recorded.");

        var draft = $"""
            Dear {FirstName(message.FromName)},

            The owner record shows a statement is available for {quarterStart:d MMMM yyyy} to {quarterEnd:d MMMM yyyy}.

            Is that the quarter you had in mind? If so, the team will send it across.

            Kind regards,
            Keyhaven Lettings
            """;
        return (draft, AutomationMode.Drafted, 92, InboxStatus.DraftReady);
    }

    private async Task<(string Draft, AutomationMode Mode, int Confidence, InboxStatus Status)> BuildRentReplyAsync(
        InboxMessage message, List<string> grounding, DateOnly today, CancellationToken ct)
    {
        var tenancy = message.LinkedTenancyId is null
            ? null
            : await db.Tenancies.AsNoTracking()
                .Include(t => t.Property)
                .Include(t => t.Tenant)
                .FirstOrDefaultAsync(t => t.Id == message.LinkedTenancyId, ct);

        if (tenancy is null || tenancy.Property is null || tenancy.Tenant is null)
        {
            grounding.Add("No active tenancy could be linked to this message.");
            return (
                "Thank you for your email. I can't match it to a tenancy record, so a member of the team will check your account and reply.",
                AutomationMode.Escalated, 30, InboxStatus.NeedsHuman);
        }

        var charges = await db.RentCharges.AsNoTracking()
            .Where(c => c.TenancyId == tenancy.Id)
            .OrderBy(c => c.DueDate)
            .ToListAsync(ct);

        var outstanding = charges.Where(c => c.AmountPaid < c.Amount).ToList();
        var total = outstanding.Sum(c => c.Amount - c.AmountPaid);

        grounding.Add($"Tenancy record: {tenancy.Property.ShortAddress} · {tenancy.Tenant.FullName} · rent £{tenancy.RentAmount:N0} pcm · {tenancy.Status}");
        grounding.Add($"Rent ledger: {charges.Count} charge(s); £{total:N2} outstanding across {outstanding.Count} charge(s).");

        if (total <= 0)
        {
            var upToDate = $"""
                Dear {FirstName(message.FromName)},

                Thank you for your email about your rent account.

                Our records show your rent is up to date as at {today:d MMMM yyyy}.

                Kind regards,
                Keyhaven Lettings
                """;
            return (upToDate, AutomationMode.AutoSent, 94, InboxStatus.AutoReplied);
        }

        var oldest = outstanding.Min(c => c.DueDate);
        grounding.Add($"Oldest unpaid charge due: {oldest:d MMMM yyyy}.");
        grounding.Add("Draft only: no payment arrangement has been agreed.");

        var draft = $"""
            Dear {FirstName(message.FromName)},

            Thank you for your email.

            The rent ledger for {tenancy.Property.ShortAddress} shows £{total:N2} outstanding across {outstanding.Count} charge(s), with the oldest due on {oldest:d MMMM yyyy}.

            I've noted your request to discuss a payment plan. Any arrangement needs to be agreed with a member of the team, so I can't confirm one from this message.

            Kind regards,
            Keyhaven Lettings
            """;
        return (draft, AutomationMode.Drafted, 93, InboxStatus.DraftReady);
    }

    private async Task<(string Draft, AutomationMode Mode, int Confidence, InboxStatus Status)> BuildViewingReplyAsync(
        InboxMessage message, List<string> grounding, DateTime now, CancellationToken ct)
    {
        var viewing = await db.Viewings.AsNoTracking()
            .Include(v => v.Property)
            .Where(v => v.ApplicantEmail.ToLower() == message.FromEmail.ToLower()
                        && v.ScheduledAt >= now.AddHours(-2)
                        && v.Status != ViewingStatus.Cancelled)
            .OrderBy(v => v.ScheduledAt)
            .FirstOrDefaultAsync(ct);

        if (viewing?.Property is null)
        {
            grounding.Add($"No upcoming viewing was found for {message.FromEmail}.");
            return (
                "Thank you for your email. I can't see an upcoming viewing linked to this email address, so the team will check the diary and come back to you.",
                AutomationMode.Escalated, 35, InboxStatus.NeedsHuman);
        }

        grounding.Add($"Viewing record: {viewing.Property.ShortAddress} · {viewing.ScheduledAt:d MMMM yyyy} at {viewing.ScheduledAt:h:mmtt} · {viewing.Status} · {viewing.ApplicantName}");
        grounding.Add("No alternative slot is recorded, so the draft doesn't confirm one.");

        var draft = $"""
            Dear {FirstName(message.FromName)},

            The viewing record shows a viewing at {viewing.Property.ShortAddress} on {viewing.ScheduledAt:d MMMM yyyy} at {TimeLabel(viewing.ScheduledAt)}.

            I've noted that you'd like to change it. The team will need to check the diary, so I can't confirm an alternative time from this message.

            Kind regards,
            Keyhaven Lettings
            """;
        return (draft, AutomationMode.Drafted, 92, InboxStatus.DraftReady);
    }

    private async Task<MaintenanceTicket?> LoadTicketAsync(InboxMessage message, CancellationToken ct)
    {
        var ticketQuery = db.MaintenanceTickets
            .Include(t => t.Property)
            .Include(t => t.AssignedContractor)
            .AsQueryable();

        if (message.LinkedTicketId is not null)
        {
            return await ticketQuery.FirstOrDefaultAsync(t => t.Id == message.LinkedTicketId, ct);
        }

        if (message.LinkedPropertyId is null)
        {
            return null;
        }

        return await ticketQuery
            .Where(t => t.PropertyId == message.LinkedPropertyId && t.Status != MaintenanceStatus.Closed)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    private static string TimeLabel(DateTime value) => value.ToString("h:mmtt").ToLowerInvariant();

    private static string TicketAddress(MaintenanceTicket ticket) =>
        ticket.Property is null ? "the property" : $"{ticket.Property.AddressLine1}, {ticket.Property.City}";

    private static ContactType ContactTypeFor(InboxCategory category) => category switch
    {
        InboxCategory.Statement => ContactType.Owner,
        InboxCategory.Rent => ContactType.Tenant,
        InboxCategory.Repair => ContactType.Tenant,
        InboxCategory.Access => ContactType.Contractor,
        _ => ContactType.Applicant
    };

    private static string FirstLine(string text)
    {
        var line = text.Replace("\r", "").Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
        return line.Length > 180 ? line[..177] + "…" : line;
    }

    private static string FirstName(string name)
    {
        var trimmed = name.Trim();
        var space = trimmed.IndexOf(' ');
        return space > 0 ? trimmed[..space] : trimmed;
    }

    private static string StatusLabel(Enum value) => value switch
    {
        PropertyStatus.UnderOffer => "Under offer",
        MaintenanceStatus.AwaitingApproval => "Awaiting approval",
        MaintenanceStatus.InProgress => "In progress",
        _ => System.Text.RegularExpressions.Regex.Replace(value.ToString(), "(\\B[A-Z])", " $1")
    };

    private static bool ContainsAny(string text, params string[] phrases) =>
        phrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
}
