using System.Text;
using System.Text.Json.Serialization;

namespace AuthTenant.Application.Common
{
    /// <summary>
    /// Represents a paginated result set for efficient data retrieval and display.
    /// Implements comprehensive pagination logic with metadata for navigation and UI binding.
    /// Thread-safe and immutable design ensures reliability in concurrent environments.
    /// Supports various pagination scenarios including empty results and single-page collections.
    /// </summary>
    /// <typeparam name="T">The type of items in the paginated collection</typeparam>
    public sealed class PagedResult<T>
    {
        /// <summary>
        /// Gets the collection of items for the current page
        /// </summary>
        public List<T> Items { get; private set; } = new();

        /// <summary>
        /// Gets the current page number (1-based indexing)
        /// </summary>
        public int Page { get; private set; }

        /// <summary>
        /// Gets the number of items per page
        /// </summary>
        public int PageSize { get; private set; }

        /// <summary>
        /// Gets the total number of items across all pages
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Gets the total number of pages available
        /// </summary>
        public int TotalPages { get; private set; }

        /// <summary>
        /// Gets a value indicating whether there is a next page available
        /// </summary>
        [JsonIgnore]
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Gets a value indicating whether there is a previous page available
        /// </summary>
        [JsonIgnore]
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Gets a value indicating whether this is the first page
        /// </summary>
        [JsonIgnore]
        public bool IsFirstPage => Page == 1;

        /// <summary>
        /// Gets a value indicating whether this is the last page
        /// </summary>
        [JsonIgnore]
        public bool IsLastPage => Page == TotalPages || TotalPages == 0;

        /// <summary>
        /// Gets a value indicating whether the result set is empty
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => TotalCount == 0;

        /// <summary>
        /// Gets the number of items on the current page
        /// </summary>
        [JsonIgnore]
        public int CurrentPageItemCount => Items.Count;

        /// <summary>
        /// Gets the starting item number for the current page (1-based)
        /// </summary>
        [JsonIgnore]
        public int StartItemNumber => IsEmpty ? 0 : ((Page - 1) * PageSize) + 1;

        /// <summary>
        /// Gets the ending item number for the current page (1-based)
        /// </summary>
        [JsonIgnore]
        public int EndItemNumber => IsEmpty ? 0 : StartItemNumber + CurrentPageItemCount - 1;

        /// <summary>
        /// Gets the next page number (null if no next page)
        /// </summary>
        [JsonIgnore]
        public int? NextPage => HasNextPage ? Page + 1 : null;

        /// <summary>
        /// Gets the previous page number (null if no previous page)
        /// </summary>
        [JsonIgnore]
        public int? PreviousPage => HasPreviousPage ? Page - 1 : null;

        /// <summary>
        /// Gets pagination metadata for API responses
        /// </summary>
        [JsonIgnore]
        public PaginationMetadata Metadata => new PaginationMetadata
        {
            Page = Page,
            PageSize = PageSize,
            TotalCount = TotalCount,
            TotalPages = TotalPages,
            HasNextPage = HasNextPage,
            HasPreviousPage = HasPreviousPage,
            StartItemNumber = StartItemNumber,
            EndItemNumber = EndItemNumber
        };

        /// <summary>
        /// Private constructor to ensure objects are created through factory methods
        /// </summary>
        private PagedResult() { }

