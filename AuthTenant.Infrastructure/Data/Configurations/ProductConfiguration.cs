using AuthTenant.Domain.Entities;
using AuthTenant.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthTenant.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Product entity.
/// Defines database schema, relationships, constraints, and business rules for product management.
/// </summary>
/// <remarks>
/// This configuration handles:
/// - Product identity and basic properties
/// - Multi-tenant data isolation
/// - Money value object conversion
/// - Business constraints and indexes
/// - Soft delete implementation
/// - Performance optimization through proper indexing
/// </remarks>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>
    /// Configures the Product entity mapping and database schema.
    /// </summary>
    /// <param name="builder">Entity type builder for Product configuration</param>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Configure entity key and table
        ConfigureKey(builder);

        // Configure basic properties
        ConfigureBasicProperties(builder);

        // Configure value objects
        ConfigureValueObjects(builder);

        // Configure tenant relationship and isolation
        ConfigureTenantRelationship(builder);

        // Configure indexes for performance
        ConfigureIndexes(builder);

        // Configure audit properties from BaseEntity
        ConfigureAuditProperties(builder);

        // Configure soft delete
        ConfigureSoftDelete(builder);
    }

    /// <summary>
    /// Configures the primary key for the Product entity.
    /// </summary>
    private static void ConfigureKey(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasComment("Unique identifier for the product");
    }

    /// <summary>
    /// Configures basic product properties with validation constraints.
    /// </summary>
    private static void ConfigureBasicProperties(EntityTypeBuilder<Product> builder)
    {
        // Product name with business constraints
        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Product name, must be unique within tenant");

        // Product description for detailed information
        builder.Property(p => p.Description)
            .HasMaxLength(2000)
            .HasComment("Detailed product description");

        // Price with precision for financial calculations
        builder.Property(p => p.Price)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Product price with high precision");

        // SKU for inventory management
        builder.Property(p => p.SKU)
            .HasMaxLength(50)
            .HasComment("Stock Keeping Unit for inventory tracking");

        // Category for product organization
        builder.Property(p => p.Category)
            .HasMaxLength(100)
            .HasComment("Product category for organization and filtering");

        // Weight for shipping calculations
        builder.Property(p => p.Weight)
            .HasPrecision(8, 3)
            .HasComment("Product weight in kilograms");

        // Dimensions for shipping and storage
        builder.Property(p => p.Length)
            .HasPrecision(8, 2)
            .HasComment("Product length in centimeters");

        builder.Property(p => p.Width)
            .HasPrecision(8, 2)
            .HasComment("Product width in centimeters");

        builder.Property(p => p.Height)
            .HasPrecision(8, 2)
            .HasComment("Product height in centimeters");

        // Stock management
        builder.Property(p => p.StockQuantity)
            .HasDefaultValue(0)
            .HasComment("Current stock quantity");

        builder.Property(p => p.MinimumStockLevel)
            .HasDefaultValue(0)
            .HasComment("Minimum stock level for reorder alerts");

        builder.Property(p => p.MaximumStockLevel)
            .HasDefaultValue(int.MaxValue)
            .HasComment("Maximum stock level");

        // Product status
        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if the product is currently active");

        builder.Property(p => p.IsFeatured)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if the product is featured");

        // Analytics
        builder.Property(p => p.ViewCount)
            .HasDefaultValue(0)
            .HasComment("Number of times the product has been viewed");

        builder.Property(p => p.LastViewedAt)
            .HasComment("Timestamp when the product was last viewed");

        // Tags configuration (JSON array)
        builder.Property(p => p.Tags)
            .HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasComment("Product tags for search and categorization");
    }

    /// <summary>
    /// Configures value objects for complex data types.
    /// Note: Money value object configuration would go here if Product used Money type
    /// Currently Product uses decimal for Price which is configured above
    /// </summary>
    private static void ConfigureValueObjects(EntityTypeBuilder<Product> builder)
    {
        // Future: If Product.Price becomes Money value object, configure it here
        // For now, Price is a simple decimal property configured in ConfigureBasicProperties
    }

    /// <summary>
    /// Configures tenant relationship for multi-tenant data isolation.
    /// </summary>
    private static void ConfigureTenantRelationship(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.TenantId)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("Tenant identifier for data isolation");

        // Note: Direct navigation to Tenant entity not configured
        // TenantId serves as foreign key for data isolation
    }

    /// <summary>
    /// Configures database indexes for performance optimization.
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<Product> builder)
    {
        // Unique index for SKU within tenant (when SKU is not null)
        builder.HasIndex(p => new { p.TenantId, p.SKU })
            .IsUnique()
            .HasFilter("\"SKU\" IS NOT NULL")
            .HasDatabaseName("IX_Products_TenantId_SKU");

        // Index for active products filtering
        builder.HasIndex(p => new { p.TenantId, p.IsActive })
            .HasDatabaseName("IX_Products_TenantId_IsActive");

        // Index for category filtering
        builder.HasIndex(p => new { p.TenantId, p.Category })
            .HasDatabaseName("IX_Products_TenantId_Category");

        // Index for name searching
        builder.HasIndex(p => new { p.TenantId, p.Name })
            .HasDatabaseName("IX_Products_TenantId_Name");

        // Index for stock level monitoring
        builder.HasIndex(p => new { p.TenantId, p.StockQuantity })
            .HasDatabaseName("IX_Products_TenantId_StockQuantity");

        // Composite index for common queries
        builder.HasIndex(p => new { p.TenantId, p.IsActive, p.Category, p.CreatedAt })
            .HasDatabaseName("IX_Products_TenantId_IsActive_Category_CreatedAt");
    }

    /// <summary>
    /// Configures audit properties inherited from BaseEntity.
    /// </summary>
    private static void ConfigureAuditProperties(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasComment("Timestamp when the product was created");

        builder.Property(p => p.UpdatedAt)
            .HasComment("Timestamp when the product was last updated");

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(100)
            .HasComment("User who created this product");

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(100)
            .HasComment("User who last updated this product");
    }

    /// <summary>
    /// Configures soft delete functionality.
    /// </summary>
    private static void ConfigureSoftDelete(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Soft delete flag");

        // Global query filter to exclude deleted products
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Index for soft delete filtering
        builder.HasIndex(p => new { p.TenantId, p.IsDeleted })
            .HasDatabaseName("IX_Products_TenantId_IsDeleted");
    }
}
