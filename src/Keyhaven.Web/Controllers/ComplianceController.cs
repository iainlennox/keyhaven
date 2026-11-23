using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Keyhaven.Web.Services;
using Keyhaven.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Controllers;

[Authorize]
public class ComplianceController(KeyhavenDbContext db, CurrentUser current, ComplianceService compliance)
    : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(ComplianceState? state, ComplianceType? type)
    {
        if (IsContractor) return AccessDenied();

        List<ComplianceRow> allRows;
        if (IsTenant && Current.LinkedId is not null)
        {
            var propertyId = await Db.Tenancies.AsNoTracking()
                .Where(t => t.TenantId == Current.LinkedId && t.Status == TenancyStatus.Active)
                .Select(t => (int?)t.PropertyId)
                .FirstOrDefaultAsync();
            allRows = propertyId is null ? new List<ComplianceRow>() : await compliance.GetRowsAsync(propertyId: propertyId);
        }
        else
        {
            allRows = await compliance.GetRowsAsync(Current.OwnerId);
        }

        var valid = allRows.Count(r => r.State == ComplianceState.Valid);
        var expiring = allRows.Count(r => r.State == ComplianceState.ExpiringSoon);
        var expired = allRows.Count(r => r.State == ComplianceState.Expired);
        var missing = allRows.Count(r => r.State == ComplianceState.Missing);
        var score = allRows.Count == 0 ? 100 : (int)Math.Round((valid + expiring * 0.5m) / allRows.Count * 100m);

        var filtered = allRows.AsEnumerable();
        if (state is not null) filtered = filtered.Where(r => r.State == state);
        if (type is not null) filtered = filtered.Where(r => r.Type == type);

        ViewBag.Contractors = new SelectList(
            await Db.Contractors.AsNoTracking().Where(c => c.Active).OrderBy(c => c.Name)
                .Select(c => new { c.Id, Name = c.Name + " — " + c.Trade }).ToListAsync(),
            "Id", "Name");

        return View(new ComplianceIndexViewModel
        {
            Rows = filtered
                .OrderBy(r => r.State == ComplianceState.Missing ? -999 : r.DaysUntilExpiry)
                .ThenBy(r => r.Property.Reference)
                .ToList(),
            Summary = new ComplianceSummary(
                allRows.Select(r => r.Property.Id).Distinct().Count(),
                allRows.Count, valid, expiring, expired, missing, score),
            State = state?.ToString(),
            Type = type
        });
    }

    public async Task<IActionResult> Create(int? propertyId = null, ComplianceType? type = null)
    {
        if (!IsStaff) return AccessDenied();

        var model = new ComplianceFormModel
        {
            PropertyId = propertyId ?? 0,
            Type = type ?? ComplianceType.GasSafety,
            IssuedOn = DateOnly.FromDateTime(DateTime.Today),
            ExpiresOn = DefaultExpiry(type ?? ComplianceType.GasSafety)
        };

        await PopulatePropertiesAsync(model.PropertyId);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplianceFormModel model)
    {
        if (!IsStaff) return AccessDenied();

        if (model.ExpiresOn <= model.IssuedOn)
        {
            ModelState.AddModelError(nameof(model.ExpiresOn), "The expiry date must be after the issue date.");
        }

        if (!ModelState.IsValid)
        {
            await PopulatePropertiesAsync(model.PropertyId);
            return View("Form", model);
        }

        var cert = new ComplianceCertificate
        {
            PropertyId = model.PropertyId,
            Type = model.Type,
            Reference = model.Reference.Trim(),
            IssuedOn = model.IssuedOn,
            ExpiresOn = model.ExpiresOn,
            Provider = model.Provider,
            Notes = model.Notes
        };

        Db.ComplianceCertificates.Add(cert);
        await Db.SaveChangesAsync();
        Toast($"{ComplianceService.TypeLabel(cert.Type)} certificate added.");

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Renew(int propertyId, ComplianceType type)
    {
        if (!IsStaff) return AccessDenied();

        var latest = await Db.ComplianceCertificates.AsNoTracking()
            .Where(c => c.PropertyId == propertyId && c.Type == type)
            .OrderByDescending(c => c.ExpiresOn)
            .FirstOrDefaultAsync();

        var model = new ComplianceFormModel
        {
            PropertyId = propertyId,
            Type = type,
            IssuedOn = DateOnly.FromDateTime(DateTime.Today),
            ExpiresOn = DefaultExpiry(type),
            Provider = latest?.Provider
        };

        await PopulatePropertiesAsync(propertyId);
        ViewData["Title"] = $"Renew {ComplianceService.TypeLabel(type)}";
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleRenewal(int id, int contractorId, DateOnly? bookedFor)
    {
        if (!IsStaff) return AccessDenied();

        var cert = await Db.ComplianceCertificates.FirstOrDefaultAsync(c => c.Id == id);
        if (cert is null) return NotFound();

        cert.RenewalContractorId = contractorId == 0 ? null : contractorId;
        cert.RenewalStatus = contractorId == 0
            ? RenewalStatus.None
            : bookedFor is null ? RenewalStatus.Assigned : RenewalStatus.Booked;
        cert.RenewalBookedFor = bookedFor;

        await Db.SaveChangesAsync();
        Toast(cert.RenewalStatus == RenewalStatus.Booked
            ? $"Renewal booked for {bookedFor:d MMM yyyy}."
            : "Contractor assigned to the renewal.", "info");
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulatePropertiesAsync(int selected)
    {
        var properties = await Db.Properties.AsNoTracking()
            .OrderBy(p => p.Reference)
            .Select(p => new { p.Id, Name = p.Reference + " — " + p.AddressLine1 + ", " + p.City })
            .ToListAsync();
        ViewBag.Properties = new SelectList(properties, "Id", "Name", selected);
    }

    private static DateOnly DefaultExpiry(ComplianceType type) => type switch
    {
        ComplianceType.Epc => DateOnly.FromDateTime(DateTime.Today).AddYears(10),
        ComplianceType.Eicr => DateOnly.FromDateTime(DateTime.Today).AddYears(5),
        ComplianceType.HmoLicence => DateOnly.FromDateTime(DateTime.Today).AddYears(5),
        _ => DateOnly.FromDateTime(DateTime.Today).AddYears(1)
    };
}
