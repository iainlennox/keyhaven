using System.ComponentModel.DataAnnotations;

namespace Keyhaven.Web.Models;

public class Contractor
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    [Required, StringLength(80)]
    public string Trade { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [StringLength(40)]
    public string? Phone { get; set; }

    [Range(0, 5)]
    public decimal Rating { get; set; } = 4.5m;

    public bool Insured { get; set; } = true;

    public bool Active { get; set; } = true;

    [StringLength(600)]
    public string? Notes { get; set; }

    public ICollection<MaintenanceTicket> Tickets { get; set; } = new List<MaintenanceTicket>();
}

public class MaintenanceTicket
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Reference { get; set; } = "";

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = "";

    [Required, StringLength(2000)]
    public string Description { get; set; } = "";

    public MaintenanceCategory Category { get; set; } = MaintenanceCategory.Other;

    public Urgency Urgency { get; set; } = Urgency.Routine;

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.New;

    [StringLength(600)]
    public string? AiSummary { get; set; }

    public Urgency? AiSuggestedUrgency { get; set; }

    [StringLength(400)]
    public string? AiReason { get; set; }

    [StringLength(500)]
    public string? AccessInstructions { get; set; }

    public int? AssignedContractorId { get; set; }
    public Contractor? AssignedContractor { get; set; }

    public decimal? QuoteAmount { get; set; }

    public bool? QuoteApproved { get; set; }

    [StringLength(400)]
    public string? QuoteNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [StringLength(600)]
    public string? CompletionNotes { get; set; }
}

public class ComplianceCertificate
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public ComplianceType Type { get; set; }

    [Required, StringLength(80)]
    public string Reference { get; set; } = "";

    public DateOnly IssuedOn { get; set; }

    public DateOnly ExpiresOn { get; set; }

    [StringLength(120)]
    public string? Provider { get; set; }

    [StringLength(400)]
    public string? Notes { get; set; }

    public int? RenewalContractorId { get; set; }
    public Contractor? RenewalContractor { get; set; }

    public RenewalStatus RenewalStatus { get; set; } = RenewalStatus.None;

    public DateOnly? RenewalBookedFor { get; set; }
}
