using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Keyhaven.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Controllers;

public class HomeController(KeyhavenDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel
        {
            ManagedProperties = await db.Properties.CountAsync(),
            ActiveTenancies = await db.Tenancies.CountAsync(t => t.Status == TenancyStatus.Active),
            ComplianceChecks = await db.ComplianceCertificates.CountAsync(),
            AvailableProperties = await db.Properties.AsNoTracking()
                .Include(p => p.Owner)
                .Where(p => p.Status == PropertyStatus.Available && p.ListingPublished)
                .OrderBy(p => p.City)
                .Take(3)
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Lettings() => View();

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
