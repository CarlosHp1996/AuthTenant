using System.ComponentModel.DataAnnotations;
using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Product;
using MediatR;

namespace AuthTenant.Application.Queries.Product
{
    /// <summary>
    /// Query for retrieving a paginated list of products with advanced filtering capabilities.
    /// Implements CQRS pattern for read operations with comprehensive search, filtering, and sorting options.
    /// Supports multi-tenant isolation, performance optimization, and flexible result customization.
    /// </summary>
    /// <param name="Page">Page number (1-based indexing)</param>
    /// <param name="PageSize">Number of items per page (1-100)</param>
    /// <param name="SearchTerm">Search term for name, description, SKU, or tags</param>
    /// <param name="IsActive">Filter by active status (null = all, true = active only, false = inactive only)</param>
    /// <param name="Categories">Filter by specific categories</param>
    /// <param name="Tags">Filter by specific tags</param>
    /// <param name="MinPrice">Minimum price filter</param>
    /// <param name="MaxPrice">Maximum price filter</param>
    /// <param name="InStockOnly">Whether to return only products with stock > 0</param>
    /// <param name="LowStockOnly">Whether to return only products with low stock (below minimum level)</param>
    /// <param name="SortBy">Field to sort by</param>
    /// <param name="SortDirection">Sort direction (asc/desc)</param>
    /// <param name="IncludeAuditInfo">Whether to include audit information in results</param>
    /// <param name="IncludeRelatedData">Whether to include categories, tags, and metadata</param>
    public sealed record GetProductsQuery(
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        int Page = 1,

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        int PageSize = 10,

        string? SearchTerm = null,
        bool? IsActive = null,
        IList<string>? Categories = null,
        IList<string>? Tags = null,

        [Range(0, double.MaxValue, ErrorMessage = "Minimum price cannot be negative")]
        decimal? MinPrice = null,

        [Range(0, double.MaxValue, ErrorMessage = "Maximum price cannot be negative")]
        decimal? MaxPrice = null,

        bool InStockOnly = false,
        bool LowStockOnly = false,

        ProductSortField SortBy = ProductSortField.Name,
        SortDirection SortDirection = SortDirection.Ascending,

        bool IncludeAuditInfo = false,
        bool IncludeRelatedData = true
    ) : IRequest<Result<PagedResult<ProductDto>>>
    {
        /// <summary>
        /// Gets a value indicating whether any search filters are applied.
        /// </summary>
        public bool HasFilters => !string.IsNullOrWhiteSpace(SearchTerm) ||
                                 IsActive.HasValue ||
                                 Categories?.Any() == true ||
                                 Tags?.Any() == true ||
                                 MinPrice.HasValue ||
                                 MaxPrice.HasValue ||
                                 InStockOnly ||
                                 LowStockOnly;

        /// <summary>
        /// Gets a value indicating whether this is a simple paginated list without complex filtering.
        /// Used for performance optimization.
        /// </summary>
        public bool IsSimpleQuery => !HasFilters && SortBy == ProductSortField.Name && SortDirection == SortDirection.Ascending;

        /// <summary>
        /// Gets a value indicating whether price range filtering is applied.
        /// </summary>
        public bool HasPriceFilter => MinPrice.HasValue || MaxPrice.HasValue;

        /// <summary>
        /// Gets a value indicating whether stock-based filtering is applied.
        /// </summary>
        public bool HasStockFilter => InStockOnly || LowStockOnly;

        /// <summary>
        /// Gets a value indicating whether text-based search is applied.
        /// </summary>
        public bool HasTextSearch => !string.IsNullOrWhiteSpace(SearchTerm);

        /// <summary>
        /// Gets a value indicating whether category/tag filtering is applied.
        /// </summary>
        public bool HasCategoryOrTagFilter => Categories?.Any() == true || Tags?.Any() == true;

        /// <summary>
        /// Gets the normalized search term for consistent searching.
        /// </summary>
        public string? NormalizedSearchTerm => string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm.Trim().ToLowerInvariant();

        /// <summary>
        /// Gets the cache key for this query result.
        /// </summary>
        public string CacheKey
        {
            get
            {
                var keyParts = new List<string>
                {
                    $"products:page:{Page}",
                    $"size:{PageSize}",
                    $"search:{NormalizedSearchTerm ?? "none"}",
                    $"active:{IsActive?.ToString() ?? "all"}",
                    $"categories:{string.Join(",", Categories ?? Array.Empty<string>())}",
                    $"tags:{string.Join(",", Tags ?? Array.Empty<string>())}",
                    $"price:{MinPrice}-{MaxPrice}",
                    $"stock:{InStockOnly}-{LowStockOnly}",
                    $"sort:{SortBy}:{SortDirection}",
                    $"audit:{IncludeAuditInfo}",
                    $"related:{IncludeRelatedData}"
                };
                return string.Join(":", keyParts);
            }
        }

        /// <summary>
        /// Validates the query parameters.
        /// </summary>
        /// <returns>True if the query is valid</returns>
        public bool IsValid()
        {
            return Page > 0 &&
                   PageSize > 0 &&
                   PageSize <= 100 &&
                   (!MinPrice.HasValue || MinPrice >= 0) &&
                   (!MaxPrice.HasValue || MaxPrice >= 0) &&
                   (!MinPrice.HasValue || !MaxPrice.HasValue || MinPrice <= MaxPrice) &&
                   (!InStockOnly || !LowStockOnly); // Cannot filter for both in-stock and low-stock simultaneously
        }

        /// <summary>
        /// Gets validation errors for the query.
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public IList<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (Page <= 0)
                errors.Add("Page must be greater than 0");

            if (PageSize <= 0)
                errors.Add("Page size must be greater than 0");

            if (PageSize > 100)
                errors.Add("Page size cannot exceed 100");

            if (MinPrice.HasValue && MinPrice < 0)
                errors.Add("Minimum price cannot be negative");

            if (MaxPrice.HasValue && MaxPrice < 0)
                errors.Add("Maximum price cannot be negative");

            if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
                errors.Add("Minimum price cannot be greater than maximum price");

            if (InStockOnly && LowStockOnly)
                errors.Add("Cannot filter for both in-stock and low-stock simultaneously");

            return errors;
        }

