using System.ComponentModel.DataAnnotations;
using Keyhaven.Web.Models;

namespace Keyhaven.Web.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class PropertyFormModel
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Reference { get; set; } = "";

    [Required, StringLength(160)]
    public string AddressLine1 { get; set; } = "";

    [StringLength(160)]
    public string? AddressLine2 { get; set; }

    [Required, StringLength(80)]
    public string City { get; set; } = "";

    [Required, StringLength(12)]
    public string Postcode { get; set; } = "";

    public PropertyType Type { get; set; } = PropertyType.Flat;

    [Range(0, 20)]
    public int Bedrooms { get; set; }

    [Range(0, 10)]
    public int Bathrooms { get; set; } = 1;

    [Range(0, 50000)]
    public decimal MonthlyRent { get; set; }

    public PropertyStatus Status { get; set; } = PropertyStatus.Available;

    [Required]
    public int OwnerId { get; set; }

    [StringLength(2)]
    public string? EpcRating { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(160)]
    public string? MarketingHeadline { get; set; }

    [StringLength(600)]
    public string? MarketingSummary { get; set; }

    public bool ListingPublished { get; set; }

    [StringLength(160)]
    public string? Channels { get; set; }

    [StringLength(240)]
    public string? ImageUrl { get; set; }
}

public class OwnerFormModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = "";

    [StringLength(120)]
    public string? CompanyName { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    public OwnerStatus Status { get; set; } = OwnerStatus.Prospect;

    [Range(0, 30)]
    public decimal ManagementFeePercent { get; set; } = 10m;

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class TenancyFormModel
{
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [Required]
    public int TenantId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    [Range(0, 50000)]
    public decimal RentAmount { get; set; }

    [Range(0, 50000)]
    public decimal DepositAmount { get; set; }

    [StringLength(40)]
    public string? DepositScheme { get; set; }

    public bool DepositRegistered { get; set; }

    [StringLength(600)]
    public string? Notes { get; set; }
}

public class TenantFormModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [StringLength(40)]
    public string? Phone { get; set; }

    public ReferencingStatus ReferencingStatus { get; set; } = ReferencingStatus.NotStarted;

    public bool RightToRentChecked { get; set; }

    [StringLength(600)]
    public string? Notes { get; set; }
}

public class MaintenanceFormModel
{
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = "";

    [Required, StringLength(2000)]
    public string Description { get; set; } = "";

    public MaintenanceCategory Category { get; set; } = MaintenanceCategory.Other;

    public Urgency Urgency { get; set; } = Urgency.Routine;

    [StringLength(500)]
    public string? AccessInstructions { get; set; }
}

public class ComplianceFormModel
{
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }

    public ComplianceType Type { get; set; } = ComplianceType.GasSafety;

    [Required, StringLength(80)]
    public string Reference { get; set; } = "";

    [Required, DataType(DataType.Date)]
    public DateOnly IssuedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, DataType(DataType.Date)]
    public DateOnly ExpiresOn { get; set; } = DateOnly.FromDateTime(DateTime.Today).AddYears(1);

    [StringLength(120)]
    public string? Provider { get; set; }

    [StringLength(400)]
    public string? Notes { get; set; }
}

public class ViewingFormModel
{
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [Required, StringLength(120)]
    public string ApplicantName { get; set; } = "";

    [Required, EmailAddress]
    public string ApplicantEmail { get; set; } = "";

    [StringLength(40)]
    public string? ApplicantPhone { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

    [Range(15, 180)]
    public int DurationMinutes { get; set; } = 30;

    public ViewingStatus Status { get; set; } = ViewingStatus.Requested;

    public PrescreenStatus PrescreenStatus { get; set; } = PrescreenStatus.NotStarted;

    [StringLength(600)]
    public string? AgentNotes { get; set; }
}

public class ConvertEnquiryModel
{
    public int EnquiryId { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; } = DateTime.Today.AddDays(1).AddHours(11);

    [StringLength(600)]
    public string? AgentNotes { get; set; }
}

public class RentPaymentModel
{
    public int ChargeId { get; set; }

    [Range(0.01, 50000)]
    public decimal Amount { get; set; }

    [Required, DataType(DataType.Date)]
    public DateOnly PaidOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(60)]
    public string? BankReference { get; set; }

    [StringLength(400)]
    public string? Notes { get; set; }
}

public class ContactFormModel
{
    public ContactType ContactType { get; set; } = ContactType.Other;

    [Required, StringLength(120)]
    public string ContactName { get; set; } = "";

    [EmailAddress, StringLength(200)]
    public string? ContactEmail { get; set; }

    public ContactChannel Channel { get; set; } = ContactChannel.Note;

    [Required, StringLength(1000)]
    public string Summary { get; set; } = "";

    public int? PropertyId { get; set; }

    public bool IsFollowUp { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? FollowUpOn { get; set; }
}

public class ContractorFormModel
{    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    [Required, StringLength(80)]
    public string Trade { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [StringLength(40)]
    public string? Phone { get; set; }

    [Range(0, 5)]
    public decimal Rating { get; set; } = 4.5m;

    public bool Insured { get; set; } = true;

    public bool Active { get; set; } = true;

    [StringLength(600)]
    public string? Notes { get; set; }
}

public class ComposeInboxModel
{
    [Required, StringLength(120)]
    public string FromName { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string FromEmail { get; set; } = "";

    [Required, StringLength(200)]
    public string Subject { get; set; } = "";

    [Required, StringLength(4000)]
    public string Body { get; set; } = "";

    public int? PropertyId { get; set; }
}

public class KeyFormModel
{
    [Required, StringLength(120)]
    public string Label { get; set; } = "";

    public KeyType Type { get; set; } = KeyType.HouseKey;

    [StringLength(40)]
    public string? Serial { get; set; }

    public int? PropertyId { get; set; }

    [StringLength(400)]
    public string? Notes { get; set; }
}
