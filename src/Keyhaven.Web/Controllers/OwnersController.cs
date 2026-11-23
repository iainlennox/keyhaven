using Keyhaven.Web.Data;
using Keyhaven.Web.Helpers;
using Keyhaven.Web.Models;
using Keyhaven.Web.Services;
using Keyhaven.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Controllers;

[Authorize]
public class OwnersController(KeyhavenDbContext db, CurrentUser current, ComplianceService compliance, ArrearsService arrears)
    : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(string? q, OwnerStatus? status)
    {
        if (!IsStaff) return AccessDenied();

        var query = Db.Owners.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(o => o.DisplayName.ToLower().Contains(term)
                                     || (o.CompanyName != null && o.CompanyName.ToLower().Contains(term))
                                     || o.Email.ToLower().Contains(term));
        }

        var owners = await query.OrderBy(o => o.DisplayName).ToListAsync();
        var properties = await Db.Properties.AsNoTracking().ToListAsync();
        var tenancies = await Db.Tenancies.AsNoTracking()
            .Where(t => t.Status == TenancyStatus.Active)
            .ToListAsync();
        var charges = await Db.RentCharges.AsNoTracking()
            .Where(c => c.AmountPaid < c.Amount)
            .ToListAsync();

        var rows = owners.Select(o =>
        {
            var ownerProperties = properties.Where(p => p.OwnerId == o.Id).ToList();
            var propertyIds = ownerProperties.Select(p => p.Id).ToHashSet();
            var ownerTenancies = tenancies.Where(t => propertyIds.Contains(t.PropertyId)).ToList();
            var tenancyIds = ownerTenancies.Select(t => t.Id).ToHashSet();
            var ownerArrears = charges.Where(c => tenancyIds.Contains(c.TenancyId)).Sum(c => c.Amount - c.AmountPaid);

            return new OwnerListRow(o, ownerProperties.Count, ownerTenancies.Count, ownerTenancies.Sum(t => t.RentAmount), ownerArrears);
        }).ToList();

        return View(new OwnersIndexViewModel
        {
            Rows = rows,
            Status = status,
            Query = q,
            ActiveCount = rows.Count(r => r.Owner.Status == OwnerStatus.Active),
            OnboardingCount = rows.Count(r => r.Owner.Status == OwnerStatus.Onboarding),
            PropertyCount = properties.Count,
            RentRoll = rows.Sum(r => r.RentRoll)
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!IsStaff) return AccessDenied();

        var owner = await Db.Owners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (owner is null) return NotFound();

        var properties = await Db.Properties.AsNoTracking()
            .Where(p => p.OwnerId == id)
            .OrderBy(p => p.Reference)
            .ToListAsync();

        var tenancies = await Db.Tenancies.AsNoTracking()
            .Where(t => t.Property!.OwnerId == id && t.Status == TenancyStatus.Active)
            .ToListAsync();

        var rows = await compliance.GetRowsAsync(ownerId: id);
        var arrearsSummary = await arrears.GetSummaryAsync(id);

        var notes = await Db.ContactNotes.AsNoTracking()
            .Where(n => n.ContactEmail == owner.Email)
            .OrderByDescending(n => n.OccurredAt)
            .Take(8)
            .ToListAsync();

        return View(new OwnerDetailsViewModel
        {
            Owner = owner,
            Properties = properties,
            Compliance = rows,
            Notes = notes,
            RentRoll = tenancies.Sum(t => t.RentAmount),
            Arrears = arrearsSummary.Total,
            ActiveTenancies = tenancies.Count
        });
    }

    public IActionResult Create()
    {
        if (!IsStaff) return AccessDenied();
        return View("Form", new OwnerFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OwnerFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (!ModelState.IsValid) return View("Form", model);

        var owner = new Owner
        {
            DisplayName = model.DisplayName.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Email = model.Email.Trim(),
            Phone = model.Phone,
            Address = model.Address,
            OnboardedOn = DateOnly.FromDateTime(DateTime.Today),
            Status = model.Status,
            ManagementFeePercent = model.ManagementFeePercent,
            Notes = model.Notes
        };

        Db.Owners.Add(owner);
        await Db.SaveChangesAsync();
        Toast($"{owner.DisplayName} added as an owner client.");
        return RedirectToAction(nameof(Details), new { id = owner.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsStaff) return AccessDenied();

        var owner = await Db.Owners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (owner is null) return NotFound();

        return View("Form", new OwnerFormModel
        {
            Id = owner.Id,
            DisplayName = owner.DisplayName,
            CompanyName = owner.CompanyName,
            Email = owner.Email,
            Phone = owner.Phone,
            Address = owner.Address,
            Status = owner.Status,
            ManagementFeePercent = owner.ManagementFeePercent,
            Notes = owner.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OwnerFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (id != model.Id) return BadRequest();

        var owner = await Db.Owners.FirstOrDefaultAsync(o => o.Id == id);
        if (owner is null) return NotFound();
        if (!ModelState.IsValid) return View("Form", model);

        owner.DisplayName = model.DisplayName.Trim();
        owner.CompanyName = model.CompanyName?.Trim();
        owner.Email = model.Email.Trim();
        owner.Phone = model.Phone;
        owner.Address = model.Address;
        owner.Status = model.Status;
        owner.ManagementFeePercent = model.ManagementFeePercent;
        owner.Notes = model.Notes;

        await Db.SaveChangesAsync();
        Toast($"{owner.DisplayName} updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignAgreement(int id)
    {
        if (!IsStaff) return AccessDenied();

        var owner = await Db.Owners.FirstOrDefaultAsync(o => o.Id == id);
        if (owner is null) return NotFound();

        owner.AgreementSigned = true;
        owner.AgreementSignedOn = DateOnly.FromDateTime(DateTime.Today);
        if (owner.Status == OwnerStatus.Prospect)
        {
            owner.Status = OwnerStatus.Onboarding;
        }

        await Db.SaveChangesAsync();
        Toast($"Management agreement recorded for {owner.DisplayName}.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvanceStatus(int id)
    {
        if (!IsStaff) return AccessDenied();

        var owner = await Db.Owners.FirstOrDefaultAsync(o => o.Id == id);
        if (owner is null) return NotFound();

        owner.Status = owner.Status switch
        {
            OwnerStatus.Prospect => OwnerStatus.Onboarding,
            OwnerStatus.Onboarding => OwnerStatus.Active,
            OwnerStatus.Active => OwnerStatus.Active,
            _ => OwnerStatus.Active
        };

        if (owner.Status == OwnerStatus.Active && !owner.AgreementSigned)
        {
            owner.AgreementSigned = true;
            owner.AgreementSignedOn = DateOnly.FromDateTime(DateTime.Today);
        }

        await Db.SaveChangesAsync();
        Toast($"{owner.DisplayName} moved to {owner.Status.Label().ToLowerInvariant()}.", "info");
        return RedirectToAction(nameof(Details), new { id });
    }
}
