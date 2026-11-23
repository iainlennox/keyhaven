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
public class ContactsController(KeyhavenDbContext db, CurrentUser current) : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(ContactType? type, string? q)
    {
        if (!IsStaff) return AccessDenied();

        var query = Db.ContactNotes.AsNoTracking().Include(n => n.Property).AsQueryable();
        if (type is not null) query = query.Where(n => n.ContactType == type);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(n => n.ContactName.ToLower().Contains(term)
                                     || (n.ContactEmail != null && n.ContactEmail.ToLower().Contains(term))
                                     || n.Summary.ToLower().Contains(term));
        }

        var notes = await query.OrderByDescending(n => n.OccurredAt).Take(100).ToListAsync();
        var followUps = await Db.ContactNotes.AsNoTracking()
            .Include(n => n.Property)
            .Where(n => n.IsFollowUp && n.FollowUpOn != null)
            .OrderBy(n => n.FollowUpOn)
            .Take(10)
            .ToListAsync();

        await PopulatePropertiesAsync();

        return View(new ContactsIndexViewModel
        {
            Notes = notes,
            FollowUps = followUps,
            Type = type,
            Query = q,
            NoteCount = await Db.ContactNotes.CountAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContactFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (!ModelState.IsValid)
        {
            Toast("Add a contact name and a note summary.", "danger");
            return RedirectToAction(nameof(Index));
        }

        Db.ContactNotes.Add(new ContactNote
        {
            ContactType = model.ContactType,
            ContactName = model.ContactName.Trim(),
            ContactEmail = model.ContactEmail?.Trim(),
            Channel = model.Channel,
            Summary = model.Summary.Trim(),
            OccurredAt = DateTime.Now,
            CreatedBy = Current.Name,
            PropertyId = model.PropertyId,
            IsFollowUp = model.IsFollowUp,
            FollowUpOn = model.IsFollowUp ? model.FollowUpOn : null
        });

        await Db.SaveChangesAsync();
        Toast("Contact note added to the timeline.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteFollowUp(int id)
    {
        if (!IsStaff) return AccessDenied();

        var note = await Db.ContactNotes.FirstOrDefaultAsync(n => n.Id == id);
        if (note is null) return NotFound();

        note.IsFollowUp = false;
        note.FollowUpOn = null;
        await Db.SaveChangesAsync();
        Toast("Follow-up completed.");
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulatePropertiesAsync()
    {
        var properties = await Db.Properties.AsNoTracking()
            .OrderBy(p => p.Reference)
            .Select(p => new { p.Id, Name = p.Reference + " — " + p.AddressLine1 + ", " + p.City })
            .ToListAsync();
        ViewBag.Properties = new SelectList(properties, "Id", "Name");
    }
}
