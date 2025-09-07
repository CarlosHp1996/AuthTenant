using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Application.Behaviors
{
    /// <summary>
    /// MediatR pipeline behavior that provides comprehensive logging for all requests and responses.
    /// Captures request execution time, success/failure status, and contextual information for observability.
    /// Implements structured logging for better monitoring and debugging capabilities.
    /// </summary>
    /// <typeparam name="TRequest">Type of the MediatR request</typeparam>
    /// <typeparam name="TResponse">Type of the MediatR response</typeparam>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Initializes a new instance of the LoggingBehavior
        /// </summary>
        /// <param name="logger">Logger instance for structured logging</param>
        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the request with comprehensive logging and performance tracking
        /// </summary>
        /// <param name="request">The MediatR request to be processed</param>
        /// <param name="next">Delegate to the next handler in the pipeline</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>The response from the request handler</returns>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var executionContext = CreateExecutionContext(request);
            var stopwatch = Stopwatch.StartNew();

            LogRequestStart(executionContext);

            try
            {
                var response = await next();
                stopwatch.Stop();

                LogRequestSuccess(executionContext, response, stopwatch.ElapsedMilliseconds);
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogRequestFailure(executionContext, ex, stopwatch.ElapsedMilliseconds);
                throw; // Re-throw to maintain exception flow
            }
        }

        /// <summary>
        /// Creates execution context with request metadata and performance tracking
        /// </summary>
        private static RequestExecutionContext CreateExecutionContext(TRequest request)
        {
            var requestType = typeof(TRequest);
            var responseType = typeof(TResponse);

            return new RequestExecutionContext
            {
                RequestId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                RequestName = GetFriendlyRequestName(requestType),
                RequestType = requestType.Name,
                ResponseType = responseType.Name,
                RequestNamespace = requestType.Namespace ?? "Unknown",
                IsCommand = IsCommandRequest(requestType),
                IsQuery = IsQueryRequest(requestType),
                Timestamp = DateTime.UtcNow,
                Request = request
            };
        }

        /// <summary>
        /// Logs the start of request processing with contextual information
        /// </summary>
        private void LogRequestStart(RequestExecutionContext context)
        {
            using var scope = CreateLoggingScope(context);

            _logger.LogInformation(
                "🚀 Starting {RequestType} | Name: {RequestName} | ID: {RequestId} | Type: {HandlerType}",
                context.IsCommand ? "Command" : context.IsQuery ? "Query" : "Request",
                context.RequestName,
                context.RequestId,
                context.RequestType
            );

            // Log request details in debug level for development
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var requestJson = SerializeRequest(context.Request);
                _logger.LogDebug(
                    "📋 Request Details | ID: {RequestId} | Payload: {RequestPayload}",
                    context.RequestId,
                    requestJson
                );
            }
        }

        /// <summary>
        /// Logs successful request completion with performance metrics and response details
        /// </summary>
        private void LogRequestSuccess(RequestExecutionContext context, TResponse response, long elapsedMs)
        {
            using var scope = CreateLoggingScope(context, elapsedMs, true);

            var performanceCategory = CategorizePerformance(elapsedMs);
            var logLevel = GetPerformanceLogLevel(performanceCategory);

            _logger.Log(logLevel,
                "✅ Completed {RequestType} | Name: {RequestName} | ID: {RequestId} | Duration: {ElapsedMs}ms | Performance: {PerformanceCategory}",
                context.IsCommand ? "Command" : context.IsQuery ? "Query" : "Request",
                context.RequestName,
                context.RequestId,
                elapsedMs,
                performanceCategory
            );

            // Log response details in debug level for development
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var responseJson = SerializeResponse(response);
                _logger.LogDebug(
                    "📤 Response Details | ID: {RequestId} | Response: {ResponsePayload}",
                    context.RequestId,
                    responseJson
                );
            }

            // Log performance warnings for slow requests
            if (performanceCategory is PerformanceCategory.Slow or PerformanceCategory.VerySlow)
            {
                _logger.LogWarning(
                    "⚠️ Slow Request Detected | Name: {RequestName} | ID: {RequestId} | Duration: {ElapsedMs}ms | Threshold: {Threshold}ms",
                    context.RequestName,
                    context.RequestId,
                    elapsedMs,
                    GetPerformanceThreshold(performanceCategory)
                );
            }
        }

        /// <summary>
        /// Logs request failure with exception details and performance context
        /// </summary>
        private void LogRequestFailure(RequestExecutionContext context, Exception exception, long elapsedMs)
        {
            using var scope = CreateLoggingScope(context, elapsedMs, false);

            _logger.LogError(exception,
                "❌ Failed {RequestType} | Name: {RequestName} | ID: {RequestId} | Duration: {ElapsedMs}ms | Exception: {ExceptionType} | Message: {ExceptionMessage}",
                context.IsCommand ? "Command" : context.IsQuery ? "Query" : "Request",
                context.RequestName,
                context.RequestId,
                elapsedMs,
                exception.GetType().Name,
                exception.Message
            );

            // Log additional context for specific exception types
            LogExceptionContext(context, exception);
        }

        /// <summary>
        /// Creates a logging scope with structured data for better observability
        /// </summary>
        private static IDisposable CreateLoggingScope(RequestExecutionContext context, long? elapsedMs = null, bool? success = null)
        {
            var scopeData = new Dictionary<string, object>
            {
                ["RequestId"] = context.RequestId,
                ["RequestName"] = context.RequestName,
                ["RequestType"] = context.RequestType,
                ["ResponseType"] = context.ResponseType,
                ["IsCommand"] = context.IsCommand,
                ["IsQuery"] = context.IsQuery,
                ["Namespace"] = context.RequestNamespace,
                ["Timestamp"] = context.Timestamp
            };

            if (elapsedMs.HasValue)
                scopeData["ElapsedMs"] = elapsedMs.Value;

            if (success.HasValue)
                scopeData["Success"] = success.Value;

            return new DisposableScope(scopeData);
        }

        /// <summary>
        /// Logs additional context information for specific exception types
        /// </summary>
        private void LogExceptionContext(RequestExecutionContext context, Exception exception)
        {
            switch (exception)
            {
                case FluentValidation.ValidationException validationEx:
                    _logger.LogWarning(
                        "🔍 Validation Failed | ID: {RequestId} | Errors: {ValidationErrors}",
                        context.RequestId,
                        string.Join("; ", validationEx.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))
                    );
                    break;

                case UnauthorizedAccessException:
                    _logger.LogWarning(
                        "🔐 Authorization Failed | ID: {RequestId} | Request: {RequestName}",
                        context.RequestId,
                        context.RequestName
                    );
                    break;

                case KeyNotFoundException:
                    _logger.LogInformation(
                        "🔍 Resource Not Found | ID: {RequestId} | Request: {RequestName}",
                        context.RequestId,
                        context.RequestName
                    );
                    break;

                case TimeoutException or TaskCanceledException:
                    _logger.LogWarning(
                        "⏰ Request Timeout | ID: {RequestId} | Request: {RequestName} | Duration: {ElapsedMs}ms",
                        context.RequestId,
                        context.RequestName,
                        context.Timestamp
                    );
                    break;
            }
        }

        /// <summary>
        /// Safely serializes request object for logging, handling sensitive data
        /// </summary>
        private static string SerializeRequest(TRequest request)
        {
            try
            {
                // For security, we might want to exclude sensitive properties
                if (IsSensitiveRequest(request))
                {
                    return $"[{typeof(TRequest).Name} - Contains sensitive data]";
                }

                return JsonSerializer.Serialize(request, JsonOptions);
            }
            catch (Exception)
            {
                return $"[{typeof(TRequest).Name} - Serialization failed]";
            }
        }

        /// <summary>
        /// Safely serializes response object for logging
        /// </summary>
        private static string SerializeResponse(TResponse response)
        {
            try
            {
                if (response == null)
                    return "[null]";

                // Handle large responses
                var responseJson = JsonSerializer.Serialize(response, JsonOptions);
                if (responseJson.Length > 1000) // Limit response size in logs
                {
                    return $"[{typeof(TResponse).Name} - Large response ({responseJson.Length} chars)]";
                }

                return responseJson;
            }
            catch (Exception)
            {
                return $"[{typeof(TResponse).Name} - Serialization failed]";
            }
        }

        /// <summary>
        /// Determines if a request contains sensitive data that should not be logged
        /// </summary>
        private static bool IsSensitiveRequest(TRequest request)
        {
            var requestType = typeof(TRequest);
            var requestName = requestType.Name.ToLowerInvariant();

            // Check for common sensitive request patterns
            return requestName.Contains("password") ||
                   requestName.Contains("login") ||
                   requestName.Contains("auth") ||
                   requestName.Contains("token") ||
                   requestName.Contains("secret") ||
                   requestName.Contains("key");
        }

        /// <summary>
        /// Gets a user-friendly name for the request type
        /// </summary>
        private static string GetFriendlyRequestName(Type requestType)
        {
            var name = requestType.Name;

            // Remove common suffixes for cleaner logging
            var suffixes = new[] { "Command", "Query", "Request" };
            foreach (var suffix in suffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                    break;
                }
            }

            return name;
        }

        /// <summary>
        /// Determines if the request is a command (write operation)
        /// </summary>
        private static bool IsCommandRequest(Type requestType)
        {
            return requestType.Name.Contains("Command", StringComparison.OrdinalIgnoreCase) ||
                   requestType.Namespace?.Contains("Commands", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Determines if the request is a query (read operation)
        /// </summary>
        private static bool IsQueryRequest(Type requestType)
        {
            return requestType.Name.Contains("Query", StringComparison.OrdinalIgnoreCase) ||
                   requestType.Namespace?.Contains("Queries", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Categorizes request performance based on execution time
        /// </summary>
        private static PerformanceCategory CategorizePerformance(long elapsedMs)
        {
            return elapsedMs switch
            {
                < 100 => PerformanceCategory.Fast,
                < 500 => PerformanceCategory.Normal,
                < 1000 => PerformanceCategory.Slow,
                < 5000 => PerformanceCategory.VerySlow,
                _ => PerformanceCategory.Critical
            };
        }

        /// <summary>
        /// Gets appropriate log level based on performance category
        /// </summary>
        private static LogLevel GetPerformanceLogLevel(PerformanceCategory category)
        {
            return category switch
            {
                PerformanceCategory.Fast or PerformanceCategory.Normal => LogLevel.Information,
                PerformanceCategory.Slow => LogLevel.Warning,
                PerformanceCategory.VerySlow or PerformanceCategory.Critical => LogLevel.Error,
                _ => LogLevel.Information
            };
        }

        /// <summary>
        /// Gets the performance threshold for a given category
        /// </summary>
        private static int GetPerformanceThreshold(PerformanceCategory category)
        {
            return category switch
            {
                PerformanceCategory.Slow => 500,
                PerformanceCategory.VerySlow => 1000,
                PerformanceCategory.Critical => 5000,
                _ => 100
            };
        }

        /// <summary>
        /// Performance categories for request execution time
        /// </summary>
        private enum PerformanceCategory
        {
            Fast,
            Normal,
            Slow,
            VerySlow,
            Critical
        }

        /// <summary>
        /// Context information for request execution
        /// </summary>
        private sealed class RequestExecutionContext
        {
            public required string RequestId { get; init; }
            public required string RequestName { get; init; }
            public required string RequestType { get; init; }
            public required string ResponseType { get; init; }
            public required string RequestNamespace { get; init; }
            public required bool IsCommand { get; init; }
            public required bool IsQuery { get; init; }
            public required DateTime Timestamp { get; init; }
            public required TRequest Request { get; init; }
        }

        /// <summary>
        /// Custom disposable scope for structured logging
        /// </summary>
        private sealed class DisposableScope : IDisposable
        {
            private readonly Dictionary<string, object> _scopeData;

            public DisposableScope(Dictionary<string, object> scopeData)
            {
                _scopeData = scopeData;
            }

            public void Dispose()
            {
                // Implementation would depend on the logging framework
                // This is a placeholder for proper scope disposal
            }
        }
    }
}
