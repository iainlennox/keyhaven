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
public class InboxController(KeyhavenDbContext db, CurrentUser current, AiInboxService ai)
    : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(string filter = "attention")
    {
        if (!IsStaff) return AccessDenied();

        var query = Db.InboxMessages.AsNoTracking().AsQueryable();
        query = filter switch
        {
            "drafts" => query.Where(m => m.Status == InboxStatus.DraftReady),
            "auto" => query.Where(m => m.Status == InboxStatus.AutoReplied),
            "handled" => query.Where(m => m.Status == InboxStatus.Replied || m.Status == InboxStatus.AutoReplied),
            "all" => query,
            _ => query.Where(m => m.Status == InboxStatus.New || m.Status == InboxStatus.DraftReady || m.Status == InboxStatus.NeedsHuman)
        };

        var messages = await query.OrderByDescending(m => m.ReceivedAt).Take(100).ToListAsync();
        var today = DateTime.Today;

        return View(new InboxIndexViewModel
        {
            Messages = messages,
            Filter = filter,
            NeedsAttention = await Db.InboxMessages.CountAsync(m =>
                m.Status == InboxStatus.New || m.Status == InboxStatus.DraftReady || m.Status == InboxStatus.NeedsHuman),
            Drafts = await Db.InboxMessages.CountAsync(m => m.Status == InboxStatus.DraftReady),
            AutoReplied = await Db.InboxMessages.CountAsync(m => m.Status == InboxStatus.AutoReplied),
            HandledToday = await Db.InboxMessages.CountAsync(m => m.HandledAt >= today)
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!IsStaff) return AccessDenied();

        var message = await Db.InboxMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (message is null) return NotFound();

        return View(new InboxDetailsViewModel
        {
            Message = message,
            Property = message.LinkedPropertyId is null ? null : await Db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == message.LinkedPropertyId),
            Tenancy = message.LinkedTenancyId is null ? null : await Db.Tenancies.AsNoTracking().Include(t => t.Tenant).FirstOrDefaultAsync(t => t.Id == message.LinkedTenancyId),
            Ticket = message.LinkedTicketId is null ? null : await Db.MaintenanceTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == message.LinkedTicketId),
            Owner = message.LinkedOwnerId is null ? null : await Db.Owners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == message.LinkedOwnerId),
            Queue = await Db.InboxMessages.AsNoTracking()
                .Where(m => m.Id != id && (m.Status == InboxStatus.DraftReady || m.Status == InboxStatus.NeedsHuman || m.Status == InboxStatus.New))
                .OrderBy(m => m.ReceivedAt)
                .Take(6)
                .ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyse(int id)
    {
        if (!IsStaff) return AccessDenied();

        var message = await Db.InboxMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (message is null) return NotFound();

        message.AutomationMode = AutomationMode.Pending;
        message.Status = InboxStatus.New;
        await ai.AnalyseAsync(message);
        await Db.SaveChangesAsync();

        Toast(message.AutomationMode switch
        {
            AutomationMode.AutoSent => "Reply sent automatically — the record supported a routine answer.",
            AutomationMode.Drafted => "Draft prepared for review, grounded in the linked records.",
            _ => "No supporting record was found, so this needs a person.",
        }, message.AutomationMode == AutomationMode.Escalated ? "warning" : "info");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int id, string? reply)
    {
        if (!IsStaff) return AccessDenied();

        var message = await Db.InboxMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (message is null) return NotFound();

        var text = string.IsNullOrWhiteSpace(reply) ? message.DraftReply : reply;
        if (string.IsNullOrWhiteSpace(text))
        {
            Toast("There's nothing to send yet. Write a reply or run the assistant first.", "danger");
            return RedirectToAction(nameof(Details), new { id });
        }

        await ai.SendReplyAsync(message, text, Current.Name);
        Toast($"Reply sent to {message.FromName}.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Escalate(int id)
    {
        if (!IsStaff) return AccessDenied();

        var message = await Db.InboxMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (message is null) return NotFound();

        message.AutomationMode = AutomationMode.Escalated;
        message.Status = InboxStatus.NeedsHuman;
        await Db.SaveChangesAsync();

        Toast("Message left for a person to handle.", "warning");
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Compose()
    {
        if (!IsStaff) return AccessDenied();
        await PopulatePropertiesAsync(null);
        return View(new ComposeInboxModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Compose(ComposeInboxModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (!ModelState.IsValid)
        {
            await PopulatePropertiesAsync(model.PropertyId);
            return View(model);
        }

        var message = new InboxMessage
        {
            FromName = model.FromName.Trim(),
            FromEmail = model.FromEmail.Trim(),
            Subject = model.Subject.Trim(),
            Body = model.Body.Trim(),
            ReceivedAt = DateTime.Now,
            LinkedPropertyId = model.PropertyId
        };

        Db.InboxMessages.Add(message);
        await ai.AnalyseAsync(message);
        await Db.SaveChangesAsync();

        Toast("Inbound email triaged.", "info");
        return RedirectToAction(nameof(Details), new { id = message.Id });
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
