using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthTenant.Application.Models.Dtos.Product
{
    /// <summary>
    /// Data Transfer Object representing a product entity.
    /// Contains complete product information for API responses in a multi-tenant environment.
    /// Designed for read operations and data display purposes.
    /// </summary>
    public sealed class ProductDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the product.
        /// This is a GUID that uniquely identifies the product across the system.
        /// </summary>
        /// <example>550e8400-e29b-41d4-a716-446655440000</example>
        [Required]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the product name.
        /// This is the display name shown to users and should be descriptive and unique within the tenant.
        /// </summary>
        /// <example>Premium Wireless Headphones</example>
        [Required]
        [StringLength(200, MinimumLength = 1)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product description.
        /// Optional detailed description of the product features, specifications, or usage instructions.
        /// </summary>
        /// <example>High-quality wireless headphones with noise cancellation and 30-hour battery life.</example>
        [StringLength(2000)]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the product price.
        /// Must be a positive decimal value representing the current selling price.
        /// </summary>
        /// <example>299.99</example>
        [Required]
        [Range(0.01, (double)decimal.MaxValue)]
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the Stock Keeping Unit identifier.
        /// Optional unique identifier used for inventory management and tracking.
        /// </summary>
        /// <example>WH-PRE-001</example>
        [StringLength(50)]
        [JsonPropertyName("sku")]
        public string? SKU { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is active.
        /// Inactive products are not displayed in catalogs but remain in the system for historical purposes.
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the current stock quantity.
        /// Represents the number of units available for sale.
        /// </summary>
        /// <example>150</example>
        [Range(0, int.MaxValue)]
        [JsonPropertyName("stockQuantity")]
        public int StockQuantity { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// Ensures proper data isolation in multi-tenant architecture.
        /// </summary>
        /// <example>tenant-retail-001</example>
        [Required]
        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the product was created.
        /// Stored in UTC format for consistency across timezones.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the product was last updated.
        /// Null if the product has never been updated since creation.
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the user ID who created this product.
        /// Used for auditing and tracking product ownership.
        /// </summary>
        [JsonPropertyName("createdBy")]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the user ID who last updated this product.
        /// Used for auditing and tracking modification history.
        /// </summary>
        [JsonPropertyName("updatedBy")]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Gets the formatted price string for display purposes.
        /// Includes currency formatting based on the current culture.
        /// </summary>
        [JsonPropertyName("formattedPrice")]
        public string FormattedPrice => FormatPrice(Price);

        /// <summary>
        /// Gets a value indicating whether the product is in stock.
        /// Returns true if stock quantity is greater than zero.
        /// </summary>
        [JsonPropertyName("inStock")]
        public bool InStock => StockQuantity > 0;

        /// <summary>
        /// Gets the stock status description based on current inventory levels.
        /// Provides human-readable stock status for UI display.
        /// </summary>
        [JsonPropertyName("stockStatus")]
        public string StockStatus => GetStockStatus(StockQuantity);

        /// <summary>
        /// Gets a value indicating whether the product is newly created.
        /// Returns true if created within the last 7 days.
        /// </summary>
        [JsonPropertyName("isNew")]
        public bool IsNew => (DateTime.UtcNow - CreatedAt).TotalDays <= 7;

        /// <summary>
        /// Gets a value indicating whether the product was recently updated.
        /// Returns true if updated within the last 24 hours.
        /// </summary>
        [JsonPropertyName("recentlyUpdated")]
        public bool RecentlyUpdated => UpdatedAt.HasValue && (DateTime.UtcNow - UpdatedAt.Value).TotalHours <= 24;

        /// <summary>
        /// Gets the display name for the product.
        /// Uses SKU and name combination if SKU is available, otherwise just the name.
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName => !string.IsNullOrWhiteSpace(SKU) ? $"[{SKU}] {Name}" : Name;

        /// <summary>
        /// Gets or sets the product categories.
        /// Used for classification and filtering purposes.
        /// </summary>
        [JsonPropertyName("categories")]
        public IReadOnlyList<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the product tags for enhanced searchability.
        /// Used for search optimization and content discovery.
        /// </summary>
        [JsonPropertyName("tags")]
        public IReadOnlyList<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Formats the price value for display purposes.
        /// </summary>
        /// <param name="price">The price to format</param>
        /// <returns>Formatted price string</returns>
        private static string FormatPrice(decimal price)
        {
            try
            {
                return price.ToString("C2");
            }
            catch
            {
                return price.ToString("F2");
            }
        }

        /// <summary>
        /// Determines the stock status based on quantity levels.
        /// </summary>
        /// <param name="stockQuantity">Current stock quantity</param>
        /// <returns>Stock status description</returns>
        private static string GetStockStatus(int stockQuantity)
        {
            return stockQuantity switch
            {
                0 => "Out of Stock",
                <= 5 => "Low Stock",
                <= 20 => "Limited Stock",
                _ => "In Stock"
            };
        }

        /// <summary>
        /// Validates the product DTO for consistency and business rules.
        /// </summary>
        /// <returns>True if the product DTO is valid</returns>
        public bool IsValid()
        {
            return Id != Guid.Empty &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(TenantId) &&
                   Price > 0 &&
                   StockQuantity >= 0 &&
                   Name.Length <= 200 &&
                   (Description?.Length ?? 0) <= 2000 &&
                   (SKU?.Length ?? 0) <= 50;
        }

        /// <summary>
        /// Gets a summary of the product for display in lists or search results.
        /// </summary>
        /// <returns>Product summary information</returns>
        public ProductSummary GetSummary()
        {
            return new ProductSummary
            {
                Id = Id,
                Name = Name,
                Price = Price,
                FormattedPrice = FormattedPrice,
                InStock = InStock,
                StockStatus = StockStatus,
                IsActive = IsActive,
                SKU = SKU
            };
        }

        /// <summary>
        /// Creates a new ProductDto instance for testing purposes.
        /// </summary>
        /// <param name="name">Product name</param>
        /// <param name="price">Product price</param>
        /// <param name="tenantId">Tenant identifier</param>
        /// <returns>Configured ProductDto instance</returns>
        public static ProductDto CreateForTesting(string name, decimal price, string tenantId)
        {
            return new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                Price = price,
                TenantId = tenantId,
                IsActive = true,
                StockQuantity = 100,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Lightweight summary representation of a product.
    /// Used for list views and search results to minimize data transfer.
    /// </summary>
    public sealed class ProductSummary
    {
        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product price.
        /// </summary>
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the formatted price string.
        /// </summary>
        [JsonPropertyName("formattedPrice")]
        public string FormattedPrice { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the product is in stock.
        /// </summary>
        [JsonPropertyName("inStock")]
        public bool InStock { get; set; }

        /// <summary>
        /// Gets or sets the stock status description.
        /// </summary>
        [JsonPropertyName("stockStatus")]
        public string StockStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the product is active.
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the product SKU.
        /// </summary>
        [JsonPropertyName("sku")]
        public string? SKU { get; set; }
    }
}
