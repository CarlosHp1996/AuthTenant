using System.Diagnostics;
using System.Net;
using System.Text.Json;

using AuthTenant.Domain.Exceptions;

namespace AuthTenant.API.Middleware
{
    /// <summary>
    /// Global exception handling middleware that catches unhandled exceptions,
    /// logs them appropriately, and returns standardized error responses to clients.
    /// Provides security by preventing sensitive error details from leaking in production.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// Initializes a new instance of the ExceptionMiddleware
        /// </summary>
        /// <param name="next">Next middleware delegate in the pipeline</param>
        /// <param name="logger">Logger instance for exception logging</param>
        /// <param name="environment">Web host environment for environment-specific behavior</param>
        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Processes the HTTP request with comprehensive exception handling
        /// </summary>
        /// <param name="context">HTTP context containing request and response information</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = context.TraceIdentifier;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await LogAndHandleExceptionAsync(context, ex, requestId, stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Logs the exception with appropriate detail level and handles the HTTP response
        /// </summary>
        /// <param name="context">HTTP context for the failed request</param>
        /// <param name="exception">Exception that occurred during request processing</param>
        /// <param name="requestId">Unique identifier for request correlation</param>
        /// <param name="elapsedMs">Time elapsed before exception occurred</param>
        private async Task LogAndHandleExceptionAsync(
            HttpContext context,
            Exception exception,
            string requestId,
            long elapsedMs)
        {
            var exceptionDetails = CreateExceptionDetails(exception, context, requestId, elapsedMs);

            LogException(exceptionDetails);
            await WriteErrorResponseAsync(context, exceptionDetails);
        }

        /// <summary>
        /// Creates detailed exception information for logging and response generation
        /// </summary>
        private ExceptionDetails CreateExceptionDetails(Exception exception, HttpContext context, string requestId, long elapsedMs)
        {
            var (statusCode, userMessage, logLevel) = MapExceptionToResponse(exception);

            return new ExceptionDetails
            {
                Exception = exception,
                RequestId = requestId,
                Path = context.Request.Path,
                Method = context.Request.Method,
                UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                ClientIp = GetClientIpAddress(context),
                UserId = GetUserId(context),
                TenantId = GetTenantId(context),
                StatusCode = statusCode,
                UserMessage = userMessage,
                LogLevel = logLevel,
                ElapsedMs = elapsedMs,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Maps different exception types to appropriate HTTP status codes, user messages, and log levels
        /// </summary>
        private (HttpStatusCode StatusCode, string UserMessage, LogLevel LogLevel) MapExceptionToResponse(Exception exception)
        {
            return exception switch
            {
                // Domain-specific exceptions
                TenantNotFoundException ex => (
                    HttpStatusCode.NotFound,
                    "The requested tenant was not found.",
                    LogLevel.Warning
                ),

                UnauthorizedTenantAccessException ex => (
                    HttpStatusCode.Forbidden,
                    "Access to the requested tenant is forbidden.",
                    LogLevel.Warning
                ),

                DomainException ex => (
                    HttpStatusCode.BadRequest,
                    ex.Message,
                    LogLevel.Information
                ),

                // Validation exceptions
                FluentValidation.ValidationException validationEx => (
                    HttpStatusCode.BadRequest,
                    "One or more validation errors occurred.",
                    LogLevel.Information
                ),

                // Authentication exceptions
                UnauthorizedAccessException ex => (
                    HttpStatusCode.Unauthorized,
                    "Authentication is required to access this resource.",
                    LogLevel.Warning
                ),

                // Not found exceptions
                KeyNotFoundException ex => (
                    HttpStatusCode.NotFound,
                    "The requested resource was not found.",
                    LogLevel.Information
                ),

                // Argument exceptions (bad requests) - specific types first
                ArgumentNullException ex => (
                    HttpStatusCode.BadRequest,
                    "Required request parameter is missing.",
                    LogLevel.Information
                ),

                ArgumentException ex => (
                    HttpStatusCode.BadRequest,
                    "Invalid request parameters provided.",
                    LogLevel.Information
                ),

                // Operation exceptions
                InvalidOperationException ex => (
                    HttpStatusCode.Conflict,
                    "The requested operation cannot be completed in the current state.",
                    LogLevel.Warning
                ),

                NotSupportedException ex => (
                    HttpStatusCode.BadRequest,
                    "The requested operation is not supported.",
                    LogLevel.Information
                ),

                // Timeout exceptions
                TimeoutException ex => (
                    HttpStatusCode.RequestTimeout,
                    "The request timed out. Please try again.",
                    LogLevel.Warning
                ),

                TaskCanceledException ex when ex.InnerException is TimeoutException => (
                    HttpStatusCode.RequestTimeout,
                    "The request timed out. Please try again.",
                    LogLevel.Warning
                ),

                // HTTP request exceptions (external service calls)
                HttpRequestException ex => (
                    HttpStatusCode.BadGateway,
                    "An error occurred while communicating with an external service.",
                    LogLevel.Error
                ),

                // Default: Internal server error
                _ => (
                    HttpStatusCode.InternalServerError,
                    "An internal server error occurred. Please try again later.",
                    LogLevel.Error
                )
            };
        }

        /// <summary>
        /// Logs exception details with appropriate log level and structured data
        /// </summary>
        private void LogException(ExceptionDetails details)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = details.RequestId,
                ["Path"] = details.Path,
                ["Method"] = details.Method,
                ["StatusCode"] = (int)details.StatusCode,
                ["ElapsedMs"] = details.ElapsedMs,
                ["ClientIp"] = details.ClientIp,
                ["UserId"] = details.UserId ?? "Anonymous",
                ["TenantId"] = details.TenantId ?? "Unknown"
            });

