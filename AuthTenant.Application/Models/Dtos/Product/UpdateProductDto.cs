using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AuthTenant.Application.Models.Dtos.Product
{
    /// <summary>
    /// Data Transfer Object for updating existing products.
    /// Contains all modifiable product information with comprehensive validation rules.
    /// Supports partial updates while maintaining data integrity and business rule compliance.
    /// </summary>
    public sealed class UpdateProductDto
    {
        /// <summary>
        /// Gets or sets the product name.
        /// Must remain unique within the tenant and be descriptive of the product.
        /// </summary>
        /// <example>Premium Wireless Headphones - Updated Edition</example>
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product description.
        /// Optional detailed description of product features, specifications, or usage instructions.
        /// Supports markdown formatting for rich text display.
        /// </summary>
        /// <example>Updated high-quality wireless headphones with enhanced active noise cancellation, 35-hour battery life, and premium audio drivers.</example>
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the product price.
        /// Must be a positive value representing the current selling price in the tenant's currency.
        /// </summary>
        /// <example>349.99</example>
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Price must be greater than 0")]
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the Stock Keeping Unit identifier.
        /// Optional unique identifier for inventory management and tracking.
        /// Must remain unique within the tenant if changed.
        /// </summary>
        /// <example>WH-PRE-002</example>
        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "SKU can only contain letters, numbers, hyphens, and underscores")]
        [JsonPropertyName("sku")]
        public string? SKU { get; set; }

        /// <summary>
        /// Gets or sets the current stock quantity.
        /// Represents the number of units available for sale.
        /// Updates may trigger stock level alerts if below minimum threshold.
        /// </summary>
        /// <example>150</example>
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        [JsonPropertyName("stockQuantity")]
        public int StockQuantity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is active.
        /// Inactive products are hidden from catalogs and unavailable for purchase.
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the product categories.
        /// Used for organization, filtering, and navigation purposes.
        /// Replaces existing categories if provided.
        /// </summary>
        [JsonPropertyName("categories")]
        public IList<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the product tags.
        /// Used for search optimization and content discovery.
        /// Replaces existing tags if provided.
        /// </summary>
        [JsonPropertyName("tags")]
        public IList<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the minimum stock level for low stock alerts.
        /// When stock falls below this level, alerts will be triggered.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock level cannot be negative")]
        [JsonPropertyName("minimumStockLevel")]
        public int? MinimumStockLevel { get; set; }

        /// <summary>
        /// Gets or sets the product weight in grams.
        /// Used for shipping calculations and logistics.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Weight cannot be negative")]
        [JsonPropertyName("weight")]
        public double? Weight { get; set; }

        /// <summary>
        /// Gets or sets the product dimensions.
        /// Used for shipping and storage calculations.
        /// </summary>
        [JsonPropertyName("dimensions")]
        public ProductDimensions? Dimensions { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the product.
        /// Flexible key-value pairs for custom attributes specific to business needs.
        /// Merges with existing metadata if provided.
        /// </summary>
        [JsonPropertyName("metadata")]
        public IDictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets flags indicating which fields should be updated.
        /// Used for partial updates to distinguish between null values and fields that should not be changed.
        /// </summary>
        [JsonPropertyName("updateFields")]
        public UpdateFields? UpdateFields { get; set; }

        /// <summary>
        /// Gets the normalized product name for searching and comparison.
        /// Converts to lowercase and removes extra whitespace.
        /// </summary>
        [JsonIgnore]
        public string NormalizedName => NormalizeName(Name);

        /// <summary>
        /// Gets the normalized SKU for uniqueness validation.
        /// Converts to uppercase and removes extra whitespace.
        /// </summary>
        [JsonIgnore]
        public string? NormalizedSKU => string.IsNullOrWhiteSpace(SKU) ? null : SKU.Trim().ToUpperInvariant();

        /// <summary>
        /// Gets a value indicating whether the update request has sufficient information.
        /// Validates beyond basic attribute validation to ensure business rule compliance.
        /// </summary>
        [JsonIgnore]
        public bool HasSufficientInfo => !string.IsNullOrWhiteSpace(Name) && Price > 0;

        /// <summary>
        /// Gets a value indicating whether any stock-related fields are being updated.
        /// </summary>
        [JsonIgnore]
        public bool IsStockUpdate => UpdateFields?.StockQuantity == true || UpdateFields?.MinimumStockLevel == true;

        /// <summary>
        /// Gets a value indicating whether any pricing fields are being updated.
        /// </summary>
        [JsonIgnore]
        public bool IsPriceUpdate => UpdateFields?.Price == true;

        /// <summary>
        /// Validates the update product request for completeness and business rules.
        /// </summary>
        /// <returns>True if the request is valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   Name.Length >= 2 &&
                   Name.Length <= 200 &&
                   Price > 0 &&
                   StockQuantity >= 0 &&
                   (MinimumStockLevel ?? 0) >= 0 &&
                   (Description?.Length ?? 0) <= 2000 &&
                   (SKU?.Length ?? 0) <= 50 &&
                   (Weight ?? 0) >= 0 &&
                   IsValidName(Name) &&
                   IsValidSKU(SKU) &&
                   IsValidCategories(Categories) &&
                   IsValidTags(Tags);
        }

        /// <summary>
        /// Validates the product name format and content.
        /// </summary>
        /// <param name="name">The product name to validate</param>
        /// <returns>True if the name is valid</returns>
        private static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Check for minimum meaningful content (not just spaces/special chars)
            var cleanName = Regex.Replace(name.Trim(), @"[^\w\s]", "");
            return cleanName.Length >= 2;
        }

        /// <summary>
        /// Validates the SKU format if provided.
        /// </summary>
        /// <param name="sku">The SKU to validate</param>
        /// <returns>True if the SKU is valid or null</returns>
        private static bool IsValidSKU(string? sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return true;

            return Regex.IsMatch(sku, @"^[A-Za-z0-9\-_]+$") && sku.Length <= 50;
        }

        /// <summary>
        /// Validates the categories list.
        /// </summary>
        /// <param name="categories">The categories to validate</param>
        /// <returns>True if categories are valid</returns>
        private static bool IsValidCategories(IList<string> categories)
        {
            if (categories == null || !categories.Any())
                return true;

            return categories.All(c => !string.IsNullOrWhiteSpace(c) && c.Length <= 100) &&
                   categories.Count <= 10;
        }

        /// <summary>
        /// Validates the tags list.
        /// </summary>
        /// <param name="tags">The tags to validate</param>
        /// <returns>True if tags are valid</returns>
        private static bool IsValidTags(IList<string> tags)
        {
            if (tags == null || !tags.Any())
                return true;

            return tags.All(t => !string.IsNullOrWhiteSpace(t) && t.Length <= 50) &&
                   tags.Count <= 20;
        }

        /// <summary>
        /// Normalizes a product name for consistent processing.
        /// </summary>
        /// <param name="name">The name to normalize</param>
        /// <returns>Normalized name</returns>
        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return Regex.Replace(name.Trim(), @"\s+", " ").ToLowerInvariant();
        }

        /// <summary>
        /// Sanitizes the update request by cleaning and normalizing input data.
        /// </summary>
        public void Sanitize()
        {
            Name = Name?.Trim() ?? string.Empty;
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
            SKU = string.IsNullOrWhiteSpace(SKU) ? null : SKU.Trim().ToUpperInvariant();

            // Clean categories and tags
            Categories = Categories?.Where(c => !string.IsNullOrWhiteSpace(c))
                                  .Select(c => c.Trim())
                                  .Distinct()
                                  .ToList() ?? new List<string>();

            Tags = Tags?.Where(t => !string.IsNullOrWhiteSpace(t))
                       .Select(t => t.Trim().ToLowerInvariant())
                       .Distinct()
                       .ToList() ?? new List<string>();

            // Ensure minimum stock level is reasonable
            if (MinimumStockLevel.HasValue && MinimumStockLevel > StockQuantity)
                MinimumStockLevel = Math.Max(0, StockQuantity - 1);
        }

        /// <summary>
        /// Gets validation errors for the update request.
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public IList<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Product name is required");
            else if (Name.Length < 2)
                errors.Add("Product name must be at least 2 characters long");
            else if (Name.Length > 200)
                errors.Add("Product name cannot exceed 200 characters");

            if (Price <= 0)
                errors.Add("Price must be greater than 0");

            if (StockQuantity < 0)
                errors.Add("Stock quantity cannot be negative");

            if (!string.IsNullOrWhiteSpace(Description) && Description.Length > 2000)
                errors.Add("Description cannot exceed 2000 characters");

            if (!string.IsNullOrWhiteSpace(SKU) && !IsValidSKU(SKU))
                errors.Add("SKU contains invalid characters or exceeds length limit");

            if (Categories?.Count > 10)
                errors.Add("Cannot have more than 10 categories");

            if (Tags?.Count > 20)
                errors.Add("Cannot have more than 20 tags");

            if (Weight.HasValue && Weight < 0)
                errors.Add("Weight cannot be negative");

            if (MinimumStockLevel.HasValue && MinimumStockLevel < 0)
                errors.Add("Minimum stock level cannot be negative");

            return errors;
        }

        /// <summary>
        /// Determines if the update represents a significant change that requires additional validation or notifications.
        /// </summary>
        /// <returns>True if this is a significant update</returns>
        public bool IsSignificantUpdate()
        {
            return IsPriceUpdate || IsStockUpdate || UpdateFields?.IsActive == true;
        }

        /// <summary>
        /// Creates a new UpdateProductDto for testing purposes.
        /// </summary>
        /// <param name="name">Product name</param>
        /// <param name="price">Product price</param>
        /// <returns>Configured UpdateProductDto instance</returns>
        public static UpdateProductDto CreateForTesting(string name, decimal price)
        {
            return new UpdateProductDto
            {
                Name = name,
                Price = price,
                StockQuantity = 100,
                IsActive = true,
                UpdateFields = new UpdateFields
                {
                    Name = true,
                    Price = true,
                    StockQuantity = true,
                    IsActive = true
                }
            };
        }
    }

    /// <summary>
    /// Represents flags indicating which fields should be updated in a partial update scenario.
    /// Helps distinguish between explicit null values and fields that should remain unchanged.
    /// </summary>
    public sealed class UpdateFields
    {
        /// <summary>
        /// Gets or sets a value indicating whether the name should be updated.
        /// </summary>
        [JsonPropertyName("name")]
        public bool Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the description should be updated.
        /// </summary>
        [JsonPropertyName("description")]
        public bool Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the price should be updated.
        /// </summary>
        [JsonPropertyName("price")]
        public bool Price { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the SKU should be updated.
        /// </summary>
        [JsonPropertyName("sku")]
        public bool SKU { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the stock quantity should be updated.
        /// </summary>
        [JsonPropertyName("stockQuantity")]
        public bool StockQuantity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the active status should be updated.
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the categories should be updated.
        /// </summary>
        [JsonPropertyName("categories")]
        public bool Categories { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tags should be updated.
        /// </summary>
        [JsonPropertyName("tags")]
        public bool Tags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the minimum stock level should be updated.
        /// </summary>
        [JsonPropertyName("minimumStockLevel")]
        public bool MinimumStockLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the weight should be updated.
        /// </summary>
        [JsonPropertyName("weight")]
        public bool Weight { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dimensions should be updated.
        /// </summary>
        [JsonPropertyName("dimensions")]
        public bool Dimensions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the metadata should be updated.
        /// </summary>
        [JsonPropertyName("metadata")]
        public bool Metadata { get; set; }
    }
}
