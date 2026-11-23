using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Keyhaven.Web.Services;
using Keyhaven.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Controllers;

[Authorize]
public class ContractorsController(KeyhavenDbContext db, CurrentUser current) : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index()
    {
        if (!IsStaff) return AccessDenied();

        var contractors = await Db.Contractors.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var tickets = await Db.MaintenanceTickets.AsNoTracking().ToListAsync();

        var rows = contractors.Select(c =>
        {
            var mine = tickets.Where(t => t.AssignedContractorId == c.Id).ToList();
            return new ContractorListRow(
                c,
                mine.Count(t => t.Status != MaintenanceStatus.Completed && t.Status != MaintenanceStatus.Closed),
                mine.Count(t => t.Status is MaintenanceStatus.Completed or MaintenanceStatus.Closed),
                mine.Where(t => t.Status != MaintenanceStatus.Completed && t.Status != MaintenanceStatus.Closed).Sum(t => t.QuoteAmount ?? 0m));
        }).ToList();

        return View(new ContractorsIndexViewModel
        {
            Rows = rows,
            ActiveCount = rows.Count(r => r.Contractor.Active),
            ActiveJobs = rows.Sum(r => r.ActiveJobs),
            QuotedValue = rows.Sum(r => r.QuotedValue)
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!IsStaff) return AccessDenied();

        var contractor = await Db.Contractors.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (contractor is null) return NotFound();

        var tickets = await Db.MaintenanceTickets.AsNoTracking()
            .Include(t => t.Property)
            .Where(t => t.AssignedContractorId == id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(new ContractorDetailsViewModel
        {
            Contractor = contractor,
            ActiveJobs = tickets.Where(t => t.Status != MaintenanceStatus.Completed && t.Status != MaintenanceStatus.Closed).ToList(),
            CompletedJobs = tickets.Where(t => t.Status is MaintenanceStatus.Completed or MaintenanceStatus.Closed).Take(8).ToList()
        });
    }

    public IActionResult Create()
    {
        if (!IsStaff) return AccessDenied();
        return View("Form", new ContractorFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContractorFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (!ModelState.IsValid) return View("Form", model);

        var contractor = new Contractor
        {
            Name = model.Name.Trim(),
            Trade = model.Trade.Trim(),
            Email = model.Email.Trim(),
            Phone = model.Phone,
            Rating = model.Rating,
            Insured = model.Insured,
            Active = model.Active,
            Notes = model.Notes
        };

        Db.Contractors.Add(contractor);
        await Db.SaveChangesAsync();
        Toast($"{contractor.Name} added to the contractor panel.");
        return RedirectToAction(nameof(Details), new { id = contractor.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsStaff) return AccessDenied();

        var contractor = await Db.Contractors.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (contractor is null) return NotFound();

        return View("Form", new ContractorFormModel
        {
            Id = contractor.Id,
            Name = contractor.Name,
            Trade = contractor.Trade,
            Email = contractor.Email,
            Phone = contractor.Phone,
            Rating = contractor.Rating,
            Insured = contractor.Insured,
            Active = contractor.Active,
            Notes = contractor.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ContractorFormModel model)
    {
        if (!IsStaff) return AccessDenied();
        if (id != model.Id) return BadRequest();

        var contractor = await Db.Contractors.FirstOrDefaultAsync(c => c.Id == id);
        if (contractor is null) return NotFound();
        if (!ModelState.IsValid) return View("Form", model);

        contractor.Name = model.Name.Trim();
        contractor.Trade = model.Trade.Trim();
        contractor.Email = model.Email.Trim();
        contractor.Phone = model.Phone;
        contractor.Rating = model.Rating;
        contractor.Insured = model.Insured;
        contractor.Active = model.Active;
        contractor.Notes = model.Notes;

        await Db.SaveChangesAsync();
        Toast($"{contractor.Name} updated.");
        return RedirectToAction(nameof(Details), new { id });
    }
}
