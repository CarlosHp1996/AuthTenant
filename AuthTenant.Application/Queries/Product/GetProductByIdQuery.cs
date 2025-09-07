using System.ComponentModel.DataAnnotations;

using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Product;

using MediatR;

namespace AuthTenant.Application.Queries.Product
{
    /// <summary>
    /// Query for retrieving a single product by its unique identifier.
    /// Implements CQRS pattern for read operations with comprehensive validation and error handling.
    /// Supports multi-tenant isolation and includes caching considerations.
    /// </summary>
    /// <param name="Id">The unique identifier of the product to retrieve</param>
    /// <param name="IncludeInactive">Whether to include inactive products in the search (default: false)</param>
    /// <param name="IncludeAuditInfo">Whether to include audit information in the response (default: true)</param>
    /// <param name="IncludeRelatedData">Whether to include related data like categories and tags (default: true)</param>
    public sealed record GetProductByIdQuery(
        [Required] Guid Id,
        bool IncludeInactive = false,
        bool IncludeAuditInfo = true,
        bool IncludeRelatedData = true
    ) : IRequest<Result<ProductDto>>
    {
        /// <summary>
        /// Gets a value indicating whether the query has a valid product ID.
        /// </summary>
        public bool HasValidId => Id != Guid.Empty;

        /// <summary>
        /// Gets a value indicating whether this is a query for basic product information only.
        /// Used for performance optimization when full details are not needed.
        /// </summary>
        public bool IsBasicQuery => !IncludeAuditInfo && !IncludeRelatedData;

        /// <summary>
        /// Gets the cache key for this query result.
        /// Used for implementing caching strategies.
        /// </summary>
        public string CacheKey => $"product:{Id}:inactive:{IncludeInactive}:audit:{IncludeAuditInfo}:related:{IncludeRelatedData}";

        /// <summary>
        /// Validates the query parameters.
        /// </summary>
        /// <returns>True if the query is valid</returns>
        public bool IsValid()
        {
            return Id != Guid.Empty;
        }

        /// <summary>
        /// Gets validation errors for the query.
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public IList<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (Id == Guid.Empty)
                errors.Add("Product ID cannot be empty");

            return errors;
        }

        /// <summary>
        /// Creates a basic query for retrieving minimal product information.
        /// Optimized for performance when full details are not needed.
        /// </summary>
        /// <param name="id">Product identifier</param>
        /// <returns>Configured query instance</returns>
        public static GetProductByIdQuery CreateBasic(Guid id)
        {
            return new GetProductByIdQuery(
                Id: id,
                IncludeInactive: false,
                IncludeAuditInfo: false,
                IncludeRelatedData: false
            );
        }

        /// <summary>
        /// Creates a comprehensive query for retrieving complete product information.
        /// Includes all available data fields and audit information.
        /// </summary>
        /// <param name="id">Product identifier</param>
        /// <param name="includeInactive">Whether to include inactive products</param>
        /// <returns>Configured query instance</returns>
        public static GetProductByIdQuery CreateDetailed(Guid id, bool includeInactive = false)
        {
            return new GetProductByIdQuery(
                Id: id,
                IncludeInactive: includeInactive,
                IncludeAuditInfo: true,
                IncludeRelatedData: true
            );
        }

        /// <summary>
        /// Creates a query specifically for administrative purposes.
        /// Includes inactive products and full audit trail.
        /// </summary>
        /// <param name="id">Product identifier</param>
        /// <returns>Configured query instance for admin use</returns>
        public static GetProductByIdQuery CreateForAdmin(Guid id)
        {
            return new GetProductByIdQuery(
                Id: id,
                IncludeInactive: true,
                IncludeAuditInfo: true,
                IncludeRelatedData: true
            );
        }

        /// <summary>
        /// Creates a query for testing purposes with default parameters.
        /// </summary>
        /// <param name="id">Product identifier</param>
        /// <returns>Configured query instance for testing</returns>
        public static GetProductByIdQuery CreateForTesting(Guid id)
        {
            return new GetProductByIdQuery(id);
        }
    }
}