        /// <summary>
        /// Creates a new paginated result with the specified parameters
        /// </summary>
        /// <param name="items">The items for the current page</param>
        /// <param name="page">The current page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <param name="totalCount">The total number of items across all pages</param>
        /// <returns>A new PagedResult instance</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
        public static PagedResult<T> Create(
            IEnumerable<T> items,
            int page,
            int pageSize,
            int totalCount)
        {
            // Validate parameters
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (page < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(page));

            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

            if (totalCount < 0)
                throw new ArgumentException("Total count cannot be negative", nameof(totalCount));

            var itemsList = items.ToList();

            // Validate that items count doesn't exceed page size (except for last page)
            if (itemsList.Count > pageSize)
                throw new ArgumentException($"Items count ({itemsList.Count}) cannot exceed page size ({pageSize})", nameof(items));

            // Calculate total pages
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((decimal)totalCount / pageSize);

            // Validate that current page doesn't exceed total pages (unless empty result)
            if (totalCount > 0 && page > totalPages)
                throw new ArgumentException($"Page number ({page}) cannot exceed total pages ({totalPages})", nameof(page));

            return new PagedResult<T>
            {
                Items = itemsList,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        /// <summary>
        /// Creates an empty paginated result
        /// </summary>
        /// <param name="page">The current page number</param>
        /// <param name="pageSize">The page size</param>
        /// <returns>An empty PagedResult</returns>
        public static PagedResult<T> Empty(int page = 1, int pageSize = 10)
        {
            return Create(new List<T>(), page, pageSize, 0);
        }

        /// <summary>
        /// Creates a single-page result containing all items
        /// </summary>
        /// <param name="items">All items to include</param>
        /// <returns>A PagedResult containing all items on page 1</returns>
        public static PagedResult<T> SinglePage(IEnumerable<T> items)
        {
            var itemsList = items?.ToList() ?? new List<T>();
            return Create(itemsList, 1, Math.Max(itemsList.Count, 1), itemsList.Count);
        }

        /// <summary>
        /// Transforms the items in the paginated result using a mapping function
        /// </summary>
        /// <typeparam name="TResult">The target type for transformation</typeparam>
        /// <param name="mapper">Function to transform each item</param>
        /// <returns>A new PagedResult with transformed items</returns>
        /// <exception cref="ArgumentNullException">Thrown when mapper is null</exception>
        public PagedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            try
            {
                var mappedItems = Items.Select(mapper).ToList();
                return PagedResult<TResult>.Create(mappedItems, Page, PageSize, TotalCount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error mapping items: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Transforms the items in the paginated result using an async mapping function
        /// </summary>
        /// <typeparam name="TResult">The target type for transformation</typeparam>
        /// <param name="mapper">Async function to transform each item</param>
        /// <returns>A new PagedResult with transformed items</returns>
        public async Task<PagedResult<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            try
            {
                var mappingTasks = Items.Select(mapper);
                var mappedItems = await Task.WhenAll(mappingTasks);
                return PagedResult<TResult>.Create(mappedItems, Page, PageSize, TotalCount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error mapping items asynchronously: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Filters items in the current page using a predicate (does not affect pagination metadata)
        /// </summary>
        /// <param name="predicate">The filter predicate</param>
        /// <returns>A new PagedResult with filtered items</returns>
        /// <remarks>
        /// Note: This method filters items on the current page only and does not recalculate 
        /// pagination metadata. Use this carefully as it may result in pages with fewer items.
        /// </remarks>
        public PagedResult<T> Filter(Func<T, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var filteredItems = Items.Where(predicate).ToList();

            return new PagedResult<T>
            {
                Items = filteredItems,
                Page = Page,
                PageSize = PageSize,
                TotalCount = TotalCount, // Keep original total count
                TotalPages = TotalPages  // Keep original total pages
            };
        }

        /// <summary>
        /// Gets a range of page numbers for pagination UI (e.g., "1 2 3 ... 8 9 10")
        /// </summary>
        /// <param name="range">Number of pages to show around the current page</param>
        /// <returns>A collection of page numbers for display</returns>
        public IEnumerable<int> GetPageRange(int range = 2)
        {
            if (range < 0)
                throw new ArgumentException("Range must be non-negative", nameof(range));

            if (TotalPages == 0)
                return Enumerable.Empty<int>();

            var start = Math.Max(1, Page - range);
            var end = Math.Min(TotalPages, Page + range);

            return Enumerable.Range(start, end - start + 1);
        }

        /// <summary>
        /// Calculates the skip count for database queries based on current page
        /// </summary>
        /// <returns>Number of items to skip</returns>
        [JsonIgnore]
        public int SkipCount => (Page - 1) * PageSize;

        /// <summary>
        /// Validates the pagination parameters for consistency
        /// </summary>
        /// <returns>True if all parameters are consistent</returns>
        public bool IsValid()
        {
            if (Page < 1 || PageSize < 1 || TotalCount < 0)
                return false;

            if (TotalCount == 0)
                return TotalPages == 0 && Items.Count == 0;

            var expectedTotalPages = (int)Math.Ceiling((decimal)TotalCount / PageSize);
            if (TotalPages != expectedTotalPages)
                return false;

            if (Page > TotalPages)
                return false;

            // Check if items count is appropriate for the page
            var maxItemsForPage = Page == TotalPages
                ? TotalCount - ((TotalPages - 1) * PageSize)  // Last page can have fewer items
                : PageSize;  // Other pages should have full page size

            return Items.Count <= maxItemsForPage;
        }

        /// <summary>
        /// Provides a safe string representation for logging and debugging
        /// </summary>
        /// <returns>A formatted string with pagination information</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"PagedResult<{typeof(T).Name}>(");
            sb.Append($"Page: {Page}/{TotalPages}, ");
            sb.Append($"Items: {Items.Count}/{PageSize}, ");
            sb.Append($"Total: {TotalCount}");

            if (HasNextPage || HasPreviousPage)
            {
                sb.Append(", Navigation: ");
                if (HasPreviousPage) sb.Append("←");
                sb.Append("•");
                if (HasNextPage) sb.Append("→");
            }

            sb.Append(")");
            return sb.ToString();
        }

        /// <summary>
        /// Determines equality based on pagination state and items
        /// </summary>
        /// <param name="obj">Object to compare with</param>
        /// <returns>True if objects are equal</returns>
        public override bool Equals(object? obj)
        {
            if (obj is not PagedResult<T> other)
                return false;

            return Page == other.Page &&
                   PageSize == other.PageSize &&
                   TotalCount == other.TotalCount &&
                   Items.SequenceEqual(other.Items);
        }

        /// <summary>
        /// Gets the hash code for the paginated result
        /// </summary>
        /// <returns>Hash code value</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Page, PageSize, TotalCount, Items.Count);
        }
    }

    /// <summary>
    /// Pagination metadata for API responses and UI binding
    /// </summary>
    public sealed class PaginationMetadata
    {
        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Starting item number for current page
        /// </summary>
        public int StartItemNumber { get; set; }

        /// <summary>
        /// Ending item number for current page
        /// </summary>
        public int EndItemNumber { get; set; }
    }

    /// <summary>
    /// Extension methods for creating paginated results from queryable sources
    /// </summary>
    public static class PagedResultExtensions
    {
        /// <summary>
        /// Creates a paginated result from an IQueryable source
        /// </summary>
        /// <typeparam name="T">The type of items</typeparam>
        /// <param name="source">The queryable source</param>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The page size</param>
        /// <returns>A paginated result</returns>
        public static PagedResult<T> ToPagedResult<T>(
            this IQueryable<T> source,
            int page,
            int pageSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var totalCount = source.Count();
            var items = source
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return PagedResult<T>.Create(items, page, pageSize, totalCount);
        }

        /// <summary>
        /// Creates a paginated result from an IEnumerable source (in-memory pagination)
        /// </summary>
        /// <typeparam name="T">The type of items</typeparam>
        /// <param name="source">The enumerable source</param>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The page size</param>
        /// <returns>A paginated result</returns>
        public static PagedResult<T> ToPagedResult<T>(
            this IEnumerable<T> source,
            int page,
            int pageSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var sourceList = source.ToList();
            var totalCount = sourceList.Count;
            var items = sourceList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return PagedResult<T>.Create(items, page, pageSize, totalCount);
        }
    }
}
