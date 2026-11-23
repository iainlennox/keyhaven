using Keyhaven.Web.Models;
using Keyhaven.Web.Services;

namespace Keyhaven.Web.ViewModels;

public record RentMonthPoint(string Label, decimal Billed, decimal Collected);

public class StaffDashboardViewModel
{
    public string Name { get; set; } = "";
    public bool IsLandlord { get; set; }
    public int PropertyCount { get; set; }
    public int LetCount { get; set; }
    public int AvailableCount { get; set; }
    public int ActiveTenancies { get; set; }
    public decimal MonthlyRentRoll { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal DueThisMonth { get; set; }
    public decimal ArrearsTotal { get; set; }
    public int OpenTickets { get; set; }
    public int ComplianceActions { get; set; }
    public int ViewingsThisWeek { get; set; }
    public int DraftsWaiting { get; set; }
    public int AutoRepliedToday { get; set; }
    public int ComplianceScore { get; set; }

    public List<RentMonthPoint> RentHistory { get; set; } = [];
    public List<ArrearsRow> Arrears { get; set; } = [];
    public List<ComplianceRow> ComplianceWatch { get; set; } = [];
    public List<Viewing> UpcomingViewings { get; set; } = [];
    public List<MaintenanceTicket> RecentTickets { get; set; } = [];
    public List<ContactNote> RecentActivity { get; set; } = [];
    public List<InboxMessage> InboxNeedsAttention { get; set; } = [];

    public int CollectedPercent => DueThisMonth <= 0 ? 100 : (int)Math.Round(CollectedThisMonth / DueThisMonth * 100);
}

public class TenantDashboardViewModel
{
    public Tenant? Tenant { get; set; }
    public Tenancy? Tenancy { get; set; }
    public Property? Property { get; set; }
    public decimal Outstanding { get; set; }
    public decimal RentAmount { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public bool DepositRegistered { get; set; }
    public string? DepositScheme { get; set; }

    public List<RentCharge> RecentCharges { get; set; } = [];
    public List<MaintenanceTicket> OpenTickets { get; set; } = [];
    public List<ContactNote> RecentNotes { get; set; } = [];
}

public class ContractorDashboardViewModel
{
    public Contractor? Contractor { get; set; }
    public List<MaintenanceTicket> ActiveJobs { get; set; } = [];
    public List<MaintenanceTicket> RecentCompleted { get; set; } = [];
    public int UrgentJobs { get; set; }
    public decimal QuotedValue { get; set; }
}
