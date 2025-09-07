using AuthTenant.Application.Common;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AuthTenant.Application.Commands.Product
{
    /// <summary>
    /// Command for deleting products from the system.
    /// Represents a request to remove a product with business validation and domain rules.
    /// Follows DDD Command pattern and Clean Architecture principles for product deletion.
    /// </summary>
    /// <param name="Id">Product unique identifier (required, must be valid GUID)</param>
    public sealed record DeleteProductCommand(
        [Required(ErrorMessage = "Product ID is required")]
        Guid Id
    ) : IRequest<Result<bool>>
    {
        /// <summary>
        /// Indicates if the command has a valid product ID
        /// </summary>
        public bool HasValidId => Id != Guid.Empty;

        /// <summary>
        /// Gets the product ID as string for logging purposes
        /// </summary>
        public string ProductIdString => Id.ToString();

        /// <summary>
        /// Validates the command data according to business rules
        /// </summary>
        /// <returns>True if the command is valid according to domain rules</returns>
        public bool IsValid()
        {
            return IsProductIdValid();
        }

        /// <summary>
        /// Validates product ID according to business rules
        /// </summary>
        private bool IsProductIdValid()
        {
            return Id != Guid.Empty;
        }

        /// <summary>
        /// Gets a domain-specific validation result with detailed error information
        /// </summary>
        /// <returns>Validation result with specific error messages</returns>
        public DeleteProductValidationResult GetValidationResult()
        {
            var errors = new List<string>();

            if (!IsProductIdValid())
                errors.Add("Product ID must be a valid non-empty GUID");

            return new DeleteProductValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Gets a safe string representation for logging purposes
        /// </summary>
        /// <returns>Safe string representation for logging and audit</returns>
        public override string ToString()
        {
            return $"DeleteProductCommand(Id={ProductIdString})";
        }
    }

    /// <summary>
    /// Represents the result of a delete product validation operation
    /// </summary>
    /// <param name="IsValid">Indicates if the validation passed</param>
    /// <param name="Errors">Collection of validation error messages</param>
    public sealed record DeleteProductValidationResult(bool IsValid, IReadOnlyList<string> Errors);
}
