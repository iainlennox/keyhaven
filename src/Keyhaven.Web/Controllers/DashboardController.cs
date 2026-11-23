using Keyhaven.Web.Models;
using Keyhaven.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keyhaven.Web.Controllers;

[Authorize]
public class DashboardController(DashboardService dashboards, CurrentUser current) : Controller
{
    public async Task<IActionResult> Index()
    {
        switch (current.Role)
        {
            case Roles.Tenant:
                return View("Tenant", await dashboards.BuildTenantAsync(current.LinkedId ?? 0));
            case Roles.Contractor:
                return View("Contractor", await dashboards.BuildContractorAsync(current.LinkedId ?? 0));
            default:
                return View(await dashboards.BuildStaffAsync(current.OwnerId, current.Name));
        }
    }
}
