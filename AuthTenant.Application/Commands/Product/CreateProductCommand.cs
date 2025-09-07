using System.ComponentModel.DataAnnotations;

using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Product;

using MediatR;

namespace AuthTenant.Application.Commands.Product
{
    /// <summary>
    /// Command for creating new products in the system.
    /// Represents a request to create a product with business validation and domain rules.
    /// Follows DDD Command pattern and Clean Architecture principles.
    /// </summary>
    /// <param name="Name">Product name (required, unique identifier)</param>
    /// <param name="Description">Product description (optional, detailed information)</param>
    /// <param name="Price">Product price (required, must be positive)</param>
    /// <param name="SKU">Stock Keeping Unit (optional, unique product code)</param>
    /// <param name="StockQuantity">Initial stock quantity (required, must be non-negative)</param>
    public sealed record CreateProductCommand(
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-_.'""()&/]+$", ErrorMessage = "Product name contains invalid characters")]
        string Name,

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        string? Description,

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99")]
        decimal Price,

        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        [RegularExpression(@"^[A-Z0-9\-_]*$", ErrorMessage = "SKU can only contain uppercase letters, numbers, hyphens, and underscores")]
        string? SKU,

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be non-negative")]
        int StockQuantity
    ) : IRequest<Result<ProductDto>>
    {
        /// <summary>
        /// Gets the normalized product name for consistent processing
        /// </summary>
        public string NormalizedName => Name?.Trim() ?? string.Empty;

        /// <summary>
        /// Gets the normalized SKU in uppercase for consistency
        /// </summary>
        public string? NormalizedSKU => SKU?.Trim().ToUpperInvariant();

        /// <summary>
        /// Gets the trimmed description without extra whitespace
        /// </summary>
        public string? NormalizedDescription => Description?.Trim();

        /// <summary>
        /// Indicates if the product has description
        /// </summary>
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

        /// <summary>
        /// Indicates if the product has SKU
        /// </summary>
        public bool HasSKU => !string.IsNullOrWhiteSpace(SKU);

        /// <summary>
        /// Indicates if the product is initially in stock
        /// </summary>
        public bool IsInStock => StockQuantity > 0;

        /// <summary>
        /// Validates the command data according to business rules
        /// </summary>
        /// <returns>True if the command is valid according to domain rules</returns>
        public bool IsValid()
        {
            return IsNameValid() &&
                   IsPriceValid() &&
                   IsStockQuantityValid() &&
                   IsSKUValid() &&
                   IsDescriptionValid();
        }

        /// <summary>
        /// Validates product name according to business rules
        /// </summary>
        private bool IsNameValid()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   Name.Trim().Length >= 2 &&
                   Name.Length <= 200 &&
                   !Name.Trim().StartsWith(' ') &&
                   !Name.Trim().EndsWith(' ');
        }

        /// <summary>
        /// Validates price according to business rules
        /// </summary>
        private bool IsPriceValid()
        {
            return Price > 0 &&
                   Price <= 999999.99m &&
                   Math.Round(Price, 2) == Price; // Ensure max 2 decimal places
        }

        /// <summary>
        /// Validates stock quantity according to business rules
        /// </summary>
        private bool IsStockQuantityValid()
        {
            return StockQuantity >= 0;
        }

        /// <summary>
        /// Validates SKU format according to business rules
        /// </summary>
        private bool IsSKUValid()
        {
            if (string.IsNullOrWhiteSpace(SKU))
                return true; // SKU is optional

            var normalizedSKU = SKU.Trim().ToUpperInvariant();
            return normalizedSKU.Length <= 50 &&
                   normalizedSKU.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }

        /// <summary>
        /// Validates description according to business rules
        /// </summary>
        private bool IsDescriptionValid()
        {
            if (string.IsNullOrWhiteSpace(Description))
                return true; // Description is optional

            return Description.Length <= 1000;
        }

        /// <summary>
        /// Gets a domain-specific validation result with detailed error information
        /// </summary>
        /// <returns>Validation result with specific error messages</returns>
        public ProductValidationResult GetValidationResult()
        {
            var errors = new List<string>();

            if (!IsNameValid())
                errors.Add("Product name must be 2-200 characters and cannot start/end with spaces");

            if (!IsPriceValid())
                errors.Add("Price must be positive, up to 999,999.99, and have maximum 2 decimal places");

            if (!IsStockQuantityValid())
                errors.Add("Stock quantity must be non-negative");

            if (!IsSKUValid())
                errors.Add("SKU must be up to 50 characters and contain only letters, numbers, hyphens, and underscores");

            if (!IsDescriptionValid())
                errors.Add("Description cannot exceed 1000 characters");

            return new ProductValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Gets a safe string representation for logging purposes (excludes sensitive data)
        /// </summary>
        /// <returns>Safe string representation for logging and audit</returns>
        public override string ToString()
        {
            return $"CreateProductCommand(Name={NormalizedName}, Price={Price:C}, SKU={NormalizedSKU ?? "N/A"}, Stock={StockQuantity})";
        }
    }

    /// <summary>
    /// Represents the result of a product validation operation
    /// </summary>
    /// <param name="IsValid">Indicates if the validation passed</param>
    /// <param name="Errors">Collection of validation error messages</param>
    public sealed record ProductValidationResult(bool IsValid, IReadOnlyList<string> Errors);
}
