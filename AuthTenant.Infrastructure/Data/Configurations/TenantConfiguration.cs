using System.Text.Json;

using AuthTenant.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthTenant.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Tenant entity.
/// Defines database schema, constraints, indexes, and data seeding for multi-tenancy support.
/// </summary>
/// <remarks>
/// This configuration establishes the foundation for multi-tenant architecture by:
/// - Defining tenant identification and naming constraints
/// - Setting up JSON serialization for tenant settings
/// - Creating unique indexes for tenant isolation
/// - Seeding a default tenant for system initialization
/// </remarks>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <summary>
    /// Configures the Tenant entity mapping and database schema.
    /// </summary>
    /// <param name="builder">Entity type builder for Tenant configuration</param>
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Primary key configuration
        builder.HasKey(t => t.Id);

        // Configure Id property with validation constraints
        builder.Property(t => t.Id)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("Unique tenant identifier, used as partition key");

        // Configure Name property with business rules
        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Unique tenant name for routing and identification");

        // Configure DisplayName for user interfaces
        builder.Property(t => t.DisplayName)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Human-readable tenant name for UI display");

        // Configure optional Description
        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .HasComment("Optional detailed description of the tenant");

        // Configure optional ConnectionString for database isolation
        builder.Property(t => t.ConnectionString)
            .HasMaxLength(1000)
            .HasComment("Optional dedicated connection string for tenant data isolation");

        // Configure IsActive status
        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates whether the tenant is currently active");

        // Configure Settings as JSON with proper value comparison
        ConfigureSettingsProperty(builder);

        // Configure indexes for performance and uniqueness
        ConfigureIndexes(builder);

        // Configure audit properties from BaseEntity
        ConfigureAuditProperties(builder);

        // Seed default tenant data
        SeedDefaultTenant(builder);
    }

    /// <summary>
    /// Configures the Settings property with JSON serialization and value comparison.
    /// </summary>
    private static void ConfigureSettingsProperty(EntityTypeBuilder<Tenant> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.Property(t => t.Settings)
            .HasColumnType("jsonb") // Use jsonb for PostgreSQL, adjust for other databases
            .HasConversion(
                // Convert to string for storage
                v => JsonSerializer.Serialize(v, jsonOptions),
                // Convert from string when reading
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(
                new ValueComparer<Dictionary<string, string>>(
                    // Equality comparison
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    // Hash code generation
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    // Snapshot creation
                    c => c.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));

        builder.Property(t => t.Settings)
            .HasComment("JSON configuration settings specific to this tenant");
    }

    /// <summary>
    /// Configures database indexes for performance and data integrity.
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<Tenant> builder)
    {
        // Unique index on Name for tenant routing
        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_Name");

        // Index on IsActive for filtering active tenants
        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("IX_Tenants_IsActive");

        // Composite index for common queries
        builder.HasIndex(t => new { t.IsActive, t.CreatedAt })
            .HasDatabaseName("IX_Tenants_IsActive_CreatedAt");
    }

    /// <summary>
    /// Configures audit properties for tenant tracking.
    /// </summary>
    private static void ConfigureAuditProperties(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasComment("Timestamp when the tenant was created");

        builder.Property(t => t.UpdatedAt)
            .HasComment("Timestamp when the tenant was last updated");

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Soft delete flag");

        // Global query filter for soft delete
        builder.HasQueryFilter(t => !t.IsDeleted);
    }

    /// <summary>
    /// Seeds the default system tenant for initial setup.
    /// </summary>
    private static void SeedDefaultTenant(EntityTypeBuilder<Tenant> builder)
    {
        var defaultSettings = new Dictionary<string, string>
        {
            { "theme", "default" },
            { "language", "en-US" },
            { "timezone", "UTC" }
        };

        builder.HasData(new
        {
            Id = "default",
            Name = "default",
            DisplayName = "Default Tenant",
            Description = "Default system tenant for initial setup",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false,
            MaxUsers = 100,
            StorageQuotaBytes = 1073741824L, // 1 GB
            StorageUsedBytes = 0L,
            SubscriptionPlan = "Basic",
            Language = "en-US",
            UsesSingleSignOn = false,
            Settings = defaultSettings
        });
    }
}
