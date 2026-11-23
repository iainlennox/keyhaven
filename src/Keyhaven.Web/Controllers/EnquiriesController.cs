using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Keyhaven.Web.Services;
using Keyhaven.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Controllers;

[Authorize]
public class EnquiriesController(KeyhavenDbContext db, CurrentUser current) : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(string filter = "open")
    {
        if (!IsStaff) return AccessDenied();

        var query = Db.Enquiries.AsNoTracking().Include(e => e.Property).AsQueryable();
        query = filter switch
        {
            "converted" => query.Where(e => e.Status == EnquiryStatus.Converted),
            "all" => query,
            _ => query.Where(e => e.Status == EnquiryStatus.New || e.Status == EnquiryStatus.Replied)
        };

        return View(new EnquiriesIndexViewModel
        {
            Enquiries = await query.OrderByDescending(e => e.ReceivedAt).Take(100).ToListAsync(),
            Filter = filter
        });
    }

    public async Task<IActionResult> Convert(int id)
    {
        if (!IsStaff) return AccessDenied();

        var enquiry = await Db.Enquiries.AsNoTracking().Include(e => e.Property).FirstOrDefaultAsync(e => e.Id == id);
        if (enquiry is null) return NotFound();
        if (enquiry.PropertyId is null || enquiry.Property is null)
        {
            Toast("This enquiry isn't linked to a property, so it can't be converted to a viewing.", "danger");
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Enquiry = enquiry;
        return View(new ConvertEnquiryModel { EnquiryId = enquiry.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(ConvertEnquiryModel model)
    {
        if (!IsStaff) return AccessDenied();

        var enquiry = await Db.Enquiries.Include(e => e.Property).FirstOrDefaultAsync(e => e.Id == model.EnquiryId);
        if (enquiry?.Property is null) return NotFound();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var viewing = new Viewing
        {
            PropertyId = enquiry.PropertyId!.Value,
            ApplicantName = enquiry.Name,
            ApplicantEmail = enquiry.Email,
            ApplicantPhone = enquiry.Phone,
            ScheduledAt = model.ScheduledAt,
            Status = ViewingStatus.Confirmed,
            PrescreenStatus = PrescreenStatus.Invited,
            AgentNotes = model.AgentNotes,
            CreatedAt = DateTime.Now
        };

        Db.Viewings.Add(viewing);
        await Db.SaveChangesAsync();

        enquiry.Status = EnquiryStatus.Converted;
        enquiry.ConvertedViewingId = viewing.Id;

        Db.ContactNotes.Add(new ContactNote
        {
            ContactType = ContactType.Applicant,
            ContactName = enquiry.Name,
            ContactEmail = enquiry.Email,
            Channel = ContactChannel.Email,
            Summary = $"Enquiry converted to a viewing at {enquiry.Property.ShortAddress} on {viewing.ScheduledAt:d MMM yyyy} at {viewing.ScheduledAt:h:mmtt}.",
            OccurredAt = DateTime.Now,
            CreatedBy = Current.Name,
            PropertyId = enquiry.PropertyId
        });

        await Db.SaveChangesAsync();
        Toast($"Viewing booked for {enquiry.Name} on {viewing.ScheduledAt:d MMM} at {viewing.ScheduledAt:h:mmtt}.");
        return RedirectToAction("Index", "Viewings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReplied(int id)
    {
        if (!IsStaff) return AccessDenied();

        var enquiry = await Db.Enquiries.FirstOrDefaultAsync(e => e.Id == id);
        if (enquiry is null) return NotFound();

        enquiry.Status = EnquiryStatus.Replied;
        await Db.SaveChangesAsync();
        Toast("Enquiry marked as replied.", "info");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        if (!IsStaff) return AccessDenied();

        var enquiry = await Db.Enquiries.FirstOrDefaultAsync(e => e.Id == id);
        if (enquiry is null) return NotFound();

        enquiry.Status = EnquiryStatus.Closed;
        await Db.SaveChangesAsync();
        Toast("Enquiry closed.", "info");
        return RedirectToAction(nameof(Index));
    }
}
