using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Keyhaven.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Controllers;

public abstract class PortalControllerBase : Controller
{
    protected readonly KeyhavenDbContext Db;
    protected readonly CurrentUser Current;

    protected PortalControllerBase(KeyhavenDbContext db, CurrentUser current)
    {
        Db = db;
        Current = current;
    }

    protected bool IsStaff => Current.IsStaff;
    protected bool IsAdmin => Current.IsAdmin;
    protected bool IsLandlord => Current.Role == Roles.Landlord;
    protected bool IsTenant => Current.Role == Roles.Tenant;
    protected bool IsContractor => Current.Role == Roles.Contractor;

    protected IActionResult AccessDenied() => RedirectToAction("AccessDenied", "Account");

    /// <summary>Landlords see their own properties; tenants see the property they rent.</summary>
    protected async Task<bool> CanViewPropertyAsync(int propertyId, CancellationToken ct = default)
    {
        if (IsStaff) return true;

        if (IsLandlord && Current.LinkedId is not null)
        {
            return await Db.Properties.AnyAsync(p => p.Id == propertyId && p.OwnerId == Current.LinkedId, ct);
        }

        if (IsTenant && Current.LinkedId is not null)
        {
            return await Db.Tenancies.AnyAsync(
                t => t.PropertyId == propertyId && t.TenantId == Current.LinkedId && t.Status == TenancyStatus.Active, ct);
        }

        if (IsContractor && Current.LinkedId is not null)
        {
            return await Db.MaintenanceTickets.AnyAsync(
                t => t.PropertyId == propertyId && t.AssignedContractorId == Current.LinkedId, ct);
        }

        return false;
    }

    protected async Task<bool> CanViewTenancyAsync(int tenancyId, CancellationToken ct = default)
    {
        if (IsStaff) return true;

        var tenancy = await Db.Tenancies.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenancyId, ct);
        if (tenancy is null) return false;

        if (IsLandlord && Current.LinkedId is not null)
        {
            return await Db.Properties.AnyAsync(p => p.Id == tenancy.PropertyId && p.OwnerId == Current.LinkedId, ct);
        }

        if (IsTenant)
        {
            return tenancy.TenantId == Current.LinkedId;
        }

        return false;
    }

    protected async Task<bool> CanViewTicketAsync(int ticketId, CancellationToken ct = default)
    {
        if (IsStaff) return true;

        var ticket = await Db.MaintenanceTickets.AsNoTracking().Include(t => t.Property).FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null) return false;

        return Current.Role switch
        {
            Roles.Contractor => ticket.AssignedContractorId == Current.LinkedId,
            Roles.Tenant => ticket.TenantId == Current.LinkedId,
            Roles.Landlord => ticket.Property?.OwnerId == Current.LinkedId,
            _ => false
        };
    }

    protected void Toast(string message, string tone = "positive")
    {
        TempData["Toast"] = message;
        TempData["ToastTone"] = tone;
    }
}
