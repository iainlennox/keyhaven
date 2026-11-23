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
public class TenanciesController(KeyhavenDbContext db, CurrentUser current)
    : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(TenancyStatus? status, string? q)
    {
        if (IsContractor) return AccessDenied();

        var query = Db.Tenancies.AsNoTracking()
            .Include(t => t.Property)
            .Include(t => t.Tenant)
            .Include(t => t.RentCharges)
            .AsQueryable();

        if (IsLandlord)
        {
            query = query.Where(t => t.Property!.OwnerId == Current.LinkedId);
        }
        else if (IsTenant)
        {
            query = query.Where(t => t.TenantId == Current.LinkedId);
        }

        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(t =>
                t.Tenant!.FullName.ToLower().Contains(term) ||
                t.Property!.AddressLine1.ToLower().Contains(term) ||
                t.Property.Reference.ToLower().Contains(term) ||
                t.Property.Postcode.ToLower().Contains(term));
        }

        var tenancies = await query
            .OrderByDescending(t => t.Status == TenancyStatus.Active)
            .ThenBy(t => t.Property!.Reference)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = tenancies.Select(t => new TenancyListRow(
            t,
            t.RentCharges.Sum(c => c.Outstanding),
            t.RentCharges.Count(c => c.IsOverdue(today)))).ToList();

        return View(new TenanciesIndexViewModel
        {
            Rows = rows,
            Status = status,
            Query = q,
            ActiveCount = rows.Count(r => r.Tenancy.Status == TenancyStatus.Active),
            EndingCount = rows.Count(r => r.Tenancy.Status == TenancyStatus.Ending),
            RentRoll = rows.Where(r => r.Tenancy.Status == TenancyStatus.Active).Sum(r => r.Tenancy.RentAmount),
            Outstanding = rows.Sum(r => r.Outstanding)
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!await CanViewTenancyAsync(id)) return AccessDenied();

        var tenancy = await Db.Tenancies.AsNoTracking()
            .Include(t => t.Property)!.ThenInclude(p => p!.Owner)
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenancy is null) return NotFound();

        var charges = await Db.RentCharges.AsNoTracking()
            .Where(c => c.TenancyId == id)
            .OrderByDescending(c => c.DueDate)
            .ToListAsync();

        var notes = await Db.ContactNotes.AsNoTracking()
            .Where(n => n.PropertyId == tenancy.PropertyId && n.ContactEmail == tenancy.Tenant!.Email)
            .OrderByDescending(n => n.OccurredAt)
            .Take(6)
            .ToListAsync();

        return View(new TenancyDetailsViewModel
        {
            Tenancy = tenancy,
            Charges = charges,
            Outstanding = charges.Sum(c => c.Outstanding),
            Notes = notes
        });
    }

    public async Task<IActionResult> Create()
    {
        if (!IsStaff) return AccessDenied();
        await PopulateAsync();
        return View("Form", new TenancyFormModel { StartDate = DateOnly.FromDateTime(DateTime.Today).AddDays(14) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenancyFormModel model)
    {
        if (!IsStaff) return AccessDenied();

        var property = await Db.Properties.FirstOrDefaultAsync(p => p.Id == model.PropertyId);
        var tenant = await Db.Tenants.FirstOrDefaultAsync(t => t.Id == model.TenantId);
        if (property is null) ModelState.AddModelError(nameof(model.PropertyId), "Choose a property.");
        if (tenant is null) ModelState.AddModelError(nameof(model.TenantId), "Choose a tenant.");

        if (!ModelState.IsValid)
        {
            await PopulateAsync();
            return View("Form", model);
        }

        var tenancy = new Tenancy
        {
            PropertyId = model.PropertyId,
            TenantId = model.TenantId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            RentAmount = model.RentAmount,
            DepositAmount = model.DepositAmount,
            DepositScheme = model.DepositScheme,
            DepositRegistered = model.DepositRegistered,
            DepositRegisteredOn = model.DepositRegistered ? DateOnly.FromDateTime(DateTime.Today) : null,
            Status = TenancyStatus.Active,
            Notes = model.Notes
        };

        Db.Tenancies.Add(tenancy);
        property!.Status = PropertyStatus.Let;
        await Db.SaveChangesAsync();

        var monthStart = new DateOnly(model.StartDate.Year, model.StartDate.Month, 1);
        Db.RentCharges.Add(new RentCharge
        {
            TenancyId = tenancy.Id,
            PeriodStart = monthStart,
            DueDate = model.StartDate,
            Amount = model.RentAmount,
            BankReference = $"KH{tenancy.Id:D2}{monthStart:yyyyMM}"
        });
        await Db.SaveChangesAsync();

        Toast($"Tenancy created for {tenant!.FullName} at {property.Reference}.");
        return RedirectToAction(nameof(Details), new { id = tenancy.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsStaff) return AccessDenied();

        var tenancy = await Db.Tenancies.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (tenancy is null) return NotFound();

        await PopulateAsync();
        return View("Form", new TenancyFormModel
        {
            Id = tenancy.Id,
            PropertyId = tenancy.PropertyId,
            TenantId = tenancy.TenantId,
            StartDate = tenancy.StartDate,
            EndDate = tenancy.EndDate,
            RentAmount = tenancy.RentAmount,
            DepositAmount = tenancy.DepositAmount,
            DepositScheme = tenancy.DepositScheme,
            DepositRegistered = tenancy.DepositRegistered,
            Notes = tenancy.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TenancyFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (id != model.Id) return BadRequest();

        var tenancy = await Db.Tenancies.FirstOrDefaultAsync(t => t.Id == id);
        if (tenancy is null) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateAsync();
            return View("Form", model);
        }

        tenancy.PropertyId = model.PropertyId;
        tenancy.TenantId = model.TenantId;
        tenancy.StartDate = model.StartDate;
        tenancy.EndDate = model.EndDate;
        tenancy.RentAmount = model.RentAmount;
        tenancy.DepositAmount = model.DepositAmount;
        tenancy.DepositScheme = model.DepositScheme;
        tenancy.DepositRegistered = model.DepositRegistered;
        tenancy.DepositRegisteredOn = model.DepositRegistered
            ? tenancy.DepositRegisteredOn ?? DateOnly.FromDateTime(DateTime.Today)
            : null;
        tenancy.Notes = model.Notes;

        await Db.SaveChangesAsync();
        Toast("Tenancy updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterDeposit(int id, string scheme)
    {
        if (!IsStaff) return AccessDenied();

        var tenancy = await Db.Tenancies.FirstOrDefaultAsync(t => t.Id == id);
        if (tenancy is null) return NotFound();

        tenancy.DepositScheme = string.IsNullOrWhiteSpace(scheme) ? "TDS" : scheme;
        tenancy.DepositRegistered = true;
        tenancy.DepositRegisteredOn = DateOnly.FromDateTime(DateTime.Today);

        await Db.SaveChangesAsync();
        Toast($"Deposit registered with {tenancy.DepositScheme}.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServeNotice(int id, string noticeType)
    {
        if (!IsStaff) return AccessDenied();

        var tenancy = await Db.Tenancies.FirstOrDefaultAsync(t => t.Id == id);
        if (tenancy is null) return NotFound();

        tenancy.NoticeType = string.IsNullOrWhiteSpace(noticeType) ? "Section 21" : noticeType;
        tenancy.NoticeServedOn = DateOnly.FromDateTime(DateTime.Today);
        tenancy.Status = TenancyStatus.Ending;

        await Db.SaveChangesAsync();
        Toast($"{tenancy.NoticeType} recorded. Tenancy marked as ending.", "warning");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndTenancy(int id)
    {
        if (!IsStaff) return AccessDenied();

        var tenancy = await Db.Tenancies.Include(t => t.Property).FirstOrDefaultAsync(t => t.Id == id);
        if (tenancy is null) return NotFound();

        tenancy.Status = TenancyStatus.Ended;
        tenancy.EndDate = DateOnly.FromDateTime(DateTime.Today);

        var stillLet = await Db.Tenancies.AnyAsync(t => t.PropertyId == tenancy.PropertyId && t.Id != id && t.Status == TenancyStatus.Active);
        if (!stillLet && tenancy.Property is not null)
        {
            tenancy.Property.Status = PropertyStatus.Available;
        }

        await Db.SaveChangesAsync();
        Toast("Tenancy ended and the property is available again.", "info");
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateAsync()
    {
        var properties = await Db.Properties.AsNoTracking()
            .Where(p => p.Status != PropertyStatus.Archived)
            .OrderBy(p => p.Reference)
            .Select(p => new { p.Id, Name = p.Reference + " — " + p.AddressLine1 + ", " + p.City + " · £" + p.MonthlyRent.ToString("N0") + " pcm" })
            .ToListAsync();

        var tenants = await Db.Tenants.AsNoTracking()
            .OrderBy(t => t.FullName)
            .Select(t => new { t.Id, Name = t.FullName + " — " + t.Email })
            .ToListAsync();

        ViewBag.Properties = new SelectList(properties, "Id", "Name");
        ViewBag.Tenants = new SelectList(tenants, "Id", "Name");
    }
}
