using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Keyhaven.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Services;

public class DashboardService(KeyhavenDbContext db, ArrearsService arrears, ComplianceService compliance)
{
    public async Task<StaffDashboardViewModel> BuildStaffAsync(int? ownerId, string name, CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var properties = await db.Properties.AsNoTracking()
            .Include(p => p.Owner)
            .Where(p => ownerId == null || p.OwnerId == ownerId)
            .OrderBy(p => p.Reference)
            .ToListAsync(ct);

        var propertyIds = properties.Select(p => p.Id).ToList();

        var tenancies = await db.Tenancies.AsNoTracking()
            .Include(t => t.Property)
            .Include(t => t.Tenant)
            .Where(t => propertyIds.Contains(t.PropertyId) && t.Status == TenancyStatus.Active)
            .ToListAsync(ct);

        var charges = await db.RentCharges.AsNoTracking()
            .Where(c => propertyIds.Contains(c.Tenancy!.PropertyId)
                        && c.PeriodStart >= monthStart.AddMonths(-5))
            .ToListAsync(ct);

        var currentCharges = charges.Where(c => c.PeriodStart == monthStart).ToList();
        var dueThisMonth = currentCharges.Sum(c => c.Amount);
        var collectedThisMonth = currentCharges.Sum(c => c.AmountPaid);

        var rentHistory = new List<RentMonthPoint>();
        for (var i = 5; i >= 0; i--)
        {
            var start = monthStart.AddMonths(-i);
            var monthCharges = charges.Where(c => c.PeriodStart == start).ToList();
            rentHistory.Add(new RentMonthPoint(
                start.ToString("MMM"),
                monthCharges.Sum(c => c.Amount),
                monthCharges.Sum(c => c.AmountPaid)));
        }

        var arrearsSummary = await arrears.GetSummaryAsync(ownerId, ct);
        var complianceRows = await compliance.GetActionRowsAsync(ownerId, 60, ct);
        var complianceSummary = await compliance.GetSummaryAsync(ownerId, ct);

        var openTickets = await db.MaintenanceTickets.AsNoTracking()
            .Include(t => t.Property)
            .Include(t => t.AssignedContractor)
            .Where(t => propertyIds.Contains(t.PropertyId)
                        && t.Status != MaintenanceStatus.Completed
                        && t.Status != MaintenanceStatus.Closed)
            .OrderBy(t => t.Urgency)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        var upcomingViewings = await db.Viewings.AsNoTracking()
            .Include(v => v.Property)
            .Where(v => propertyIds.Contains(v.PropertyId)
                        && v.ScheduledAt >= now
                        && (v.Status == ViewingStatus.Requested || v.Status == ViewingStatus.Confirmed))
            .OrderBy(v => v.ScheduledAt)
            .Take(5)
            .ToListAsync(ct);

        var recentActivity = await db.ContactNotes.AsNoTracking()
            .Where(n => n.PropertyId == null || propertyIds.Contains(n.PropertyId.Value))
            .OrderByDescending(n => n.OccurredAt)
            .Take(6)
            .ToListAsync(ct);

        var inbox = new List<InboxMessage>();
        var draftsWaiting = 0;
        var autoRepliedToday = 0;
        if (ownerId is null)
        {
            inbox = await db.InboxMessages.AsNoTracking()
                .Where(m => m.Status == InboxStatus.DraftReady || m.Status == InboxStatus.NeedsHuman || m.Status == InboxStatus.New)
                .OrderByDescending(m => m.ReceivedAt)
                .Take(4)
                .ToListAsync(ct);

            draftsWaiting = await db.InboxMessages.CountAsync(
                m => m.Status == InboxStatus.DraftReady || m.Status == InboxStatus.NeedsHuman || m.Status == InboxStatus.New, ct);

            autoRepliedToday = await db.InboxMessages.CountAsync(
                m => m.Status == InboxStatus.AutoReplied && m.HandledAt >= now.Date, ct);
        }

        var viewingsThisWeek = upcomingViewings.Count(v => v.ScheduledAt <= now.AddDays(7));

        return new StaffDashboardViewModel
        {
            Name = name,
            IsLandlord = ownerId is not null,
            PropertyCount = properties.Count,
            LetCount = properties.Count(p => p.Status == PropertyStatus.Let),
            AvailableCount = properties.Count(p => p.Status == PropertyStatus.Available),
            ActiveTenancies = tenancies.Count,
            MonthlyRentRoll = tenancies.Sum(t => t.RentAmount),
            CollectedThisMonth = collectedThisMonth,
            DueThisMonth = dueThisMonth,
            ArrearsTotal = arrearsSummary.Total,
            OpenTickets = openTickets.Count,
            ComplianceActions = complianceRows.Count,
            ComplianceScore = complianceSummary.Score,
            ViewingsThisWeek = viewingsThisWeek,
            DraftsWaiting = draftsWaiting,
            AutoRepliedToday = autoRepliedToday,
            RentHistory = rentHistory,
            Arrears = (await arrears.GetRowsAsync(ownerId, ct)).Take(4).ToList(),
            ComplianceWatch = complianceRows.Take(5).ToList(),
            UpcomingViewings = upcomingViewings,
            RecentTickets = openTickets.Take(5).ToList(),
            RecentActivity = recentActivity,
            InboxNeedsAttention = inbox
        };
    }

