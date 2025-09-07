using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Application.Behaviors
{
    /// <summary>
    /// MediatR pipeline behavior that handles request validation using FluentValidation.
    /// Validates incoming requests before they reach their handlers and throws validation exceptions
    /// with detailed error information when validation fails.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being validated</typeparam>
    /// <typeparam name="TResponse">The type of response expected from the request handler</typeparam>
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        /// <summary>
        /// Initializes a new instance of the ValidationBehavior with validators and logger
        /// </summary>
        /// <param name="validators">Collection of validators for the request type</param>
        /// <param name="logger">Logger instance for diagnostic information</param>
        public ValidationBehavior(
            IEnumerable<IValidator<TRequest>> validators,
            ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the validation pipeline for incoming requests
        /// </summary>
        /// <param name="request">The request to validate</param>
        /// <param name="next">The next delegate in the pipeline</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>The response from the next handler if validation passes</returns>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(next);

            var requestType = typeof(TRequest).Name;
            var validatorCount = _validators.Count();

            // Skip validation if no validators are registered
            if (validatorCount == 0)
            {
                _logger.LogDebug("No validators registered for {RequestType}", requestType);
                return await next();
            }

            _logger.LogDebug(
                "🔍 Starting validation for {RequestType} with {ValidatorCount} validator(s)",
                requestType,
                validatorCount
            );

            try
            {
                // Execute all validators concurrently for better performance
                var validationContext = new ValidationContext<TRequest>(request);
                var validationTasks = _validators.Select(validator =>
                    validator.ValidateAsync(validationContext, cancellationToken));

                var validationResults = await Task.WhenAll(validationTasks);

                // Collect all validation failures
                var failures = validationResults
                    .Where(result => !result.IsValid)
                    .SelectMany(result => result.Errors)
                    .Where(failure => failure != null)
                    .ToList();

                // If validation failed, log details and throw exception
                if (failures.Count > 0)
                {
                    LogValidationFailures(requestType, failures);
                    throw new ValidationException(failures);
                }

                _logger.LogDebug(
                    "✅ Validation passed for {RequestType}",
                    requestType
                );

                return await next();
            }
            catch (ValidationException)
            {
                // Re-throw validation exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Unexpected error during validation for {RequestType}",
                    requestType
                );
                throw;
            }
        }

        /// <summary>
        /// Logs detailed information about validation failures for debugging and monitoring
        /// </summary>
        /// <param name="requestType">The type of request that failed validation</param>
        /// <param name="failures">Collection of validation failures</param>
        private void LogValidationFailures(string requestType, IList<ValidationFailure> failures)
        {
            var errorDetails = failures.Select(failure => new
            {
                Property = failure.PropertyName,
                Error = failure.ErrorMessage,
                AttemptedValue = failure.AttemptedValue?.ToString() ?? "null",
                Severity = failure.Severity.ToString()
            }).ToList();

            _logger.LogWarning(
                "🚫 Validation failed for {RequestType} | Errors: {ErrorCount} | Details: {@ValidationErrors}",
                requestType,
                failures.Count,
                errorDetails
            );

            // Log individual errors for easier troubleshooting
            foreach (var failure in failures)
            {
                _logger.LogDebug(
                    "❌ Validation Error | Property: {PropertyName} | Message: {ErrorMessage} | Value: {AttemptedValue}",
                    failure.PropertyName,
                    failure.ErrorMessage,
                    failure.AttemptedValue
                );
            }
        }
    }
}
