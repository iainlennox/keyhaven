using Keyhaven.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Data;

public class KeyhavenDbContext(DbContextOptions<KeyhavenDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Tenancy> Tenancies => Set<Tenancy>();
    public DbSet<RentCharge> RentCharges => Set<RentCharge>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
    public DbSet<ComplianceCertificate> ComplianceCertificates => Set<ComplianceCertificate>();
    public DbSet<Viewing> Viewings => Set<Viewing>();
    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<KeyItem> KeyItems => Set<KeyItem>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<ContactNote> ContactNotes => Set<ContactNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Property>().HasIndex(p => p.Reference).IsUnique();
        modelBuilder.Entity<MaintenanceTicket>().HasIndex(t => t.Reference).IsUnique();

        modelBuilder.Entity<Owner>()
            .HasMany(o => o.Properties)
            .WithOne(p => p.Owner)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Property>()
            .HasOne(p => p.Manager)
            .WithMany()
            .HasForeignKey(p => p.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Tenancy>()
            .HasOne(t => t.Property)
            .WithMany(p => p.Tenancies)
            .HasForeignKey(t => t.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tenancy>()
            .HasOne(t => t.Tenant)
            .WithMany(t => t.Tenancies)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RentCharge>()
            .HasOne(c => c.Tenancy)
            .WithMany(t => t.RentCharges)
            .HasForeignKey(c => c.TenancyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MaintenanceTicket>()
            .HasOne(t => t.Property)
            .WithMany(p => p.MaintenanceTickets)
            .HasForeignKey(t => t.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComplianceCertificate>()
            .HasOne(c => c.Property)
            .WithMany(p => p.Certificates)
            .HasForeignKey(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Viewing>()
            .HasOne(v => v.Property)
            .WithMany(p => p.Viewings)
            .HasForeignKey(v => v.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enquiry>()
            .HasOne(e => e.Property)
            .WithMany(p => p.Enquiries)
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<KeyItem>()
            .HasOne(k => k.Property)
            .WithMany(p => p.Keys)
            .HasForeignKey(k => k.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InboxMessage>()
            .HasOne(m => m.LinkedProperty)
            .WithMany()
            .HasForeignKey(m => m.LinkedPropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ContactNote>()
            .HasOne(n => n.Property)
            .WithMany()
            .HasForeignKey(n => n.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
