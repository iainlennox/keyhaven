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
public class ViewingsController(KeyhavenDbContext db, CurrentUser current) : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index()
    {
        if (!IsStaff) return AccessDenied();

        var now = DateTime.Now;
        var all = await Db.Viewings.AsNoTracking().Include(v => v.Property).ToListAsync();

        return View(new ViewingsIndexViewModel
        {
            Upcoming = all
                .Where(v => v.ScheduledAt >= now && v.Status is ViewingStatus.Requested or ViewingStatus.Confirmed)
                .OrderBy(v => v.ScheduledAt)
                .ToList(),
            Past = all
                .Where(v => v.ScheduledAt < now || v.Status is ViewingStatus.Completed or ViewingStatus.NoShow or ViewingStatus.Cancelled)
                .OrderByDescending(v => v.ScheduledAt)
                .Take(15)
                .ToList(),
            WeekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (DateTime.Today.DayOfWeek == DayOfWeek.Sunday ? -6 : 1))
        });
    }

    public async Task<IActionResult> Create(int? propertyId = null)
    {
        if (!IsStaff) return AccessDenied();
        await PopulatePropertiesAsync(propertyId ?? 0);
        return View("Form", new ViewingFormModel { PropertyId = propertyId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ViewingFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (!ModelState.IsValid)
        {
            await PopulatePropertiesAsync(model.PropertyId);
            return View("Form", model);
        }

        var viewing = new Viewing
        {
            PropertyId = model.PropertyId,
            ApplicantName = model.ApplicantName.Trim(),
            ApplicantEmail = model.ApplicantEmail.Trim(),
            ApplicantPhone = model.ApplicantPhone,
            ScheduledAt = model.ScheduledAt,
            DurationMinutes = model.DurationMinutes,
            Status = model.Status,
            PrescreenStatus = model.PrescreenStatus,
            AgentNotes = model.AgentNotes,
            CreatedAt = DateTime.Now
        };

        Db.Viewings.Add(viewing);
        Db.ContactNotes.Add(new ContactNote
        {
            ContactType = ContactType.Applicant,
            ContactName = viewing.ApplicantName,
            ContactEmail = viewing.ApplicantEmail,
            Channel = ContactChannel.Email,
            Summary = $"Viewing booked for {viewing.ScheduledAt:d MMM yyyy} at {viewing.ScheduledAt:h:mmtt}.",
            OccurredAt = DateTime.Now,
            CreatedBy = Current.Name,
            PropertyId = viewing.PropertyId
        });

        await Db.SaveChangesAsync();
        Toast($"Viewing booked for {viewing.ApplicantName} on {viewing.ScheduledAt:d MMM} at {viewing.ScheduledAt:h:mmtt}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        if (!IsStaff) return AccessDenied();
        return await SetStatusAsync(id, ViewingStatus.Confirmed, "Viewing confirmed.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!IsStaff) return AccessDenied();
        return await SetStatusAsync(id, ViewingStatus.Cancelled, "Viewing cancelled.", "warning");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NoShow(int id)
    {
        if (!IsStaff) return AccessDenied();
        return await SetStatusAsync(id, ViewingStatus.NoShow, "Marked as a no show.", "warning");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InvitePrescreen(int id)
    {
        if (!IsStaff) return AccessDenied();

        var viewing = await Db.Viewings.FirstOrDefaultAsync(v => v.Id == id);
        if (viewing is null) return NotFound();

        viewing.PrescreenStatus = PrescreenStatus.Invited;
        await Db.SaveChangesAsync();
        Toast($"Pre-screening invite prepared for {viewing.ApplicantName}.", "info");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? feedback)
    {
        if (!IsStaff) return AccessDenied();

        var viewing = await Db.Viewings.Include(v => v.Property).FirstOrDefaultAsync(v => v.Id == id);
        if (viewing is null) return NotFound();

        viewing.Status = ViewingStatus.Completed;
        viewing.Feedback = feedback;

        if (!string.IsNullOrWhiteSpace(feedback))
        {
            Db.ContactNotes.Add(new ContactNote
            {
                ContactType = ContactType.Applicant,
                ContactName = viewing.ApplicantName,
                ContactEmail = viewing.ApplicantEmail,
                Channel = ContactChannel.Email,
                Summary = $"Viewing feedback: {feedback}",
                OccurredAt = DateTime.Now,
                CreatedBy = Current.Name,
                PropertyId = viewing.PropertyId
            });
        }

        await Db.SaveChangesAsync();
        Toast("Viewing completed and feedback recorded.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SetStatusAsync(int id, ViewingStatus status, string message, string tone = "positive")
    {
        var viewing = await Db.Viewings.FirstOrDefaultAsync(v => v.Id == id);
        if (viewing is null) return NotFound();

        viewing.Status = status;
        await Db.SaveChangesAsync();
        Toast(message, tone);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulatePropertiesAsync(int selected)
    {
        var properties = await Db.Properties.AsNoTracking()
            .Where(p => p.Status == PropertyStatus.Available || p.Status == PropertyStatus.UnderOffer)
            .OrderBy(p => p.Reference)
            .Select(p => new { p.Id, Name = p.Reference + " — " + p.AddressLine1 + ", " + p.City })
            .ToListAsync();
        ViewBag.Properties = new SelectList(properties, "Id", "Name", selected);
    }
}
