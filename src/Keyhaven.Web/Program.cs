using Keyhaven.Web.Data;
using Keyhaven.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=keyhaven.db";
builder.Services.AddDbContext<KeyhavenDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ComplianceService>();
builder.Services.AddScoped<ArrearsService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AiInboxService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.Name = "keyhaven.auth";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KeyhavenDbContext>();
    if (args.Contains("--reset-demo"))
    {
        await db.Database.EnsureDeletedAsync();
    }

    await db.Database.EnsureCreatedAsync();
    await SeedData.InitializeAsync(db);
    await scope.ServiceProvider.GetRequiredService<AiInboxService>().ProcessPendingAsync();
}

app.Run();
