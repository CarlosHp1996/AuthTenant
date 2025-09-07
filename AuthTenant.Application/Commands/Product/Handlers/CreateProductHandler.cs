using AutoMapper;
using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Product;
using AuthTenant.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Application.Commands.Product.Handlers
{
    /// <summary>
    /// Handler for processing product creation commands in a multi-tenant environment.
    /// Implements Clean Architecture and DDD principles for product creation business logic.
    /// Follows SOLID principles with single responsibility and dependency inversion.
    /// </summary>
    public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
    {
        private readonly IRepository<Domain.Entities.Product> _productRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateProductHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the CreateProductHandler with required dependencies
        /// </summary>
        /// <param name="productRepository">Repository for product operations</param>
        /// <param name="mapper">AutoMapper instance for object mapping</param>
        /// <param name="logger">Logger instance for diagnostic information</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
        public CreateProductHandler(
            IRepository<Domain.Entities.Product> productRepository,
            IMapper mapper,
            ILogger<CreateProductHandler> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the product creation command following domain-driven design principles
        /// </summary>
        /// <param name="request">The product creation command containing product data</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Result containing product DTO or failure message</returns>
        public async Task<Result<ProductDto>> Handle(
            CreateProductCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation(
                "🛍️ Processing product creation request: {ProductName}, Price: {Price}, SKU: {SKU}",
                request.NormalizedName,
                request.Price,
                request.NormalizedSKU ?? "N/A"
            );

            try
            {
                // Domain validation: Validate command data according to business rules
                var validationResult = await ValidateProductCreationRequestAsync(request, cancellationToken);
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "❌ Product creation validation failed for: {ProductName}. Reason: {Reason}",
                        request.NormalizedName,
                        validationResult.Error
                    );
                    return validationResult;
                }

                // Domain logic: Check for duplicate products
                var duplicateCheckResult = await CheckForDuplicateProductAsync(request, cancellationToken);
                if (!duplicateCheckResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "🔄 Duplicate product check failed for: {ProductName}. Reason: {Reason}",
                        request.NormalizedName,
                        duplicateCheckResult.Error
                    );
                    return duplicateCheckResult;
                }

                // Domain entity creation: Create product according to domain rules
                var product = CreateProductEntity(request);

                // Infrastructure operation: Persist product
                var creationResult = await CreateProductAsync(product, cancellationToken);
                if (!creationResult.IsSuccess)
                {
                    _logger.LogError(
                        "🚫 Product creation failed for: {ProductName}. Error: {Error}",
                        request.NormalizedName,
                        creationResult.Error
                    );
                    return creationResult;
                }

                // Post-creation operations
                await PerformPostCreationOperationsAsync(product, cancellationToken);

                // Generate response DTO
                var productDto = GenerateProductResponse(product);

                _logger.LogInformation(
                    "✅ Product created successfully: {ProductName} with ID: {ProductId}",
                    request.NormalizedName,
                    product.Id
                );

                return Result<ProductDto>.Success(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Unexpected error during product creation for: {ProductName}",
                    request.NormalizedName
                );
                return Result<ProductDto>.Failure("An unexpected error occurred during product creation");
            }
        }

        /// <summary>
        /// Validates the product creation request according to domain business rules
        /// </summary>
        /// <param name="request">The product creation command to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        private async Task<Result<ProductDto>> ValidateProductCreationRequestAsync(
            CreateProductCommand request,
            CancellationToken cancellationToken)
        {
            // Domain validation using command's business rules
            var validationResult = request.GetValidationResult();
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                return Result<ProductDto>.Failure($"Validation failed: {errorMessage}");
            }

            // Additional domain-specific validations can be added here
            await Task.CompletedTask; // Placeholder for future async validations

            return Result<ProductDto>.Success(null!);
        }

        /// <summary>
        /// Checks for duplicate products based on business rules
        /// </summary>
        /// <param name="request">The product creation command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating if product can be created</returns>
        private async Task<Result<ProductDto>> CheckForDuplicateProductAsync(
            CreateProductCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Check for duplicate by name (case-insensitive)
                var existingByName = await _productRepository.FindAsync(
                    p => p.Name.ToLower() == request.NormalizedName.ToLower(),
                    cancellationToken);

                if (existingByName.Any())
                {
                    return Result<ProductDto>.Failure("A product with this name already exists");
                }

                // Check for duplicate by SKU if provided
                if (request.HasSKU)
                {
                    var existingBySKU = await _productRepository.FindAsync(
                        p => p.SKU != null && p.SKU.ToUpper() == request.NormalizedSKU!.ToUpper(),
                        cancellationToken);

                    if (existingBySKU.Any())
                    {
                        return Result<ProductDto>.Failure("A product with this SKU already exists");
                    }
                }

                return Result<ProductDto>.Success(null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for duplicate products: {ProductName}", request.NormalizedName);
                return Result<ProductDto>.Failure("Error validating product uniqueness");
            }
        }

        /// <summary>
        /// Creates a Product entity according to domain rules
        /// </summary>
        /// <param name="request">The product creation command</param>
        /// <returns>Configured Product entity</returns>
        private static Domain.Entities.Product CreateProductEntity(CreateProductCommand request)
        {
            return new Domain.Entities.Product
            {
                Name = request.NormalizedName,
                Description = request.NormalizedDescription,
                Price = Math.Round(request.Price, 2), // Ensure proper decimal precision
                SKU = request.NormalizedSKU,
                StockQuantity = request.StockQuantity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates the product in the repository with proper error handling
        /// </summary>
        /// <param name="product">The product entity to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the creation operation</returns>
        private async Task<Result<ProductDto>> CreateProductAsync(
            Domain.Entities.Product product,
            CancellationToken cancellationToken)
        {
            try
            {
                var createdProduct = await _productRepository.AddAsync(product, cancellationToken);

                if (createdProduct == null)
                {
                    return Result<ProductDto>.Failure("Failed to create product - repository returned null");
                }

                _logger.LogDebug(
                    "Product entity created successfully in repository: {ProductId}",
                    createdProduct.Id
                );

                // Update the original product reference with the created entity data
                product.Id = createdProduct.Id;
                product.CreatedAt = createdProduct.CreatedAt;

                return Result<ProductDto>.Success(null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product in repository: {ProductName}", product.Name);
                return Result<ProductDto>.Failure($"Error creating product: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs post-creation operations (indexing, notifications, etc.)
        /// </summary>
        /// <param name="product">The created product</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task PerformPostCreationOperationsAsync(
            Domain.Entities.Product product,
            CancellationToken cancellationToken)
        {
            try
            {
                // Future implementations:
                // - Update search index
                // - Send product creation notifications
                // - Trigger inventory management
                // - Log business events
                // - Update analytics

                _logger.LogDebug(
                    "Post-creation operations completed for product: {ProductId}",
                    product.Id
                );

                await Task.CompletedTask; // Placeholder for future async operations
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Non-critical error in post-creation operations for product: {ProductId}",
                    product.Id
                );
                // Non-critical errors shouldn't fail the creation process
            }
        }

        /// <summary>
        /// Generates the product response DTO
        /// </summary>
        /// <param name="product">The created product entity</param>
        /// <returns>Product DTO for response</returns>
        private ProductDto GenerateProductResponse(Domain.Entities.Product product)
        {
            try
            {
                var productDto = _mapper.Map<ProductDto>(product);

                if (productDto == null)
                {
                    throw new InvalidOperationException("Mapper returned null for ProductDto");
                }

                return productDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error mapping product to DTO: {ProductId}",
                    product.Id
                );
                throw;
            }
        }
    }
}
