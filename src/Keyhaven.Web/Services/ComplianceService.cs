using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Services;

public record ComplianceRow(
    Property Property,
    ComplianceType Type,
    ComplianceCertificate? Certificate,
    ComplianceState State,
    int DaysUntilExpiry,
    bool Required);

public record ComplianceSummary(
    int Properties,
    int RequiredChecks,
    int ValidChecks,
    int ExpiringChecks,
    int ExpiredChecks,
    int MissingChecks,
    int Score);

public class ComplianceService(KeyhavenDbContext db)
{
    public const int WarningDays = 30;

    public static IReadOnlyList<ComplianceType> RequiredFor(Property property)
    {
        return property.Type == PropertyType.Hmo
            ? [ComplianceType.GasSafety, ComplianceType.Epc, ComplianceType.Eicr, ComplianceType.HmoLicence]
            : [ComplianceType.GasSafety, ComplianceType.Epc, ComplianceType.Eicr];
    }

    public static ComplianceState StateFor(DateOnly? expiresOn, DateOnly today)
    {
        if (expiresOn is null)
        {
            return ComplianceState.Missing;
        }

        var days = expiresOn.Value.DayNumber - today.DayNumber;
        if (days < 0) return ComplianceState.Expired;
        if (days <= WarningDays) return ComplianceState.ExpiringSoon;
        return ComplianceState.Valid;
    }

    public async Task<List<ComplianceRow>> GetRowsAsync(int? ownerId = null, int? propertyId = null, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var query = db.Properties.AsNoTracking().Include(p => p.Owner).AsQueryable();
        if (ownerId is not null)
        {
            query = query.Where(p => p.OwnerId == ownerId);
        }
        if (propertyId is not null)
        {
            query = query.Where(p => p.Id == propertyId);
        }

        var properties = await query.OrderBy(p => p.Reference).ToListAsync(ct);
        var propertyIds = properties.Select(p => p.Id).ToList();

        var certs = await db.ComplianceCertificates.AsNoTracking()
            .Where(c => propertyIds.Contains(c.PropertyId))
            .ToListAsync(ct);

        // The current certificate for a type is the one with the latest expiry.
        var current = certs
            .GroupBy(c => (c.PropertyId, c.Type))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.ExpiresOn).First());

        var rows = new List<ComplianceRow>();
        foreach (var property in properties)
        {
            foreach (var type in RequiredFor(property))
            {
                current.TryGetValue((property.Id, type), out var cert);
                var state = StateFor(cert?.ExpiresOn, today);
                var days = cert is null ? int.MinValue : cert.ExpiresOn.DayNumber - today.DayNumber;
                rows.Add(new ComplianceRow(property, type, cert, state, days, true));
            }
        }

        return rows;
    }

    public async Task<ComplianceSummary> GetSummaryAsync(int? ownerId = null, CancellationToken ct = default)
    {
        var rows = await GetRowsAsync(ownerId, ct: ct);
        var required = rows.Count;
        var valid = rows.Count(r => r.State == ComplianceState.Valid);
        var expiring = rows.Count(r => r.State == ComplianceState.ExpiringSoon);
        var expired = rows.Count(r => r.State == ComplianceState.Expired);
        var missing = rows.Count(r => r.State == ComplianceState.Missing);
        var score = required == 0 ? 100 : (int)Math.Round((valid + expiring * 0.5m) / required * 100m);
        return new ComplianceSummary(rows.Select(r => r.Property.Id).Distinct().Count(), required, valid, expiring, expired, missing, score);
    }

    public async Task<List<ComplianceRow>> GetActionRowsAsync(int? ownerId = null, int withinDays = 60, CancellationToken ct = default)
    {
        var rows = await GetRowsAsync(ownerId, ct: ct);
        return rows
            .Where(r => r.State is ComplianceState.Expired or ComplianceState.Missing
                        || (r.State == ComplianceState.ExpiringSoon && r.DaysUntilExpiry <= withinDays))
            .OrderBy(r => r.State == ComplianceState.Missing ? -999 : r.DaysUntilExpiry)
            .ToList();
    }

    public static string TypeLabel(ComplianceType type) => type switch
    {
        ComplianceType.GasSafety => "Gas Safety",
        ComplianceType.Epc => "EPC",
        ComplianceType.Eicr => "EICR",
        ComplianceType.HmoLicence => "HMO licence",
        ComplianceType.Legionella => "Legionella",
        _ => type.ToString()
    };
}
