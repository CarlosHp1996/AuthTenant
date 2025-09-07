using AutoMapper;
using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Product;
using AuthTenant.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace AuthTenant.Application.Queries.Product.Handlers
{
    /// <summary>
    /// Handler for retrieving a single product by its unique identifier.
    /// Implements comprehensive error handling, performance optimization through caching,
    /// and flexible data inclusion based on query parameters.
    /// </summary>
    public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
    {
        private readonly IRepository<Domain.Entities.Product> _productRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetProductByIdHandler> _logger;
        private readonly IMemoryCache _cache;

        // Cache settings
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);
        private const string CacheKeyPrefix = "product_by_id";

        public GetProductByIdHandler(
            IRepository<Domain.Entities.Product> productRepository,
            IMapper mapper,
            ILogger<GetProductByIdHandler> logger,
            IMemoryCache cache)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Handles the GetProductByIdQuery request with comprehensive validation and optimization.
        /// </summary>
        /// <param name="request">The query request containing product ID and inclusion flags</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Result containing the requested product or error information</returns>
        public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate the request
                if (!request.IsValid())
                {
                    var errors = request.GetValidationErrors();
                    _logger.LogWarning("Invalid GetProductByIdQuery: {Errors}", string.Join(", ", errors));
                    return Result<ProductDto>.Failure(string.Join(", ", errors));
                }

                _logger.LogInformation("Processing GetProductByIdQuery for product {ProductId}", request.Id);

                // Check cache first (if not including dynamic data)
                var cacheKey = $"{CacheKeyPrefix}:{request.CacheKey}";
                if (_cache.TryGetValue(cacheKey, out ProductDto? cachedProduct) && cachedProduct != null)
                {
                    _logger.LogDebug("Product {ProductId} found in cache", request.Id);
                    return Result<ProductDto>.Success(cachedProduct);
                }

                // Retrieve product from repository
                var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);

                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found", request.Id);
                    return Result<ProductDto>.Failure($"Product with ID {request.Id} not found");
                }

                // Check if product is inactive and not explicitly requested
                if (!product.IsActive && !request.IncludeInactive)
                {
                    _logger.LogWarning("Attempt to access inactive product {ProductId} without permission", request.Id);
                    return Result<ProductDto>.Failure("Product not found"); // Don't expose that it exists but is inactive
                }

                // Map to DTO with conditional data inclusion
                var productDto = await MapToDto(product, request, cancellationToken);

                // Cache the result for future requests
                await CacheProductDto(cacheKey, productDto);

                _logger.LogInformation("Successfully retrieved product {ProductId}", request.Id);
                return Result<ProductDto>.Success(productDto);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("GetProductByIdQuery for product {ProductId} was cancelled", request.Id);
                return Result<ProductDto>.Failure("Operation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing GetProductByIdQuery for product {ProductId}", request.Id);
                return Result<ProductDto>.Failure("An error occurred while retrieving the product");
            }
        }

        /// <summary>
        /// Maps the product entity to DTO with conditional data inclusion.
        /// </summary>
        /// <param name="product">The product entity</param>
        /// <param name="request">The original query request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Mapped ProductDto</returns>
        private async Task<ProductDto> MapToDto(Domain.Entities.Product product, GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            // Start with basic mapping
            var productDto = _mapper.Map<ProductDto>(product);

            // Apply conditional data filtering based on request parameters
            if (!request.IncludeAuditInfo)
            {
                // Clear audit information
                productDto.CreatedAt = default;
                productDto.UpdatedAt = null;
                productDto.CreatedBy = null;
                productDto.UpdatedBy = null;
            }

            if (!request.IncludeRelatedData)
            {
                // Clear related data collections by assigning empty lists
                productDto.Categories = new List<string>();
                productDto.Tags = new List<string>();
            }

            await Task.CompletedTask;
            return productDto;
        }

        /// <summary>
        /// Loads related data for the product if not already loaded.
        /// </summary>
        /// <param name="product">The product entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task LoadRelatedData(Domain.Entities.Product product, CancellationToken cancellationToken)
        {
            try
            {
                // This would typically involve loading related entities
                // Implementation depends on the specific entity structure and relationships
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load related data for product {ProductId}", product.Id);
                // Continue without related data rather than failing the entire request
            }
        }

        /// <summary>
        /// Caches the product DTO for improved performance.
        /// </summary>
        /// <param name="cacheKey">The cache key</param>
        /// <param name="productDto">The product DTO to cache</param>
        private async Task CacheProductDto(string cacheKey, ProductDto productDto)
        {
            try
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheExpiration,
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, productDto, cacheOptions);

                _logger.LogDebug("Product {ProductId} cached with key {CacheKey}", productDto.Id, cacheKey);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache product {ProductId}", productDto.Id);
                // Continue without caching rather than failing the request
            }
        }

        /// <summary>
        /// Invalidates cache entries for a specific product.
        /// Should be called when the product is updated or deleted.
        /// </summary>
        /// <param name="productId">The product ID to invalidate</param>
        public void InvalidateCache(Guid productId)
        {
            try
            {
                // Remove all cache entries for this product
                // Note: This is a simplified approach. In production, you might want a more sophisticated cache invalidation strategy
                var keysToRemove = new[]
                {
                    $"{CacheKeyPrefix}:product:{productId}:inactive:False:audit:True:related:True",
                    $"{CacheKeyPrefix}:product:{productId}:inactive:False:audit:False:related:True",
                    $"{CacheKeyPrefix}:product:{productId}:inactive:False:audit:True:related:False",
                    $"{CacheKeyPrefix}:product:{productId}:inactive:False:audit:False:related:False",
                    $"{CacheKeyPrefix}:product:{productId}:inactive:True:audit:True:related:True",
                    $"{CacheKeyPrefix}:product:{productId}:inactive:True:audit:False:related:True",
                    $"{CacheKeyPrefix}:product:{productId}:inactive:True:audit:True:related:False",
                    $"{CacheKeyPrefix}:product:{productId}:inactive:True:audit:False:related:False"
                };

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }

                _logger.LogDebug("Invalidated cache for product {ProductId}", productId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for product {ProductId}", productId);
            }
        }

        /// <summary>
        /// Validates that the product can be accessed by the current user/tenant.
        /// </summary>
        /// <param name="product">The product to validate</param>
        /// <returns>True if access is allowed</returns>
        private static bool ValidateAccess(Domain.Entities.Product product)
        {
            // Implement tenant-specific access validation here
            // For now, return true as access control is handled at the repository level
            return product != null;
        }

        /// <summary>
        /// Gets performance metrics for monitoring.
        /// </summary>
        /// <returns>Performance metrics</returns>
        public async Task<object> GetPerformanceMetrics()
        {
            // This would typically return cache hit ratios, response times, etc.
            await Task.CompletedTask;
            return new
            {
                CacheExpiration = CacheExpiration.TotalMinutes,
                HandlerName = nameof(GetProductByIdHandler)
            };
        }
    }
}