            var message = "Exception occurred during request processing: {ExceptionType} - {ExceptionMessage}";
            var args = new object[] { details.Exception.GetType().Name, details.Exception.Message };

            switch (details.LogLevel)
            {
                case LogLevel.Information:
                    _logger.LogInformation(details.Exception, message, args);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(details.Exception, message, args);
                    break;
                case LogLevel.Error:
                    _logger.LogError(details.Exception, message, args);
                    break;
                default:
                    _logger.LogError(details.Exception, message, args);
                    break;
            }

            // Log additional debug information in development
            if (_environment.IsDevelopment())
            {
                _logger.LogDebug("Exception Details - User Agent: {UserAgent}, Stack Trace: {StackTrace}",
                    details.UserAgent, details.Exception.StackTrace);
            }
        }

        /// <summary>
        /// Writes a standardized error response to the HTTP response stream
        /// </summary>
        private async Task WriteErrorResponseAsync(HttpContext context, ExceptionDetails details)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Cannot write error response - response has already started for request {RequestId}", details.RequestId);
                return;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)details.StatusCode;

            // Add correlation headers
            context.Response.Headers.TryAdd("X-Request-ID", details.RequestId);
            context.Response.Headers.TryAdd("X-Error-Timestamp", details.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

            var errorResponse = CreateErrorResponse(details);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment(),
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }

        /// <summary>
        /// Creates the error response object with appropriate detail level based on environment
        /// </summary>
        private object CreateErrorResponse(ExceptionDetails details)
        {
            var baseResponse = new
            {
                error = new
                {
                    message = details.UserMessage,
                    code = details.Exception.GetType().Name,
                    requestId = details.RequestId,
                    timestamp = details.Timestamp,
                    path = details.Path.ToString()
                }
            };

            // Include additional details in development environment
            if (_environment.IsDevelopment())
            {
                return new
                {
                    error = new
                    {
                        message = details.UserMessage,
                        code = details.Exception.GetType().Name,
                        requestId = details.RequestId,
                        timestamp = details.Timestamp,
                        path = details.Path.ToString(),
                        details = new
                        {
                            exceptionMessage = details.Exception.Message,
                            stackTrace = details.Exception.StackTrace,
                            innerException = details.Exception.InnerException?.Message,
                            data = details.Exception.Data?.Count > 0 ? details.Exception.Data : null
                        }
                    },
                    request = new
                    {
                        method = details.Method,
                        userAgent = details.UserAgent,
                        clientIp = details.ClientIp,
                        elapsedMs = details.ElapsedMs
                    }
                };
            }

            // Handle validation exceptions with field-specific errors
            if (details.Exception is FluentValidation.ValidationException validationEx)
            {
                return new
                {
                    error = new
                    {
                        message = details.UserMessage,
                        code = details.Exception.GetType().Name,
                        requestId = details.RequestId,
                        timestamp = details.Timestamp,
                        path = details.Path.ToString(),
                        validationErrors = validationEx.Errors.Select(e => new
                        {
                            field = e.PropertyName,
                            message = e.ErrorMessage,
                            attemptedValue = e.AttemptedValue
                        }).ToArray()
                    }
                };
            }

            return baseResponse;
        }

        /// <summary>
        /// Extracts the client IP address from the request, considering proxy headers
        /// </summary>
        private static string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Extracts user ID from the authenticated user context
        /// </summary>
        private static string? GetUserId(HttpContext context)
        {
            return context.User?.FindFirst("sub")?.Value
                ?? context.User?.FindFirst("id")?.Value
                ?? context.User?.Identity?.Name;
        }

        /// <summary>
        /// Extracts tenant ID from the request context
        /// </summary>
        private static string? GetTenantId(HttpContext context)
        {
            return context.Items["TenantId"]?.ToString()
                ?? context.Request.Headers["X-Tenant-ID"].FirstOrDefault();
        }

        /// <summary>
        /// Internal class to hold exception details for structured logging and response generation
        /// </summary>
        private sealed class ExceptionDetails
        {
            public required Exception Exception { get; init; }
            public required string RequestId { get; init; }
            public required PathString Path { get; init; }
            public required string Method { get; init; }
            public string? UserAgent { get; init; }
            public required string ClientIp { get; init; }
            public string? UserId { get; init; }
            public string? TenantId { get; init; }
            public required HttpStatusCode StatusCode { get; init; }
            public required string UserMessage { get; init; }
            public required LogLevel LogLevel { get; init; }
            public required long ElapsedMs { get; init; }
            public required DateTime Timestamp { get; init; }
        }
    }

    /// <summary>
    /// Extension methods for registering ExceptionMiddleware
    /// </summary>
    public static class ExceptionMiddlewareExtensions
    {
        /// <summary>
        /// Adds the ExceptionMiddleware to the application pipeline.
        /// Should be registered early in the pipeline to catch all exceptions.
        /// </summary>
        /// <param name="builder">Application builder instance</param>
        /// <returns>Application builder for method chaining</returns>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
