using AutoMapper;

using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Product;
using AuthTenant.Domain.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace AuthTenant.Application.Commands.Product.Handlers
{
    /// <summary>
    /// Handler for processing product update commands in a multi-tenant e-commerce environment.
    /// Implements Clean Architecture and DDD principles for product update business logic.
    /// Follows SOLID principles with comprehensive validation, logging, and error handling.
    /// </summary>
    public sealed class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
    {
        private readonly IRepository<Domain.Entities.Product> _productRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateProductHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the UpdateProductHandler with required dependencies
        /// </summary>
        /// <param name="productRepository">Repository for product operations</param>
        /// <param name="mapper">AutoMapper instance for object mapping</param>
        /// <param name="logger">Logger instance for diagnostic information</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
        public UpdateProductHandler(
            IRepository<Domain.Entities.Product> productRepository,
            IMapper mapper,
            ILogger<UpdateProductHandler> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the product update command following domain-driven design principles
        /// </summary>
        /// <param name="request">The product update command containing updated data</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Result containing updated product DTO or error information</returns>
        public async Task<Result<ProductDto>> Handle(
            UpdateProductCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation(
                "🔄 Processing product update request for ID: {ProductId}",
                request.ProductIdString
            );

            try
            {
                // Domain validation: Validate command data according to business rules
                var validationResult = await ValidateUpdateRequestAsync(request, cancellationToken);
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "❌ Product update validation failed for ID: {ProductId}. Reason: {Reason}",
                        request.ProductIdString,
                        validationResult.Error
                    );
                    return validationResult;
                }

                // Domain logic: Find existing product
                var existingProduct = await FindProductAsync(request.Id, cancellationToken);
                if (existingProduct == null)
                {
                    _logger.LogWarning(
                        "🔍 Product not found for update: {ProductId}",
                        request.ProductIdString
                    );
                    return Result<ProductDto>.Failure("Product not found");
                }

                // Log current state for audit
                _logger.LogDebug(
                    "📋 Current product state - Name: {CurrentName}, Price: {CurrentPrice}, Active: {CurrentActive}",
                    existingProduct.Name,
                    existingProduct.Price,
                    existingProduct.IsActive
                );

                // Domain logic: Validate business rules for update
                var businessValidationResult = await ValidateBusinessRulesAsync(request, existingProduct, cancellationToken);
                if (!businessValidationResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "🚫 Business rule validation failed for product update: {ProductId}. Reason: {Reason}",
                        request.ProductIdString,
                        businessValidationResult.Error
                    );
                    return businessValidationResult;
                }

                // Perform pre-update operations
                await PerformPreUpdateOperationsAsync(request, existingProduct, cancellationToken);

                // Domain logic: Apply updates to product entity
                var updateResult = await ApplyUpdatesToProductAsync(request, existingProduct, cancellationToken);
                if (!updateResult.IsSuccess)
                {
                    _logger.LogError(
                        "💥 Failed to apply updates to product: {ProductId}. Error: {Error}",
                        request.ProductIdString,
                        updateResult.Error
                    );
                    return updateResult;
                }

                // Infrastructure operation: Save changes
                var saveResult = await SaveProductChangesAsync(existingProduct, cancellationToken);
                if (!saveResult.IsSuccess)
                {
                    _logger.LogError(
                        "💾 Failed to save product changes: {ProductId}. Error: {Error}",
                        request.ProductIdString,
                        saveResult.Error
                    );
                    return saveResult;
                }

                // Perform post-update operations
                await PerformPostUpdateOperationsAsync(existingProduct, cancellationToken);

                // Map to DTO for response
                var productDto = _mapper.Map<ProductDto>(existingProduct);

                _logger.LogInformation(
                    "✅ Product updated successfully: {ProductId} (Name: {ProductName})",
                    request.ProductIdString,
                    existingProduct.Name
                );

                return Result<ProductDto>.Success(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Unexpected error during product update for ID: {ProductId}",
                    request.ProductIdString
                );
                return Result<ProductDto>.Failure("An unexpected error occurred during product update");
            }
        }

        /// <summary>
        /// Validates the product update request according to domain business rules
        /// </summary>
        /// <param name="request">The update command to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        private async Task<Result<ProductDto>> ValidateUpdateRequestAsync(
            UpdateProductCommand request,
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
        /// Finds a product by ID with proper error handling
        /// </summary>
        /// <param name="productId">The product ID to find</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product entity or null if not found</returns>
        private async Task<Domain.Entities.Product?> FindProductAsync(
            Guid productId,
            CancellationToken cancellationToken)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(productId, cancellationToken);

                if (product != null)
                {
                    _logger.LogDebug(
                        "Product found for update: {ProductId} (Name: {ProductName})",
                        productId,
                        product.Name
                    );
                }

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding product for update: {ProductId}", productId);
                return null;
            }
        }

        /// <summary>
        /// Validates business rules for product update operations
        /// </summary>
        /// <param name="request">The update request</param>
        /// <param name="existingProduct">The current product entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating if business rules are satisfied</returns>
        private async Task<Result<ProductDto>> ValidateBusinessRulesAsync(
            UpdateProductCommand request,
            Domain.Entities.Product existingProduct,
            CancellationToken cancellationToken)
        {
            try
            {
                // Business rule: Check if SKU is unique (if changed)
                if (!string.IsNullOrEmpty(request.SKU) &&
                    !string.Equals(request.SKU, existingProduct.SKU, StringComparison.OrdinalIgnoreCase))
                {
                    // Future implementation: Check SKU uniqueness in repository
                    _logger.LogDebug("SKU changed from {OldSKU} to {NewSKU}", existingProduct.SKU, request.SKU);
                }

                // Business rule: Price change validation
                if (request.Price != existingProduct.Price)
                {
                    var priceChangePercentage = Math.Abs((request.Price - existingProduct.Price) / existingProduct.Price) * 100;
                    if (priceChangePercentage > 50)
                    {
                        _logger.LogWarning(
                            "Large price change detected: {OldPrice} -> {NewPrice} ({Percentage:F1}%)",
                            existingProduct.Price,
                            request.Price,
                            priceChangePercentage
                        );
                        // Future: Could require additional approval for large price changes
                    }
                }

                // Business rule: Stock quantity validation
                if (request.StockQuantity < existingProduct.StockQuantity)
                {
                    _logger.LogInformation(
                        "Stock quantity reduced from {OldStock} to {NewStock}",
                        existingProduct.StockQuantity,
                        request.StockQuantity
                    );
                    // Future: Could validate against pending orders
                }

                await Task.CompletedTask; // Placeholder for future async validations

                return Result<ProductDto>.Success(null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating business rules for product update: {ProductId}", existingProduct.Id);
                return Result<ProductDto>.Failure("Error validating business rules");
            }
        }

        /// <summary>
        /// Performs pre-update operations (validation, notifications, etc.)
        /// </summary>
        /// <param name="request">The update request</param>
        /// <param name="existingProduct">The current product entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task PerformPreUpdateOperationsAsync(
            UpdateProductCommand request,
            Domain.Entities.Product existingProduct,
            CancellationToken cancellationToken)
        {
            try
            {
                // Future implementations:
                // - Create audit log entry
                // - Send pre-update notifications
                // - Cache invalidation preparation
                // - Integration event preparation

                _logger.LogDebug(
                    "Pre-update operations completed for product: {ProductId}",
                    existingProduct.Id
                );

                await Task.CompletedTask; // Placeholder for future async operations
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Non-critical error in pre-update operations for product: {ProductId}",
                    existingProduct.Id
                );
                // Non-critical errors shouldn't fail the update process
            }
        }

        /// <summary>
        /// Applies the update command data to the product entity
        /// </summary>
        /// <param name="request">The update command</param>
        /// <param name="product">The product entity to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the update operation</returns>
        private async Task<Result<ProductDto>> ApplyUpdatesToProductAsync(
            UpdateProductCommand request,
            Domain.Entities.Product product,
            CancellationToken cancellationToken)
        {
            try
            {
                // Store original values for audit
                var originalName = product.Name;
                var originalPrice = product.Price;
                var originalIsActive = product.IsActive;

                // Apply updates to product properties
                product.Name = request.Name.Trim();
                product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
                product.Price = request.Price;
                product.SKU = string.IsNullOrWhiteSpace(request.SKU) ? null : request.SKU.Trim();
                product.StockQuantity = request.StockQuantity;
                product.IsActive = request.IsActive;

                // Log significant changes
                if (originalName != product.Name)
                    _logger.LogInformation("Product name changed: '{OldName}' -> '{NewName}'", originalName, product.Name);

                if (originalPrice != product.Price)
                    _logger.LogInformation("Product price changed: {OldPrice:C} -> {NewPrice:C}", originalPrice, product.Price);

                if (originalIsActive != product.IsActive)
                    _logger.LogInformation("Product status changed: {OldStatus} -> {NewStatus}",
                        originalIsActive ? "Active" : "Inactive",
                        product.IsActive ? "Active" : "Inactive");

                await Task.CompletedTask; // Placeholder for future async operations

                return Result<ProductDto>.Success(null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying updates to product: {ProductId}", product.Id);
                return Result<ProductDto>.Failure($"Error applying updates: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the product changes to the repository
        /// </summary>
        /// <param name="product">The updated product entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the save operation</returns>
        private async Task<Result<ProductDto>> SaveProductChangesAsync(
            Domain.Entities.Product product,
            CancellationToken cancellationToken)
        {
            try
            {
                await _productRepository.UpdateAsync(product, cancellationToken);

                _logger.LogDebug(
                    "Product changes saved successfully: {ProductId}",
                    product.Id
                );

                return Result<ProductDto>.Success(null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving product changes: {ProductId}", product.Id);
                return Result<ProductDto>.Failure($"Error saving changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs post-update operations (notifications, cache updates, etc.)
        /// </summary>
        /// <param name="product">The updated product entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task PerformPostUpdateOperationsAsync(
            Domain.Entities.Product product,
            CancellationToken cancellationToken)
        {
            try
            {
                // Future implementations:
                // - Clear caches
                // - Send update notifications
                // - Update search indexes
                // - Trigger domain events
                // - Update analytics
                // - Integration events

                _logger.LogDebug(
                    "Post-update operations completed for product: {ProductId}",
                    product.Id
                );

                await Task.CompletedTask; // Placeholder for future async operations
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Non-critical error in post-update operations for product: {ProductId}",
                    product.Id
                );
                // Non-critical errors are logged but don't affect the update result
            }
        }
    }
}