        /// <summary>
        /// Creates a simple query for basic product listing without filters.
        /// Optimized for performance when complex filtering is not needed.
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Configured query instance</returns>
        public static GetProductsQuery CreateSimple(int page = 1, int pageSize = 10)
        {
            return new GetProductsQuery(
                Page: page,
                PageSize: pageSize,
                IncludeAuditInfo: false,
                IncludeRelatedData: false
            );
        }

        /// <summary>
        /// Creates a search query with text-based filtering.
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Configured query instance</returns>
        public static GetProductsQuery CreateSearch(string searchTerm, int page = 1, int pageSize = 10)
        {
            return new GetProductsQuery(
                Page: page,
                PageSize: pageSize,
                SearchTerm: searchTerm,
                SortBy: ProductSortField.Relevance // Sort by relevance for search results
            );
        }

        /// <summary>
        /// Creates a query for retrieving active products only.
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Configured query instance</returns>
        public static GetProductsQuery CreateActiveOnly(int page = 1, int pageSize = 10)
        {
            return new GetProductsQuery(
                Page: page,
                PageSize: pageSize,
                IsActive: true
            );
        }

        /// <summary>
        /// Creates a query for retrieving products within a specific price range.
        /// </summary>
        /// <param name="minPrice">Minimum price</param>
        /// <param name="maxPrice">Maximum price</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Configured query instance</returns>
        public static GetProductsQuery CreatePriceRange(decimal minPrice, decimal maxPrice, int page = 1, int pageSize = 10)
        {
            return new GetProductsQuery(
                Page: page,
                PageSize: pageSize,
                MinPrice: minPrice,
                MaxPrice: maxPrice,
                SortBy: ProductSortField.Price
            );
        }

        /// <summary>
        /// Creates a query for retrieving products by category.
        /// </summary>
        /// <param name="categories">Categories to filter by</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Configured query instance</returns>
        public static GetProductsQuery CreateByCategory(IList<string> categories, int page = 1, int pageSize = 10)
        {
            return new GetProductsQuery(
                Page: page,
                PageSize: pageSize,
                Categories: categories,
                IncludeRelatedData: true
            );
        }

        /// <summary>
        /// Creates a query for inventory management (low stock alerts).
        /// </summary>
        /// <param name="lowStockOnly">Filter for low stock items only</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Configured query instance</returns>
        public static GetProductsQuery CreateInventoryReport(bool lowStockOnly = true, int page = 1, int pageSize = 10)
        {
            return new GetProductsQuery(
                Page: page,
                PageSize: pageSize,
                LowStockOnly: lowStockOnly,
                SortBy: ProductSortField.StockQuantity,
                SortDirection: SortDirection.Ascending,
                IncludeAuditInfo: true
            );
        }

        /// <summary>
        /// Creates a comprehensive query for administrative purposes.
        /// Includes all data fields and supports complex filtering.
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Configured query instance</returns>
        public static GetProductsQuery CreateForAdmin(int page = 1, int pageSize = 10)
        {
            return new GetProductsQuery(
                Page: page,
                PageSize: pageSize,
                IncludeAuditInfo: true,
                IncludeRelatedData: true
            );
        }

        /// <summary>
        /// Creates a query for testing purposes with default parameters.
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Configured query instance</returns>
        public static GetProductsQuery CreateForTesting(int page = 1, int pageSize = 10)
        {
            return new GetProductsQuery(page, pageSize);
        }
    }

    /// <summary>
    /// Enumeration of available sort fields for product queries.
    /// </summary>
    public enum ProductSortField
    {
        /// <summary>Sort by product name</summary>
        Name,
        /// <summary>Sort by price</summary>
        Price,
        /// <summary>Sort by creation date</summary>
        CreatedAt,
        /// <summary>Sort by last update date</summary>
        UpdatedAt,
        /// <summary>Sort by stock quantity</summary>
        StockQuantity,
        /// <summary>Sort by SKU</summary>
        SKU,
        /// <summary>Sort by search relevance (for search queries)</summary>
        Relevance
    }

    /// <summary>
    /// Enumeration of sort directions.
    /// </summary>
    public enum SortDirection
    {
        /// <summary>Ascending order</summary>
        Ascending,
        /// <summary>Descending order</summary>
        Descending
    }
}
