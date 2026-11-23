using System.Security.Claims;

namespace Keyhaven.Web.Services;

public class CurrentUser(IHttpContextAccessor accessor)
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int? Id => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string Name => Principal?.FindFirstValue(ClaimTypes.Name) ?? "Guest";

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? "";

    public string Role => Principal?.FindFirstValue(ClaimTypes.Role) ?? "";

    public int? LinkedId => int.TryParse(Principal?.FindFirstValue("linked_id"), out var id) ? id : null;

    public bool IsStaff => Role is Models.Roles.Admin or Models.Roles.Manager;

    public bool IsAdmin => Role == Models.Roles.Admin;

    public int? OwnerId => Role == Models.Roles.Landlord ? LinkedId : null;

    public int? TenantId => Role == Models.Roles.Tenant ? LinkedId : null;

    public int? ContractorId => Role == Models.Roles.Contractor ? LinkedId : null;

    public string Initials
    {
        get
        {
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "K",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
            };
        }
    }
}
