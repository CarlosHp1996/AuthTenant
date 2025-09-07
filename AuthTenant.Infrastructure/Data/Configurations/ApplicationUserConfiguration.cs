using AuthTenant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthTenant.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the ApplicationUser entity.
/// Extends IdentityUser with tenant-specific properties and multi-tenant constraints.
/// </summary>
/// <remarks>
/// This configuration handles:
/// - Multi-tenant user isolation and relationships
/// - User profile information (FirstName, LastName)
/// - Unique constraints within tenant boundaries
/// - Integration with ASP.NET Core Identity
/// - Performance optimization through proper indexing
/// </remarks>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <summary>
    /// Configures the ApplicationUser entity mapping and database schema.
    /// </summary>
    /// <param name="builder">Entity type builder for ApplicationUser configuration</param>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Configure tenant relationship and isolation
        ConfigureTenantRelationship(builder);

        // Configure user profile properties
        ConfigureUserProfile(builder);

        // Configure relationships
        ConfigureRelationships(builder);

        // Configure indexes for performance
        ConfigureIndexes(builder);

        // Configure additional Identity properties
        ConfigureIdentityProperties(builder);
    }

    /// <summary>
    /// Configures tenant relationship for multi-tenant user isolation.
    /// </summary>
    private static void ConfigureTenantRelationship(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.TenantId)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("Tenant identifier for user isolation");
    }

    /// <summary>
    /// Configures user profile properties with validation constraints.
    /// </summary>
    private static void ConfigureUserProfile(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("User's first name");

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("User's last name");
    }

    /// <summary>
    /// Configures relationships with other entities.
    /// </summary>
    private static void ConfigureRelationships(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Configure relationship with Tenant entity
        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ApplicationUsers_Tenants");
    }

    /// <summary>
    /// Configures database indexes for performance optimization.
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Unique index for email within tenant
        builder.HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique()
            .HasDatabaseName("IX_ApplicationUsers_TenantId_Email");

        // Index for user name within tenant
        builder.HasIndex(u => new { u.TenantId, u.UserName })
            .IsUnique()
            .HasDatabaseName("IX_ApplicationUsers_TenantId_UserName");

        // Index for tenant filtering
        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_ApplicationUsers_TenantId");

        // Index for name-based searches
        builder.HasIndex(u => new { u.TenantId, u.FirstName, u.LastName })
            .HasDatabaseName("IX_ApplicationUsers_TenantId_FirstName_LastName");
    }

    /// <summary>
    /// Configures additional ASP.NET Core Identity properties.
    /// </summary>
    private static void ConfigureIdentityProperties(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Configure email with enhanced constraints
        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .HasComment("User's email address (unique within tenant)");

        // Configure username with enhanced constraints
        builder.Property(u => u.UserName)
            .HasMaxLength(256)
            .HasComment("User's login name (unique within tenant)");

        // Configure phone number
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20)
            .HasComment("User's phone number");

        // Email confirmation
        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if the user's email has been confirmed");

        // Phone number confirmation
        builder.Property(u => u.PhoneNumberConfirmed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if the user's phone number has been confirmed");

        // Two-factor authentication
        builder.Property(u => u.TwoFactorEnabled)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if two-factor authentication is enabled");

        // Account lockout
        builder.Property(u => u.LockoutEnabled)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if the account can be locked out");

        builder.Property(u => u.LockoutEnd)
            .HasComment("End time of account lockout");

        // Access failed count
        builder.Property(u => u.AccessFailedCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Number of failed access attempts");
    }
}
