using System.ComponentModel.DataAnnotations;

namespace Keyhaven.Web.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [Required, StringLength(30)]
    public string Role { get; set; } = Roles.Manager;

    [StringLength(40)]
    public string? Phone { get; set; }

    /// <summary>Owner.Id, Tenant.Id or Contractor.Id depending on the role.</summary>
    public int? LinkedEntityId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}
