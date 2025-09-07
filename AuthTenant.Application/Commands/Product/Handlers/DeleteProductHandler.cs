using AuthTenant.Application.Common;
using AuthTenant.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Application.Commands.Product.Handlers
{
    /// <summary>
    /// Handler for processing product deletion commands in a multi-tenant e-commerce environment.
    /// Implements Clean Architecture and DDD principles for product deletion business logic.
    /// Follows SOLID principles with single responsibility and dependency inversion.
    /// </summary>
    public sealed class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
    {
        private readonly IRepository<Domain.Entities.Product> _productRepository;
        private readonly ILogger<DeleteProductHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the DeleteProductHandler with required dependencies
        /// </summary>
        /// <param name="productRepository">Repository for product operations</param>
        /// <param name="logger">Logger instance for diagnostic information</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
        public DeleteProductHandler(
            IRepository<Domain.Entities.Product> productRepository,
            ILogger<DeleteProductHandler> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the product deletion command following domain-driven design principles
        /// </summary>
        /// <param name="request">The product deletion command containing product ID</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Result indicating success or failure of deletion operation</returns>
        public async Task<Result<bool>> Handle(
            DeleteProductCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation(
                "🗑️ Processing product deletion request for ID: {ProductId}",
                request.ProductIdString
            );

            try
            {
                // Domain validation: Validate command data according to business rules
                var validationResult = await ValidateDeleteRequestAsync(request, cancellationToken);
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "❌ Product deletion validation failed for ID: {ProductId}. Reason: {Reason}",
                        request.ProductIdString,
                        validationResult.Error
                    );
                    return validationResult;
                }

                // Domain logic: Find product to ensure it exists
                var product = await FindProductAsync(request.Id, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning(
                        "🔍 Product not found for deletion: {ProductId}",
                        request.ProductIdString
                    );
                    return Result<bool>.Failure("Product not found");
                }

                // Domain logic: Check if product can be deleted (business rules)
                var canDeleteResult = await ValidateProductCanBeDeletedAsync(product, cancellationToken);
                if (!canDeleteResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "🚫 Product cannot be deleted: {ProductId}. Reason: {Reason}",
                        request.ProductIdString,
                        canDeleteResult.Error
                    );
                    return canDeleteResult;
                }

                // Perform pre-deletion operations
                await PerformPreDeletionOperationsAsync(product, cancellationToken);

                // Infrastructure operation: Delete product
                var deletionResult = await DeleteProductAsync(product, cancellationToken);
                if (!deletionResult.IsSuccess)
                {
                    _logger.LogError(
                        "💥 Product deletion failed for ID: {ProductId}. Error: {Error}",
                        request.ProductIdString,
                        deletionResult.Error
                    );
                    return deletionResult;
                }

                // Perform post-deletion operations
                await PerformPostDeletionOperationsAsync(product, cancellationToken);

                _logger.LogInformation(
                    "✅ Product deleted successfully: {ProductId} (Name: {ProductName})",
                    request.ProductIdString,
                    product.Name
                );

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Unexpected error during product deletion for ID: {ProductId}",
                    request.ProductIdString
                );
                return Result<bool>.Failure("An unexpected error occurred during product deletion");
            }
        }

        /// <summary>
        /// Validates the product deletion request according to domain business rules
        /// </summary>
        /// <param name="request">The delete command to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        private async Task<Result<bool>> ValidateDeleteRequestAsync(
            DeleteProductCommand request,
            CancellationToken cancellationToken)
        {
            // Domain validation using command's business rules
            var validationResult = request.GetValidationResult();
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                return Result<bool>.Failure($"Validation failed: {errorMessage}");
            }

            // Additional domain-specific validations can be added here
            await Task.CompletedTask; // Placeholder for future async validations

            return Result<bool>.Success(true);
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
                        "Product found for deletion: {ProductId} (Name: {ProductName})",
                        productId,
                        product.Name
                    );
                }

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding product for deletion: {ProductId}", productId);
                return null;
            }
        }

        /// <summary>
        /// Validates if the product can be deleted according to business rules
        /// </summary>
        /// <param name="product">The product to validate for deletion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating if product can be deleted</returns>
        private async Task<Result<bool>> ValidateProductCanBeDeletedAsync(
            Domain.Entities.Product product,
            CancellationToken cancellationToken)
        {
            try
            {
                // Business rule: Check if product is already inactive
                if (!product.IsActive)
                {
                    _logger.LogInformation(
                        "Product is already inactive: {ProductId}",
                        product.Id
                    );
                    // Allow deletion of inactive products
                }

                // Future business rules:
                // - Check if product has pending orders
                // - Check if product is referenced in other entities
                // - Check user permissions for deletion
                // - Check if product is part of active promotions

                await Task.CompletedTask; // Placeholder for future async validations

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating product deletion permissions: {ProductId}", product.Id);
                return Result<bool>.Failure("Error validating deletion permissions");
            }
        }

        /// <summary>
        /// Performs pre-deletion operations (archiving, notifications, etc.)
        /// </summary>
        /// <param name="product">The product to be deleted</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task PerformPreDeletionOperationsAsync(
            Domain.Entities.Product product,
            CancellationToken cancellationToken)
        {
            try
            {
                // Future implementations:
                // - Archive product data for audit
                // - Notify interested parties
                // - Remove from search indexes
                // - Update related inventory records
                // - Log business events

                _logger.LogDebug(
                    "Pre-deletion operations completed for product: {ProductId}",
                    product.Id
                );

                await Task.CompletedTask; // Placeholder for future async operations
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Non-critical error in pre-deletion operations for product: {ProductId}",
                    product.Id
                );
                // Non-critical errors shouldn't fail the deletion process
            }
        }

        /// <summary>
        /// Deletes the product from the repository with proper error handling
        /// </summary>
        /// <param name="product">The product entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the deletion operation</returns>
        private async Task<Result<bool>> DeleteProductAsync(
            Domain.Entities.Product product,
            CancellationToken cancellationToken)
        {
            try
            {
                await _productRepository.DeleteAsync(product, cancellationToken);

                _logger.LogDebug(
                    "Product entity deleted successfully from repository: {ProductId}",
                    product.Id
                );

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product from repository: {ProductId}", product.Id);
                return Result<bool>.Failure($"Error deleting product: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs post-deletion operations (cleanup, notifications, etc.)
        /// </summary>
        /// <param name="product">The deleted product information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task PerformPostDeletionOperationsAsync(
            Domain.Entities.Product product,
            CancellationToken cancellationToken)
        {
            try
            {
                // Future implementations:
                // - Clear caches
                // - Update analytics
                // - Send deletion notifications
                // - Clean up related resources
                // - Update search indexes
                // - Trigger domain events

                _logger.LogDebug(
                    "Post-deletion operations completed for product: {ProductId}",
                    product.Id
                );

                await Task.CompletedTask; // Placeholder for future async operations
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Non-critical error in post-deletion operations for product: {ProductId}",
                    product.Id
                );
                // Non-critical errors are logged but don't affect the deletion result
            }
        }
    }
}
