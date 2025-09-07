using System.ComponentModel.DataAnnotations;
using System.Text;

using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Product;

using MediatR;

namespace AuthTenant.Application.Commands.Product
{
    /// <summary>
    /// Command for updating an existing product in a multi-tenant e-commerce environment.
    /// Implements comprehensive validation, domain logic, and follows CQRS patterns.
    /// Supports partial updates while maintaining data integrity and business rules.
    /// </summary>
    /// <param name="Id">The unique identifier of the product to update (required)</param>
    /// <param name="Name">The updated product name (required, 1-200 characters)</param>
    /// <param name="Description">The updated product description (optional, max 2000 characters)</param>
    /// <param name="Price">The updated product price (required, must be positive)</param>
    /// <param name="SKU">The updated product SKU (optional, max 50 characters)</param>
    /// <param name="StockQuantity">The updated stock quantity (required, must be non-negative)</param>
    /// <param name="IsActive">Whether the product should be active or inactive</param>
    public sealed record UpdateProductCommand(
        [Required(ErrorMessage = "Product ID is required")]
        Guid Id,

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Product name must be between 1 and 200 characters")]
        string Name,

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        string? Description,

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        decimal Price,

        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        string? SKU,

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be non-negative")]
        int StockQuantity,

        bool IsActive
    ) : IRequest<Result<ProductDto>>
    {
        /// <summary>
        /// Gets a safe string representation of the product ID for logging purposes
        /// </summary>
        public string ProductIdString => Id != Guid.Empty ? Id.ToString()[..8] + "..." : "Invalid";

        /// <summary>
        /// Validates the command data according to business rules and data constraints
        /// </summary>
        /// <returns>True if the command is valid; otherwise, false</returns>
        public bool IsValid()
        {
            // Basic null and empty validations
            if (Id == Guid.Empty) return false;
            if (string.IsNullOrWhiteSpace(Name)) return false;
            if (Price <= 0) return false;
            if (StockQuantity < 0) return false;

            // Length validations
            if (Name.Length > 200) return false;
            if (!string.IsNullOrEmpty(Description) && Description.Length > 2000) return false;
            if (!string.IsNullOrEmpty(SKU) && SKU.Length > 50) return false;

            // Business rule validations
            if (Name.Trim().Length == 0) return false;

            return true;
        }

        /// <summary>
        /// Gets detailed validation results with specific error messages for each validation failure
        /// </summary>
        /// <returns>Validation result containing success status and error messages</returns>
        public ValidationResult GetValidationResult()
        {
            var errors = new List<string>();

            // Required field validations
            if (Id == Guid.Empty)
                errors.Add("Product ID cannot be empty");

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Product name is required");

            if (Price <= 0)
                errors.Add("Price must be greater than 0");

            if (StockQuantity < 0)
                errors.Add("Stock quantity must be non-negative");

            // Length validations
            if (!string.IsNullOrEmpty(Name))
            {
                if (Name.Length > 200)
                    errors.Add("Product name cannot exceed 200 characters");
                else if (Name.Trim().Length == 0)
                    errors.Add("Product name cannot be empty or only whitespace");
            }

            if (!string.IsNullOrEmpty(Description) && Description.Length > 2000)
                errors.Add("Description cannot exceed 2000 characters");

            if (!string.IsNullOrEmpty(SKU) && SKU.Length > 50)
                errors.Add("SKU cannot exceed 50 characters");

            // Business rule validations
            if (Price > 1000000)
                errors.Add("Price cannot exceed 1,000,000");

            if (StockQuantity > 1000000)
                errors.Add("Stock quantity cannot exceed 1,000,000");

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Provides a safe string representation of the command for logging and debugging.
        /// Excludes sensitive information and truncates long values.
        /// </summary>
        /// <returns>A formatted string representation safe for logging</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("UpdateProduct(");
            sb.Append($"ID: {ProductIdString}, ");
            sb.Append($"Name: '{Name?.Substring(0, Math.Min(Name?.Length ?? 0, 30))}{(Name?.Length > 30 ? "..." : "")}', ");
            sb.Append($"Price: {Price:C}, ");
            sb.Append($"Stock: {StockQuantity}, ");
            sb.Append($"Active: {IsActive}");
            sb.Append(')');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents the result of command validation with detailed error information
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of validation error messages
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
}
