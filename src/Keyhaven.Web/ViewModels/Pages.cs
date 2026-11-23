using Keyhaven.Web.Models;
using Keyhaven.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Keyhaven.Web.ViewModels;

public record TenancyListRow(Tenancy Tenancy, decimal Outstanding, int OverdueCharges);

public class TenanciesIndexViewModel
{
    public List<TenancyListRow> Rows { get; set; } = [];
    public int ActiveCount { get; set; }
    public int EndingCount { get; set; }
    public decimal RentRoll { get; set; }
    public decimal Outstanding { get; set; }
    public string? Query { get; set; }
    public TenancyStatus? Status { get; set; }
}

public record PropertyListRow(Property Property, Tenancy? ActiveTenancy, int OpenTickets, ComplianceState WorstCompliance);

public class PropertiesIndexViewModel
{
    public List<PropertyListRow> Rows { get; set; } = [];
    public string? Status { get; set; }
    public string? Query { get; set; }
    public int TotalProperties { get; set; }
    public int LetProperties { get; set; }
    public int AvailableProperties { get; set; }
    public decimal RentRoll { get; set; }
}

public class PropertyDetailsViewModel
{
    public Property Property { get; set; } = null!;
    public Tenancy? ActiveTenancy { get; set; }
    public List<Tenancy> Tenancies { get; set; } = [];
    public List<ComplianceRow> Compliance { get; set; } = [];
    public int ComplianceScore { get; set; }
    public List<MaintenanceTicket> OpenTickets { get; set; } = [];
    public List<KeyItem> Keys { get; set; } = [];
    public List<Viewing> UpcomingViewings { get; set; } = [];
    public List<Enquiry> RecentEnquiries { get; set; } = [];
    public decimal Outstanding { get; set; }
    public decimal CollectedLastSixMonths { get; set; }
    public List<RentCharge> RecentCharges { get; set; } = [];
}

public class TenancyDetailsViewModel
{
    public Tenancy Tenancy { get; set; } = null!;
    public decimal Outstanding { get; set; }
    public List<RentCharge> Charges { get; set; } = [];
    public List<ContactNote> Notes { get; set; } = [];
}

public class RentIndexViewModel
{
    public List<RentCharge> Charges { get; set; } = [];
    public ArrearsSummary Arrears { get; set; } = new(0, 0, 0, 0, 0);
    public decimal BilledThisMonth { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal TotalOutstanding { get; set; }
    public string Filter { get; set; } = "all";
    public string? Query { get; set; }
}

public class MaintenanceIndexViewModel
{
    public List<MaintenanceTicket> Tickets { get; set; } = [];
    public string Filter { get; set; } = "open";
    public string? Query { get; set; }
    public int OpenCount { get; set; }
    public int NewCount { get; set; }
    public int UrgentCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedThisMonth { get; set; }
}

public class MaintenanceDetailsViewModel
{
    public MaintenanceTicket Ticket { get; set; } = null!;
    public List<Contractor> Contractors { get; set; } = [];
}

public class ComplianceIndexViewModel
{
    public List<ComplianceRow> Rows { get; set; } = [];
    public ComplianceSummary Summary { get; set; } = new(0, 0, 0, 0, 0, 0, 100);
    public string? State { get; set; }
    public ComplianceType? Type { get; set; }
}

public class ViewingsIndexViewModel
{
    public List<Viewing> Upcoming { get; set; } = [];
    public List<Viewing> Past { get; set; } = [];
    public DateTime WeekStart { get; set; }
}

public class EnquiriesIndexViewModel
{
    public List<Enquiry> Enquiries { get; set; } = [];
    public List<Property> Properties { get; set; } = [];
    public string Filter { get; set; } = "open";
}

public class InboxIndexViewModel
{
    public List<InboxMessage> Messages { get; set; } = [];
    public string Filter { get; set; } = "attention";
    public int NeedsAttention { get; set; }
    public int Drafts { get; set; }
    public int AutoReplied { get; set; }
    public int HandledToday { get; set; }
}

public class InboxDetailsViewModel
{
    public InboxMessage Message { get; set; } = null!;
    public Property? Property { get; set; }
    public Tenancy? Tenancy { get; set; }
    public MaintenanceTicket? Ticket { get; set; }
    public Owner? Owner { get; set; }
    public List<InboxMessage> Queue { get; set; } = [];
}

public class KeysIndexViewModel
{
    public List<KeyItem> Keys { get; set; } = [];
    public string Filter { get; set; } = "all";
    public int CheckedOut { get; set; }
    public int Overdue { get; set; }
    public int Total { get; set; }
}

public class OwnerDetailsViewModel
{
    public Owner Owner { get; set; } = null!;
    public List<Property> Properties { get; set; } = [];
    public List<ComplianceRow> Compliance { get; set; } = [];
    public List<ContactNote> Notes { get; set; } = [];
    public decimal RentRoll { get; set; }
    public decimal Arrears { get; set; }
    public int ActiveTenancies { get; set; }
}

public class ContactsIndexViewModel
{
    public List<ContactNote> Notes { get; set; } = [];
    public ContactType? Type { get; set; }
    public string? Query { get; set; }
    public List<ContactNote> FollowUps { get; set; } = [];
    public int NoteCount { get; set; }
}

public class ContractorDetailsViewModel
{
    public Contractor Contractor { get; set; } = null!;
    public List<MaintenanceTicket> ActiveJobs { get; set; } = [];
    public List<MaintenanceTicket> CompletedJobs { get; set; } = [];
}

public record OwnerListRow(Owner Owner, int PropertyCount, int ActiveTenancies, decimal RentRoll, decimal Arrears);

public class OwnersIndexViewModel
{
    public List<OwnerListRow> Rows { get; set; } = [];
    public int ActiveCount { get; set; }
    public int OnboardingCount { get; set; }
    public int PropertyCount { get; set; }
    public decimal RentRoll { get; set; }
    public string? Query { get; set; }
    public OwnerStatus? Status { get; set; }
}

public record ContractorListRow(Contractor Contractor, int ActiveJobs, int CompletedJobs, decimal QuotedValue);

public class ContractorsIndexViewModel
{
    public List<ContractorListRow> Rows { get; set; } = [];
    public int ActiveCount { get; set; }
    public int ActiveJobs { get; set; }
    public decimal QuotedValue { get; set; }
}

public class HomeViewModel
{
    public List<Property> AvailableProperties { get; set; } = [];
    public int ManagedProperties { get; set; }
    public int ActiveTenancies { get; set; }
    public int ComplianceChecks { get; set; }
}
