using System.ComponentModel.DataAnnotations;

namespace Keyhaven.Web.Models;

public class Owner
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = "";

    [StringLength(120)]
    public string? CompanyName { get; set; }

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    public DateOnly OnboardedOn { get; set; }

    public OwnerStatus Status { get; set; } = OwnerStatus.Prospect;

    public bool AgreementSigned { get; set; }

    public DateOnly? AgreementSignedOn { get; set; }

    [Range(0, 30)]
    public decimal ManagementFeePercent { get; set; } = 10m;

    public int? PortalUserId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public ICollection<Property> Properties { get; set; } = new List<Property>();
}

public class Property
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

    public int OwnerId { get; set; }
    public Owner? Owner { get; set; }

    public int? ManagerId { get; set; }
    public AppUser? Manager { get; set; }

    [StringLength(2)]
    public string? EpcRating { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(160)]
    public string? MarketingHeadline { get; set; }

    [StringLength(600)]
    public string? MarketingSummary { get; set; }

    public bool ListingPublished { get; set; }

    public DateOnly? ListedOn { get; set; }

    [StringLength(160)]
    public string? Channels { get; set; }

    [StringLength(240)]
    public string? ImageUrl { get; set; }

    public DateOnly? GasSafetyOn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tenancy> Tenancies { get; set; } = new List<Tenancy>();
    public ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();
    public ICollection<ComplianceCertificate> Certificates { get; set; } = new List<ComplianceCertificate>();
    public ICollection<Viewing> Viewings { get; set; } = new List<Viewing>();
    public ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();
    public ICollection<KeyItem> Keys { get; set; } = new List<KeyItem>();

    public string FullAddress => string.Join(", ", new[] { AddressLine1, AddressLine2, City, Postcode }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public string ShortAddress => $"{AddressLine1}, {City}";
}

public class KeyItem
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Label { get; set; } = "";

    public KeyType Type { get; set; } = KeyType.HouseKey;

    [StringLength(40)]
    public string? Serial { get; set; }

    public int? PropertyId { get; set; }
    public Property? Property { get; set; }

    public bool IsCheckedOut { get; set; }

    [StringLength(120)]
    public string? CheckedOutToName { get; set; }

    public DateTime? CheckedOutAt { get; set; }

    [StringLength(240)]
    public string? CheckOutReason { get; set; }

    public DateTime? DueBackAt { get; set; }

    public DateTime? CheckedInAt { get; set; }

    [StringLength(400)]
    public string? Notes { get; set; }
}
