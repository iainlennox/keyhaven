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
public class KeysController(KeyhavenDbContext db, CurrentUser current) : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(string filter = "all")
    {
        if (!IsStaff) return AccessDenied();

        var all = await Db.KeyItems.AsNoTracking()
            .Include(k => k.Property)
            .OrderBy(k => k.Property!.Reference)
            .ThenBy(k => k.Label)
            .ToListAsync();

        var now = DateTime.Now;
        IEnumerable<KeyItem> filtered = filter switch
        {
            "out" => all.Where(k => k.IsCheckedOut),
            "overdue" => all.Where(k => k.IsCheckedOut && k.DueBackAt < now),
            "in" => all.Where(k => !k.IsCheckedOut),
            _ => all
        };

        return View(new KeysIndexViewModel
        {
            Keys = filtered.ToList(),
            Filter = filter,
            Total = all.Count,
            CheckedOut = all.Count(k => k.IsCheckedOut),
            Overdue = all.Count(k => k.IsCheckedOut && k.DueBackAt < now)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int id, string to, string? reason, DateOnly? dueBackOn)
    {
        if (!IsStaff) return AccessDenied();

        var key = await Db.KeyItems.FirstOrDefaultAsync(k => k.Id == id);
        if (key is null) return NotFound();
        if (key.IsCheckedOut)
        {
            Toast($"{key.Label} is already checked out.", "warning");
            return RedirectToAction(nameof(Index));
        }

        key.IsCheckedOut = true;
        key.CheckedOutToName = to;
        key.CheckedOutAt = DateTime.Now;
        key.CheckOutReason = reason;
        key.DueBackAt = dueBackOn?.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(18)));
        key.CheckedInAt = null;

        await Db.SaveChangesAsync();
        Toast($"{key.Label} checked out to {to}.", "info");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkin(int id)
    {
        if (!IsStaff) return AccessDenied();

        var key = await Db.KeyItems.FirstOrDefaultAsync(k => k.Id == id);
        if (key is null) return NotFound();

        key.IsCheckedOut = false;
        key.CheckedInAt = DateTime.Now;

        await Db.SaveChangesAsync();
        Toast($"{key.Label} checked back in.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Create()
    {
        if (!IsStaff) return AccessDenied();
        await PopulatePropertiesAsync(null);
        return View("Form", new KeyFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KeyFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (!ModelState.IsValid)
        {
            await PopulatePropertiesAsync(model.PropertyId);
            return View("Form", model);
        }

        var key = new KeyItem
        {
            Label = model.Label.Trim(),
            Type = model.Type,
            Serial = model.Serial,
            PropertyId = model.PropertyId,
            Notes = model.Notes
        };

        Db.KeyItems.Add(key);
        await Db.SaveChangesAsync();
        Toast($"{key.Label} added to the key vault.");
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulatePropertiesAsync(int? selected)
    {
        var properties = await Db.Properties.AsNoTracking()
            .OrderBy(p => p.Reference)
            .Select(p => new { p.Id, Name = p.Reference + " — " + p.AddressLine1 + ", " + p.City })
            .ToListAsync();
        ViewBag.Properties = new SelectList(properties, "Id", "Name", selected);
    }
}
