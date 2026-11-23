using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Services;

public record ArrearsRow(
    Tenancy Tenancy,
    decimal Outstanding,
    int OverdueCharges,
    int DaysOverdue,
    DateOnly OldestDue);

public record ArrearsSummary(decimal Total, int TenanciesInArrears, decimal Bucket0To30, decimal Bucket31To60, decimal Bucket60Plus);

public class ArrearsService(KeyhavenDbContext db)
{
    public async Task<List<ArrearsRow>> GetRowsAsync(int? ownerId = null, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var query = db.RentCharges
            .Include(c => c.Tenancy)!.ThenInclude(t => t!.Property)
            .Include(c => c.Tenancy)!.ThenInclude(t => t!.Tenant)
            .Where(c => c.AmountPaid < c.Amount)
            .AsQueryable();

        if (ownerId is not null)
        {
            query = query.Where(c => c.Tenancy!.Property!.OwnerId == ownerId);
        }

        var charges = await query.AsNoTracking().ToListAsync(ct);

        return charges
            .GroupBy(c => c.TenancyId)
            .Select(g =>
            {
                var first = g.First();
                var overdue = g.Where(c => c.IsOverdue(today)).ToList();
                var oldest = overdue.Count > 0 ? overdue.Min(c => c.DueDate) : g.Min(c => c.DueDate);
                var days = Math.Max(0, today.DayNumber - oldest.DayNumber);
                return new ArrearsRow(
                    first.Tenancy!,
                    g.Sum(c => c.Outstanding),
                    overdue.Count,
                    days,
                    oldest);
            })
            .OrderByDescending(r => r.Outstanding)
            .ToList();
    }

    public async Task<ArrearsSummary> GetSummaryAsync(int? ownerId = null, CancellationToken ct = default)
    {
        var rows = await GetRowsAsync(ownerId, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        decimal Bucket(int min, int max) => rows
            .Where(r => r.DaysOverdue >= min && r.DaysOverdue <= max)
            .Sum(r => r.Outstanding);

        return new ArrearsSummary(
            rows.Sum(r => r.Outstanding),
            rows.Count,
            Bucket(0, 30),
            Bucket(31, 60),
            rows.Where(r => r.DaysOverdue > 60).Sum(r => r.Outstanding));
    }

    public async Task<decimal> OutstandingForTenancyAsync(int tenancyId, CancellationToken ct = default)
    {
        return await db.RentCharges
            .Where(c => c.TenancyId == tenancyId && c.AmountPaid < c.Amount)
            .SumAsync(c => c.Amount - c.AmountPaid, ct);
    }

    public static string BuildChaserDraft(Tenancy tenancy, decimal outstanding, DateOnly oldestDue, string tenantName)
    {
        var property = tenancy.Property?.ShortAddress ?? "your property";
        return $"""
            Dear {tenantName},

            I'm writing about your rent account for {property}. Our records show £{outstanding:N2} outstanding, with the oldest unpaid charge due on {oldestDue:d MMMM yyyy}.

            If you believe this is incorrect, or if you would like to discuss a payment arrangement, please reply to this email and a member of the team will come back to you.

            Kind regards,
            Keyhaven Lettings
            """;
    }
}