    public async Task<TenantDashboardViewModel> BuildTenantAsync(int tenantId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        var tenancy = await db.Tenancies.AsNoTracking()
            .Include(t => t.Property)
            .Include(t => t.RentCharges)
            .Where(t => t.TenantId == tenantId && t.Status == TenancyStatus.Active)
            .OrderByDescending(t => t.StartDate)
            .FirstOrDefaultAsync(ct);

        var model = new TenantDashboardViewModel { Tenant = tenant, Tenancy = tenancy, Property = tenancy?.Property };

        if (tenancy is not null)
        {
            var charges = tenancy.RentCharges.OrderByDescending(c => c.DueDate).ToList();
            model.RentAmount = tenancy.RentAmount;
            model.Outstanding = charges.Sum(c => c.Outstanding);
            model.DepositRegistered = tenancy.DepositRegistered;
            model.DepositScheme = tenancy.DepositScheme;
            model.RecentCharges = charges.Take(6).ToList();
            model.NextDueDate = charges.Where(c => c.Outstanding > 0).OrderBy(c => c.DueDate).Select(c => (DateOnly?)c.DueDate).FirstOrDefault()
                                ?? charges.OrderByDescending(c => c.DueDate).Select(c => (DateOnly?)c.DueDate.AddMonths(1)).FirstOrDefault();

            model.OpenTickets = await db.MaintenanceTickets.AsNoTracking()
                .Where(t => t.TenantId == tenantId && t.Status != MaintenanceStatus.Completed && t.Status != MaintenanceStatus.Closed)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);

            model.RecentNotes = await db.ContactNotes.AsNoTracking()
                .Where(n => n.ContactEmail == tenant!.Email && n.PropertyId == tenancy.PropertyId)
                .OrderByDescending(n => n.OccurredAt)
                .Take(4)
                .ToListAsync(ct);
        }

        return model;
    }

    public async Task<ContractorDashboardViewModel> BuildContractorAsync(int contractorId, CancellationToken ct = default)
    {
        var contractor = await db.Contractors.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contractorId, ct);

        var active = await db.MaintenanceTickets.AsNoTracking()
            .Include(t => t.Property)
            .Where(t => t.AssignedContractorId == contractorId
                        && t.Status != MaintenanceStatus.Completed
                        && t.Status != MaintenanceStatus.Closed)
            .OrderBy(t => t.Urgency)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        var completed = await db.MaintenanceTickets.AsNoTracking()
            .Include(t => t.Property)
            .Where(t => t.AssignedContractorId == contractorId
                        && (t.Status == MaintenanceStatus.Completed || t.Status == MaintenanceStatus.Closed))
            .OrderByDescending(t => t.CompletedAt)
            .Take(4)
            .ToListAsync(ct);

        return new ContractorDashboardViewModel
        {
            Contractor = contractor,
            ActiveJobs = active,
            RecentCompleted = completed,
            UrgentJobs = active.Count(t => t.Urgency is Urgency.Emergency or Urgency.Urgent),
            QuotedValue = active.Sum(t => t.QuoteAmount ?? 0m)
        };
    }
}
