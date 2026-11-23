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
public class RentController(KeyhavenDbContext db, CurrentUser current, ArrearsService arrears)
    : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index(string filter = "all", string? q = null)
    {
        if (IsContractor) return AccessDenied();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var allQuery = Db.RentCharges.AsNoTracking()
            .Include(c => c.Tenancy)!.ThenInclude(t => t!.Property)
            .Include(c => c.Tenancy)!.ThenInclude(t => t!.Tenant)
            .AsQueryable();

        if (IsLandlord)
        {
            allQuery = allQuery.Where(c => c.Tenancy!.Property!.OwnerId == Current.LinkedId);
        }
        else if (IsTenant)
        {
            allQuery = allQuery.Where(c => c.Tenancy!.TenantId == Current.LinkedId);
        }

        var all = await allQuery.ToListAsync();

        var filtered = all.AsQueryable();
        filtered = filter switch
        {
            "overdue" => filtered.Where(c => c.AmountPaid < c.Amount && c.DueDate < today),
            "due" => filtered.Where(c => c.AmountPaid < c.Amount && c.DueDate >= today),
            "paid" => filtered.Where(c => c.AmountPaid >= c.Amount),
            "unreconciled" => filtered.Where(c => c.Reconciliation == RentReconciliation.Unreconciled)
                .OrderByDescending(c => c.DueDate),
            "arrears" => filtered.Where(c => c.AmountPaid < c.Amount).OrderByDescending(c => c.DueDate),
            _ => filtered
        };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            filtered = filtered.Where(c =>
                c.Tenancy!.Tenant!.FullName.ToLower().Contains(term) ||
                c.Tenancy.Property!.Reference.ToLower().Contains(term) ||
                c.Tenancy.Property.AddressLine1.ToLower().Contains(term) ||
                (c.BankReference != null && c.BankReference.ToLower().Contains(term)));
        }

        var current = all.Where(c => c.PeriodStart == monthStart).ToList();

        return View(new RentIndexViewModel
        {
            Charges = filtered.OrderByDescending(c => c.DueDate).ThenBy(c => c.Tenancy!.Property!.Reference).Take(250).ToList(),
            Arrears = await arrears.GetSummaryAsync(Current.OwnerId),
            BilledThisMonth = current.Sum(c => c.Amount),
            CollectedThisMonth = current.Sum(c => c.AmountPaid),
            TotalOutstanding = all.Sum(c => c.Outstanding),
            Filter = filter,
            Query = q
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(RentPaymentModel model)
    {
        if (!IsStaff) return AccessDenied();

        var charge = await Db.RentCharges.Include(c => c.Tenancy).ThenInclude(t => t!.Property)
            .FirstOrDefaultAsync(c => c.Id == model.ChargeId);
        if (charge is null) return NotFound();

        if (!ModelState.IsValid)
        {
            Toast("Enter a valid payment amount.", "danger");
            return RedirectToAction(nameof(Index));
        }

        charge.AmountPaid += model.Amount;
        charge.PaidOn = model.PaidOn;
        charge.BankReference = model.BankReference;
        charge.Notes = model.Notes;
        charge.Reconciliation = charge.AmountPaid switch
        {
            _ when charge.AmountPaid >= charge.Amount + 0.01m => RentReconciliation.Overpaid,
            _ when charge.AmountPaid >= charge.Amount => RentReconciliation.Matched,
            _ when charge.AmountPaid > 0 => RentReconciliation.Shortfall,
            _ => RentReconciliation.Unreconciled
        };

        await Db.SaveChangesAsync();
        Toast($"Payment of {model.Amount.Money2()} recorded against {charge.Tenancy?.Property?.Reference}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reconcile(int id)
    {
        if (!IsStaff) return AccessDenied();

        var charge = await Db.RentCharges.FirstOrDefaultAsync(c => c.Id == id);
        if (charge is null) return NotFound();

        charge.Reconciliation = charge.AmountPaid switch
        {
            _ when charge.AmountPaid >= charge.Amount + 0.01m => RentReconciliation.Overpaid,
            _ when charge.AmountPaid >= charge.Amount => RentReconciliation.Matched,
            _ when charge.AmountPaid > 0 => RentReconciliation.Shortfall,
            _ => RentReconciliation.Unreconciled
        };

        await Db.SaveChangesAsync();
        Toast("Charge reconciled against the bank ledger.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendChaser(int id)
    {
        if (!IsStaff) return AccessDenied();

        var charge = await Db.RentCharges
            .Include(c => c.Tenancy)!.ThenInclude(t => t!.Property)
            .Include(c => c.Tenancy)!.ThenInclude(t => t!.Tenant)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (charge?.Tenancy?.Tenant is null || charge.Tenancy.Property is null) return NotFound();

        var tenancyId = charge.TenancyId;
        var schedule = await Db.RentCharges.AsNoTracking()
            .Where(c => c.TenancyId == tenancyId && c.AmountPaid < c.Amount)
            .ToListAsync();

        var outstanding = schedule.Sum(c => c.Amount - c.AmountPaid);
        var oldest = schedule.Count > 0 ? schedule.Min(c => c.DueDate) : charge.DueDate;
        var draft = ArrearsService.BuildChaserDraft(charge.Tenancy, outstanding, oldest, charge.Tenancy.Tenant.FullName);

        charge.ChaserSentOn = DateOnly.FromDateTime(DateTime.Today);
        Db.ContactNotes.Add(new ContactNote
        {
            ContactType = ContactType.Tenant,
            ContactName = charge.Tenancy.Tenant.FullName,
            ContactEmail = charge.Tenancy.Tenant.Email,
            Channel = ContactChannel.Email,
            Summary = $"Rent chaser prepared: £{outstanding:N2} outstanding. Draft: {draft.Replace("\n", " ")}",
            OccurredAt = DateTime.Now,
            CreatedBy = Current.Name,
            PropertyId = charge.Tenancy.PropertyId,
            IsFollowUp = true,
            FollowUpOn = DateOnly.FromDateTime(DateTime.Today).AddDays(5)
        });

        await Db.SaveChangesAsync();
        Toast("Rent chaser recorded and saved to the contact timeline.", "info");
        return RedirectToAction(nameof(Index));
    }
}
