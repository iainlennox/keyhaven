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
public class MaintenanceController(KeyhavenDbContext db, CurrentUser current) : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(string filter = "open", string? q = null)
    {
        var all = await ScopedTicketsAsync();

        var filtered = filter switch
        {
            "new" => all.Where(t => t.Status == MaintenanceStatus.New),
            "urgent" => all.Where(t => t.Urgency is Urgency.Emergency or Urgency.Urgent
                                       && t.Status != MaintenanceStatus.Completed && t.Status != MaintenanceStatus.Closed),
            "completed" => all.Where(t => t.Status is MaintenanceStatus.Completed or MaintenanceStatus.Closed),
            "all" => all,
            _ => all.Where(t => t.Status != MaintenanceStatus.Completed && t.Status != MaintenanceStatus.Closed)
        };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            filtered = filtered.Where(t =>
                t.Title.ToLower().Contains(term) ||
                t.Reference.ToLower().Contains(term) ||
                t.Property!.Reference.ToLower().Contains(term) ||
                t.Property.AddressLine1.ToLower().Contains(term));
        }

        var today = DateTime.Today;
        return View(new MaintenanceIndexViewModel
        {
            Tickets = filtered.OrderBy(t => t.Urgency).ThenByDescending(t => t.CreatedAt).ToList(),
            Filter = filter,
            Query = q,
            OpenCount = all.Count(t => t.Status != MaintenanceStatus.Completed && t.Status != MaintenanceStatus.Closed),
            NewCount = all.Count(t => t.Status == MaintenanceStatus.New),
            UrgentCount = all.Count(t => t.Urgency is Urgency.Emergency or Urgency.Urgent
                                          && t.Status != MaintenanceStatus.Completed && t.Status != MaintenanceStatus.Closed),
            InProgressCount = all.Count(t => t.Status is MaintenanceStatus.InProgress or MaintenanceStatus.Assigned),
            CompletedThisMonth = all.Count(t => t.CompletedAt is not null && t.CompletedAt >= today.AddDays(-30))
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!await CanViewTicketAsync(id)) return AccessDenied();

        var ticket = await Db.MaintenanceTickets.AsNoTracking()
            .Include(t => t.Property)!.ThenInclude(p => p!.Owner)
            .Include(t => t.Tenant)
            .Include(t => t.AssignedContractor)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null) return NotFound();

        await PopulateContractorsAsync(ticket.AssignedContractorId);

        return View(new MaintenanceDetailsViewModel { Ticket = ticket });
    }

    public async Task<IActionResult> Create(int? propertyId = null)
    {
        if (IsContractor) return AccessDenied();

        var properties = await AvailablePropertiesAsync();
        if (IsTenant && properties.Count == 1)
        {
            propertyId ??= properties[0].Id;
        }

        await PopulatePropertiesAsync(properties);
        return View("Form", new MaintenanceFormModel { PropertyId = propertyId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceFormModel model)
    {
        if (IsContractor) return AccessDenied();

        var allowed = await AvailablePropertiesAsync();
        if (allowed.All(p => p.Id != model.PropertyId))
        {
            return AccessDenied();
        }

        if (!ModelState.IsValid)
        {
            await PopulatePropertiesAsync(allowed);
            return View("Form", model);
        }

        var property = await Db.Properties.FirstOrDefaultAsync(p => p.Id == model.PropertyId);
        if (property is null) return NotFound();

        var triage = MaintenanceTriageService.Triage(model.Title, model.Description, model.Category);
        var urgency = Max(model.Urgency, triage.Urgency);

        var count = await Db.MaintenanceTickets.CountAsync();
        var ticket = new MaintenanceTicket
        {
            Reference = $"JR-{2049 + count + 1}",
            PropertyId = model.PropertyId,
            TenantId = IsTenant ? Current.LinkedId : null,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Category = model.Category == MaintenanceCategory.Other ? triage.Category : model.Category,
            Urgency = urgency,
            Status = MaintenanceStatus.Triaged,
            AiSummary = triage.Summary,
            AiSuggestedUrgency = triage.Urgency,
            AiReason = triage.Reason,
            AccessInstructions = model.AccessInstructions,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        Db.MaintenanceTickets.Add(ticket);
        Db.ContactNotes.Add(new ContactNote
        {
            ContactType = IsTenant ? ContactType.Tenant : ContactType.Other,
            ContactName = Current.Name,
            ContactEmail = Current.Email,
            Channel = ContactChannel.Note,
            Summary = $"Reported repair {ticket.Reference}: {ticket.Title}. AI triage set {ticket.Urgency.Label().ToLowerInvariant()} priority.",
            OccurredAt = DateTime.Now,
            CreatedBy = Current.Name,
            PropertyId = model.PropertyId
        });

        await Db.SaveChangesAsync();
        Toast($"Repair {ticket.Reference} logged and triaged as {ticket.Urgency.Label().ToLowerInvariant()}.");
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Triage(int id)
    {
        if (!IsStaff) return AccessDenied();

        var ticket = await Db.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();

        var triage = MaintenanceTriageService.Triage(ticket.Title, ticket.Description, ticket.Category);
        ticket.Category = triage.Category;
        ticket.Urgency = Max(ticket.Urgency, triage.Urgency);
        ticket.AiSummary = triage.Summary;
        ticket.AiSuggestedUrgency = triage.Urgency;
        ticket.AiReason = triage.Reason;
        ticket.UpdatedAt = DateTime.Now;
        if (ticket.Status == MaintenanceStatus.New) ticket.Status = MaintenanceStatus.Triaged;

        await Db.SaveChangesAsync();
        Toast("AI triage refreshed.", "info");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quote(int id, int contractorId, decimal quoteAmount, string? quoteNotes)
    {
        if (!IsStaff) return AccessDenied();

        var ticket = await Db.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();

        ticket.AssignedContractorId = contractorId;
        ticket.QuoteAmount = quoteAmount;
        ticket.QuoteNotes = quoteNotes;
        ticket.QuoteApproved = null;
        ticket.Status = MaintenanceStatus.AwaitingApproval;
        ticket.UpdatedAt = DateTime.Now;

        await Db.SaveChangesAsync();
        Toast("Quote recorded and sent for approval.", "info");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveQuote(int id)
    {
        if (!IsStaff) return AccessDenied();

        var ticket = await Db.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();

        ticket.QuoteApproved = true;
        ticket.Status = MaintenanceStatus.Assigned;
        ticket.UpdatedAt = DateTime.Now;

        await Db.SaveChangesAsync();
        Toast("Quote approved and the job released to the contractor.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineQuote(int id)
    {
        if (!IsStaff) return AccessDenied();

        var ticket = await Db.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();

        ticket.QuoteApproved = false;
        ticket.Status = MaintenanceStatus.Triaged;
        ticket.UpdatedAt = DateTime.Now;

        await Db.SaveChangesAsync();
        Toast("Quote declined. The job is back with the team.", "warning");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, int contractorId)
    {
        if (!IsStaff) return AccessDenied();

        var ticket = await Db.MaintenanceTickets.Include(t => t.Property).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();

        ticket.AssignedContractorId = contractorId;
        if (ticket.Status is MaintenanceStatus.New or MaintenanceStatus.Triaged or MaintenanceStatus.Quoted)
        {
            ticket.Status = MaintenanceStatus.Assigned;
        }
        ticket.UpdatedAt = DateTime.Now;

        await Db.SaveChangesAsync();
        Toast("Contractor assigned.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var ticket = await Db.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (!CanUpdateJob(ticket)) return AccessDenied();

        ticket.Status = MaintenanceStatus.InProgress;
        ticket.UpdatedAt = DateTime.Now;
        await Db.SaveChangesAsync();
        Toast("Job marked as in progress.", "info");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? completionNotes)
    {
        var ticket = await Db.MaintenanceTickets.Include(t => t.Property).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (!CanUpdateJob(ticket)) return AccessDenied();

        ticket.Status = MaintenanceStatus.Completed;
        ticket.CompletedAt = DateTime.Now;
        ticket.CompletionNotes = completionNotes;
        ticket.UpdatedAt = DateTime.Now;

        Db.ContactNotes.Add(new ContactNote
        {
            ContactType = ContactType.Other,
            ContactName = Current.Name,
            ContactEmail = Current.Email,
            Channel = ContactChannel.Note,
            Summary = $"Repair {ticket.Reference} completed at {ticket.Property?.Reference}. {completionNotes}".Trim(),
            OccurredAt = DateTime.Now,
            CreatedBy = Current.Name,
            PropertyId = ticket.PropertyId
        });

        await Db.SaveChangesAsync();
        Toast($"{ticket.Reference} marked complete.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        if (!IsStaff) return AccessDenied();

        var ticket = await Db.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();

        ticket.Status = MaintenanceStatus.Closed;
        ticket.UpdatedAt = DateTime.Now;
        await Db.SaveChangesAsync();
        Toast("Job closed.", "info");
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<List<MaintenanceTicket>> ScopedTicketsAsync()
    {
        var query = Db.MaintenanceTickets.AsNoTracking()
            .Include(t => t.Property)
            .Include(t => t.Tenant)
            .Include(t => t.AssignedContractor)
            .AsQueryable();

        if (IsLandlord)
        {
            query = query.Where(t => t.Property!.OwnerId == Current.LinkedId);
        }
        else if (IsTenant)
        {
            query = query.Where(t => t.TenantId == Current.LinkedId);
        }
        else if (IsContractor)
        {
            query = query.Where(t => t.AssignedContractorId == Current.LinkedId);
        }

        return await query.ToListAsync();
    }

    private async Task<List<Property>> AvailablePropertiesAsync()
    {
        var query = Db.Properties.AsNoTracking().AsQueryable();

        if (IsTenant)
        {
            query = query.Where(p => p.Tenancies.Any(t => t.TenantId == Current.LinkedId && t.Status == TenancyStatus.Active));
        }
        else if (IsLandlord)
        {
            query = query.Where(p => p.OwnerId == Current.LinkedId);
        }

        return await query.OrderBy(p => p.Reference).ToListAsync();
    }

    private async Task PopulatePropertiesAsync(List<Property> properties)
    {
        ViewBag.Properties = new SelectList(
            properties.Select(p => new { p.Id, Name = $"{p.Reference} — {p.AddressLine1}, {p.City}" }),
            "Id", "Name");
    }

    private async Task PopulateContractorsAsync(int? selected)
    {
        var contractors = await Db.Contractors.AsNoTracking()
            .Where(c => c.Active)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, Name = c.Name + " — " + c.Trade })
            .ToListAsync();
        ViewBag.Contractors = new SelectList(contractors, "Id", "Name", selected);
    }

    private bool CanUpdateJob(MaintenanceTicket ticket)
    {
        if (IsStaff) return true;
        return IsContractor && ticket.AssignedContractorId == Current.LinkedId;
    }

    private static Urgency Max(Urgency a, Urgency b) => (Urgency)Math.Min((int)a, (int)b);
}
