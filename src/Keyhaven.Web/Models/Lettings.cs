using System.ComponentModel.DataAnnotations;

namespace Keyhaven.Web.Models;

public class Tenant
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [StringLength(40)]
    public string? Phone { get; set; }

    public ReferencingStatus ReferencingStatus { get; set; } = ReferencingStatus.NotStarted;

    public bool RightToRentChecked { get; set; }

    public DateOnly? RightToRentCheckedOn { get; set; }

    public int? PortalUserId { get; set; }

    [StringLength(600)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tenancy> Tenancies { get; set; } = new List<Tenancy>();
}

public class Tenancy
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [Range(0, 50000)]
    public decimal RentAmount { get; set; }

    [Range(0, 50000)]
    public decimal DepositAmount { get; set; }

    [StringLength(40)]
    public string? DepositScheme { get; set; }

    public bool DepositRegistered { get; set; }

    public DateOnly? DepositRegisteredOn { get; set; }

    public TenancyStatus Status { get; set; } = TenancyStatus.Pending;

    public DateOnly? NoticeServedOn { get; set; }

    [StringLength(60)]
    public string? NoticeType { get; set; }

    [StringLength(600)]
    public string? Notes { get; set; }

    public ICollection<RentCharge> RentCharges { get; set; } = new List<RentCharge>();

    public string DepositSchemeDisplay => string.IsNullOrWhiteSpace(DepositScheme) ? "Not registered" : DepositScheme;
}

public class RentCharge
{
    public int Id { get; set; }

    public int TenancyId { get; set; }
    public Tenancy? Tenancy { get; set; }

    public DateOnly PeriodStart { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Amount { get; set; }

    public decimal AmountPaid { get; set; }

    public DateOnly? PaidOn { get; set; }

    [StringLength(60)]
    public string? BankReference { get; set; }

    public RentReconciliation Reconciliation { get; set; } = RentReconciliation.Unreconciled;

    public DateOnly? ChaserSentOn { get; set; }

    [StringLength(400)]
    public string? Notes { get; set; }

    public decimal Outstanding => Math.Max(0m, Amount - AmountPaid);
    public bool IsFullyPaid => AmountPaid >= Amount;

    public bool IsOverdue(DateOnly today) => Outstanding > 0 && DueDate < today;
}

public class Viewing
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    [Required, StringLength(120)]
    public string ApplicantName { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string ApplicantEmail { get; set; } = "";

    [StringLength(40)]
    public string? ApplicantPhone { get; set; }

    public DateTime ScheduledAt { get; set; }

    public int DurationMinutes { get; set; } = 30;

    public ViewingStatus Status { get; set; } = ViewingStatus.Requested;

    public PrescreenStatus PrescreenStatus { get; set; } = PrescreenStatus.NotStarted;

    [StringLength(600)]
    public string? AgentNotes { get; set; }

    [StringLength(1000)]
    public string? Feedback { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Enquiry
{
    public int Id { get; set; }

    public int? PropertyId { get; set; }
    public Property? Property { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [StringLength(40)]
    public string? Phone { get; set; }

    [Required, StringLength(2000)]
    public string Message { get; set; } = "";

    public EnquirySource Source { get; set; } = EnquirySource.Website;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public EnquiryStatus Status { get; set; } = EnquiryStatus.New;

    [StringLength(600)]
    public string? Notes { get; set; }

    public int? ConvertedViewingId { get; set; }
}
