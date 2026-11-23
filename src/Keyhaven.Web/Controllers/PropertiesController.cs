using Keyhaven.Web.Data;
using Keyhaven.Web.Helpers;
using Keyhaven.Web.Models;
using Keyhaven.Web.Services;
using Keyhaven.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Controllers;

[Authorize]
public class PropertiesController(KeyhavenDbContext db, CurrentUser current, ComplianceService compliance)
    : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(PropertyStatus? status, string? q)
    {
        var query = Db.Properties.AsNoTracking().Include(p => p.Owner).AsQueryable();

        if (IsLandlord)
        {
            query = query.Where(p => p.OwnerId == Current.LinkedId);
        }
        else if (IsTenant)
        {
            query = query.Where(p => p.Tenancies.Any(t => t.TenantId == Current.LinkedId && t.Status == TenancyStatus.Active));
        }
        else if (IsContractor)
        {
            query = query.Where(p => p.MaintenanceTickets.Any(t => t.AssignedContractorId == Current.LinkedId));
        }

        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Reference.ToLower().Contains(term) ||
                p.AddressLine1.ToLower().Contains(term) ||
                p.City.ToLower().Contains(term) ||
                p.Postcode.ToLower().Contains(term) ||
                (p.Owner != null && p.Owner.DisplayName.ToLower().Contains(term)));
        }

        var properties = await query.OrderBy(p => p.Reference).ToListAsync();

        var propertyIds = properties.Select(p => p.Id).ToList();
        var tenancies = await Db.Tenancies.AsNoTracking().Include(t => t.Tenant)
            .Where(t => propertyIds.Contains(t.PropertyId) && t.Status == TenancyStatus.Active)
            .ToListAsync();

        var ticketCounts = await Db.MaintenanceTickets.AsNoTracking()
            .Where(t => propertyIds.Contains(t.PropertyId)
                        && t.Status != MaintenanceStatus.Completed
                        && t.Status != MaintenanceStatus.Closed)
            .GroupBy(t => t.PropertyId)
            .Select(g => new { PropertyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PropertyId, x => x.Count);

        var complianceRows = await compliance.GetRowsAsync(propertyId: null, ct: HttpContext.RequestAborted);
        var worst = complianceRows
            .Where(r => propertyIds.Contains(r.Property.Id))
            .GroupBy(r => r.Property.Id)
            .ToDictionary(g => g.Key, g => g.Select(r => r.State)
                .OrderByDescending(StateRank)
                .First());

        var rows = properties.Select(p => new PropertyListRow(
            p,
            tenancies.FirstOrDefault(t => t.PropertyId == p.Id),
            ticketCounts.GetValueOrDefault(p.Id),
            worst.GetValueOrDefault(p.Id, ComplianceState.Valid))).ToList();

        var model = new PropertiesIndexViewModel
        {
            Rows = rows,
            Status = status?.ToString(),
            Query = q,
            TotalProperties = rows.Count,
            LetProperties = rows.Count(r => r.Property.Status == PropertyStatus.Let),
            AvailableProperties = rows.Count(r => r.Property.Status == PropertyStatus.Available),
            RentRoll = rows.Where(r => r.ActiveTenancy is not null).Sum(r => r.ActiveTenancy!.RentAmount)
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!await CanViewPropertyAsync(id))
        {
            return AccessDenied();
        }

        var property = await Db.Properties.AsNoTracking()
            .Include(p => p.Owner)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
        {
            return NotFound();
        }

        var tenancies = await Db.Tenancies.AsNoTracking()
            .Include(t => t.Tenant)
            .Where(t => t.PropertyId == id)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();

        var active = tenancies.FirstOrDefault(t => t.Status == TenancyStatus.Active);
        var complianceRows = await compliance.GetRowsAsync(propertyId: id);
        var valid = complianceRows.Count(r => r.State is ComplianceState.Valid or ComplianceState.ExpiringSoon);
        var score = complianceRows.Count == 0 ? 100 : (int)Math.Round((decimal)valid / complianceRows.Count * 100);

        var charges = active is null
            ? []
            : await Db.RentCharges.AsNoTracking()
                .Where(c => c.TenancyId == active.Id && c.PeriodStart >= DateOnly.FromDateTime(DateTime.Today).AddMonths(-5))
                .OrderByDescending(c => c.PeriodStart)
                .ToListAsync();

        var model = new PropertyDetailsViewModel
        {
            Property = property,
            ActiveTenancy = active,
            Tenancies = tenancies,
            Compliance = complianceRows,
            ComplianceScore = score,
            OpenTickets = await Db.MaintenanceTickets.AsNoTracking()
                .Include(t => t.AssignedContractor)
                .Where(t => t.PropertyId == id && t.Status != MaintenanceStatus.Completed && t.Status != MaintenanceStatus.Closed)
                .OrderBy(t => t.Urgency)
                .ToListAsync(),
            Keys = await Db.KeyItems.AsNoTracking().Where(k => k.PropertyId == id).OrderBy(k => k.Label).ToListAsync(),
            UpcomingViewings = await Db.Viewings.AsNoTracking()
                .Where(v => v.PropertyId == id && v.ScheduledAt >= DateTime.Now && v.Status != ViewingStatus.Cancelled)
                .OrderBy(v => v.ScheduledAt).Take(4).ToListAsync(),
            RecentEnquiries = await Db.Enquiries.AsNoTracking()
                .Where(e => e.PropertyId == id).OrderByDescending(e => e.ReceivedAt).Take(4).ToListAsync(),
            RecentCharges = charges.ToList(),
            Outstanding = charges.Sum(c => c.Outstanding),
            CollectedLastSixMonths = charges.Sum(c => c.AmountPaid)
        };

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        if (!IsStaff) return AccessDenied();

        var nextNumber = await Db.Properties.CountAsync() + 1001;
        var model = new PropertyFormModel
        {
            Reference = $"KH-{nextNumber}",
            ImageUrl = "/img/property-glasgow.svg",
            Channels = "Agency website"
        };
        await PopulateOwnersAsync(model.OwnerId);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (await Db.Properties.AnyAsync(p => p.Reference == model.Reference))
        {
            ModelState.AddModelError(nameof(model.Reference), "That reference is already in use.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateOwnersAsync(model.OwnerId);
            return View("Form", model);
        }

        var property = new Property
        {
            Reference = model.Reference.Trim(),
            AddressLine1 = model.AddressLine1.Trim(),
            AddressLine2 = model.AddressLine2?.Trim(),
            City = model.City.Trim(),
            Postcode = model.Postcode.Trim().ToUpperInvariant(),
            Type = model.Type,
            Bedrooms = model.Bedrooms,
            Bathrooms = model.Bathrooms,
            MonthlyRent = model.MonthlyRent,
            Status = model.Status,
            OwnerId = model.OwnerId,
            ManagerId = Current.Id,
            EpcRating = model.EpcRating,
            Description = model.Description,
            MarketingHeadline = model.MarketingHeadline,
            MarketingSummary = model.MarketingSummary,
            ListingPublished = model.ListingPublished,
            ListedOn = model.ListingPublished ? DateOnly.FromDateTime(DateTime.Today) : null,
            Channels = model.Channels,
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? "/img/property-glasgow.svg" : model.ImageUrl,
            CreatedAt = DateTime.Now
        };

        Db.Properties.Add(property);
        await Db.SaveChangesAsync();
        Toast($"{property.Reference} added to the portfolio.");
        return RedirectToAction(nameof(Details), new { id = property.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsStaff) return AccessDenied();

        var property = await Db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (property is null) return NotFound();

        var model = new PropertyFormModel
        {
            Id = property.Id,
            Reference = property.Reference,
            AddressLine1 = property.AddressLine1,
            AddressLine2 = property.AddressLine2,
            City = property.City,
            Postcode = property.Postcode,
            Type = property.Type,
            Bedrooms = property.Bedrooms,
            Bathrooms = property.Bathrooms,
            MonthlyRent = property.MonthlyRent,
            Status = property.Status,
            OwnerId = property.OwnerId,
            EpcRating = property.EpcRating,
            Description = property.Description,
            MarketingHeadline = property.MarketingHeadline,
            MarketingSummary = property.MarketingSummary,
            ListingPublished = property.ListingPublished,
            Channels = property.Channels,
            ImageUrl = property.ImageUrl
        };

        await PopulateOwnersAsync(model.OwnerId);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (id != model.Id) return BadRequest();

        var property = await Db.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null) return NotFound();

        if (await Db.Properties.AnyAsync(p => p.Reference == model.Reference && p.Id != id))
        {
            ModelState.AddModelError(nameof(model.Reference), "That reference is already in use.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateOwnersAsync(model.OwnerId);
            return View("Form", model);
        }

        if (!property.ListingPublished && model.ListingPublished)
        {
            property.ListedOn = DateOnly.FromDateTime(DateTime.Today);
        }

        property.Reference = model.Reference.Trim();
        property.AddressLine1 = model.AddressLine1.Trim();
        property.AddressLine2 = model.AddressLine2?.Trim();
        property.City = model.City.Trim();
        property.Postcode = model.Postcode.Trim().ToUpperInvariant();
        property.Type = model.Type;
        property.Bedrooms = model.Bedrooms;
        property.Bathrooms = model.Bathrooms;
        property.MonthlyRent = model.MonthlyRent;
        property.Status = model.Status;
        property.OwnerId = model.OwnerId;
        property.EpcRating = model.EpcRating;
        property.Description = model.Description;
        property.MarketingHeadline = model.MarketingHeadline;
        property.MarketingSummary = model.MarketingSummary;
        property.ListingPublished = model.ListingPublished;
        property.Channels = model.Channels;
        property.ImageUrl = model.ImageUrl;

        await Db.SaveChangesAsync();
        Toast($"{property.Reference} updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleListing(int id)
    {
        if (!IsStaff) return AccessDenied();

        var property = await Db.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null) return NotFound();

        property.ListingPublished = !property.ListingPublished;
        property.ListedOn = property.ListingPublished ? DateOnly.FromDateTime(DateTime.Today) : null;
        if (property.ListingPublished && string.IsNullOrWhiteSpace(property.Channels))
        {
            property.Channels = "Rightmove, Find your Home, Agency website";
        }

        await Db.SaveChangesAsync();
        Toast(property.ListingPublished
            ? $"{property.Reference} is now published."
            : $"{property.Reference} has been unpublished.", "info");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, PropertyStatus status)
    {
        if (!IsStaff) return AccessDenied();

        var property = await Db.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null) return NotFound();

        property.Status = status;
        await Db.SaveChangesAsync();
        Toast($"{property.Reference} marked as {status.Label().ToLowerInvariant()}.", "info");
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateOwnersAsync(int selected)
    {
        var owners = await Db.Owners.AsNoTracking()
            .OrderBy(o => o.DisplayName)
            .Select(o => new { o.Id, Name = o.DisplayName + (o.CompanyName != null ? $" — {o.CompanyName}" : "") })
            .ToListAsync();
        ViewBag.Owners = new SelectList(owners, "Id", "Name", selected);
    }

    private static int StateRank(ComplianceState state) => state switch
    {
        ComplianceState.Missing => 4,
        ComplianceState.Expired => 3,
        ComplianceState.ExpiringSoon => 2,
        _ => 1
    };
}
