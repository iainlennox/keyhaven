using Keyhaven.Web.Data;
using Keyhaven.Web.Models;
using Keyhaven.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Controllers;

[Authorize]
public class AdminController(KeyhavenDbContext db, CurrentUser current, AiInboxService ai) : PortalControllerBase(db, current)
{
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin) return AccessDenied();

        ViewBag.Users = await Db.Users.AsNoTracking().OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
        ViewBag.PropertyCount = await Db.Properties.CountAsync();
        ViewBag.TenancyCount = await Db.Tenancies.CountAsync();
        ViewBag.MessageCount = await Db.InboxMessages.CountAsync();
        ViewBag.CertificateCount = await Db.ComplianceCertificates.CountAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDemo()
    {
        if (!IsAdmin) return AccessDenied();

        await Db.Database.EnsureDeletedAsync();
        await Db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(Db);
        await ai.ProcessPendingAsync();

        Toast("Demo data has been rebuilt from the seed.", "info");
        return RedirectToAction(nameof(Index));
    }
}
