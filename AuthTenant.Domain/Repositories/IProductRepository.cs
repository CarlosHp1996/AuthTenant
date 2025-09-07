using AuthTenant.Domain.Entities;
using AuthTenant.Domain.Interfaces;

namespace AuthTenant.Domain.Repositories;

/// <summary>
/// Repository interface for Product entity operations.
/// Extends the base repository with product-specific operations including 
/// search functionality and active product filtering.
/// </summary>
/// <remarks>
/// This interface follows the Repository pattern and provides a contract
/// for product data access operations. Implementations should handle
/// tenant isolation and soft delete filtering automatically.
/// </remarks>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Retrieves all active (non-deleted) products for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of active products</returns>
    /// <remarks>
    /// Products are considered active when IsDeleted = false.
    /// Results are automatically filtered by current tenant context.
    /// </remarks>
    Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name, description, or other searchable fields.
    /// </summary>
    /// <param name="searchTerm">Search term to match against product fields</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of products matching the search criteria</returns>
    /// <remarks>
    /// Search is case-insensitive and supports partial matches.
    /// Only active products are included in search results.
    /// Results are automatically filtered by current tenant context.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when searchTerm is null or empty</exception>
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves products with pagination support.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Paginated collection of products</returns>
    /// <remarks>
    /// Results are ordered by creation date (newest first) and filtered by tenant.
    /// Only active products are included in results.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when pageNumber or pageSize is invalid</exception>
    Task<IEnumerable<Product>> GetPagedProductsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves products by category or similar grouping criteria.
    /// </summary>
    /// <param name="category">Category identifier or name</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of products in the specified category</returns>
    /// <remarks>
    /// Category matching is case-insensitive.
    /// Only active products are included in results.
    /// Results are automatically filtered by current tenant context.
    /// </remarks>
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product with the specified name already exists for the current tenant.
    /// </summary>
    /// <param name="name">Product name to check</param>
    /// <param name="excludeId">Optional product ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if a product with the name exists, false otherwise</returns>
    /// <remarks>
    /// Name comparison is case-insensitive.
    /// Checks only active products within the current tenant.
    /// </remarks>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
