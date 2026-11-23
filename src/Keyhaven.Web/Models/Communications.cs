using System.ComponentModel.DataAnnotations;

namespace Keyhaven.Web.Models;

public class InboxMessage
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string FromName { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string FromEmail { get; set; } = "";

    [Required, StringLength(200)]
    public string Subject { get; set; } = "";

    [Required, StringLength(4000)]
    public string Body { get; set; } = "";

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public InboxCategory Category { get; set; } = InboxCategory.Other;

    public InboxStatus Status { get; set; } = InboxStatus.New;

    public int? LinkedPropertyId { get; set; }
    public Property? LinkedProperty { get; set; }

    public int? LinkedTenancyId { get; set; }
    public Tenancy? LinkedTenancy { get; set; }

    public int? LinkedTicketId { get; set; }
    public MaintenanceTicket? LinkedTicket { get; set; }

    public int? LinkedOwnerId { get; set; }
    public Owner? LinkedOwner { get; set; }

    [StringLength(4000)]
    public string? DraftReply { get; set; }

    /// <summary>Newline separated list of the records the reply was built from.</summary>
    [StringLength(2000)]
    public string? Grounding { get; set; }

    [Range(0, 100)]
    public int Confidence { get; set; }

    public AutomationMode AutomationMode { get; set; } = AutomationMode.Pending;

    public DateTime? HandledAt { get; set; }

    [StringLength(120)]
    public string? HandledBy { get; set; }
}

public class ContactNote
{
    public int Id { get; set; }

    public ContactType ContactType { get; set; } = ContactType.Other;

    [Required, StringLength(120)]
    public string ContactName { get; set; } = "";

    [StringLength(200)]
    public string? ContactEmail { get; set; }

    public ContactChannel Channel { get; set; } = ContactChannel.Note;

    [Required, StringLength(1000)]
    public string Summary { get; set; } = "";

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    [StringLength(120)]
    public string CreatedBy { get; set; } = "System";

    public int? PropertyId { get; set; }
    public Property? Property { get; set; }

    public bool IsFollowUp { get; set; }

    public DateOnly? FollowUpOn { get; set; }
}
