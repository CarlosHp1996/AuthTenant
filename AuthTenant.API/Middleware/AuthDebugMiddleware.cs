using System.Diagnostics;
using System.Security.Claims;

namespace AuthTenant.API.Middleware
{
    public class AuthDebugMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthDebugMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public AuthDebugMiddleware(
            RequestDelegate next,
            ILogger<AuthDebugMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_environment.IsDevelopment())
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestId = context.TraceIdentifier;

            try
            {
                LogRequestStart(context, requestId);
                LogAuthenticationState(context, requestId, "BEFORE");
                LogRequestHeaders(context, requestId);

                await _next(context);

                LogAuthenticationState(context, requestId, "AFTER");
                LogRequestEnd(context, requestId, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                LogException(context, requestId, ex, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private void LogRequestStart(HttpContext context, string requestId)
        {
            _logger.LogInformation(
                "[AUTH-DEBUG] Request Started | ID: {RequestId} | Path: {Path} | Method: {Method} | RemoteIP: {RemoteIP}",
                requestId,
                context.Request.Path,
                context.Request.Method,
                GetClientIpAddress(context)
            );
        }

        private void LogAuthenticationState(HttpContext context, string requestId, string stage)
        {
            var user = context.User;
            var identity = user?.Identity;

            _logger.LogInformation(
                "[AUTH-DEBUG] User State {Stage} | ID: {RequestId} | Authenticated: {IsAuthenticated} | Name: {UserName} | AuthType: {AuthType}",
                stage,
                requestId,
                identity?.IsAuthenticated ?? false,
                identity?.Name ?? "Anonymous",
                identity?.AuthenticationType ?? "None"
            );

            if (identity?.IsAuthenticated == true && user?.Claims?.Any() == true)
                LogUserClaims(user, requestId, stage);

            if (user?.IsInRole("Admin") == true || user?.IsInRole("User") == true)
            {
                var roles = GetUserRoles(user);
                _logger.LogInformation(
                    "[AUTH-DEBUG] User Roles {Stage} | ID: {RequestId} | Roles: [{Roles}]",
                    stage,
                    requestId,
                    string.Join(", ", roles)
                );
            }
        }

        private void LogRequestHeaders(HttpContext context, string requestId)
        {
            var headers = context.Request.Headers;

            if (headers.ContainsKey("Authorization"))
            {
                var authHeader = headers["Authorization"].FirstOrDefault();
                var maskedHeader = MaskSensitiveData(authHeader);

                _logger.LogInformation(
                    "[AUTH-DEBUG] Auth Header | ID: {RequestId} | Header: {AuthHeader} | Length: {Length}",
                    requestId,
                    maskedHeader,
                    authHeader?.Length ?? 0
                );

                if (authHeader?.StartsWith("Bearer ") == true)
                    ValidateJwtFormat(authHeader, requestId);
            }
            else
            {
                _logger.LogWarning(
                    "[AUTH-DEBUG] Missing Auth Header | ID: {RequestId} | Path: {Path}",
                    requestId,
                    context.Request.Path
                );
            }

            LogAdditionalHeaders(headers, requestId);
        }

        private void LogUserClaims(ClaimsPrincipal user, string requestId, string stage)
        {
            var claims = user.Claims.Select(c => new { Type = c.Type, Value = MaskClaimValue(c) }).ToList();

            _logger.LogInformation(
                "[AUTH-DEBUG] User Claims {Stage} | ID: {RequestId} | ClaimsCount: {ClaimsCount}",
                stage,
                requestId,
                claims.Count
            );

            foreach (var claim in claims.Take(10)) // Limit to first 10 claims to avoid log spam
            {
                _logger.LogDebug(
                    "[AUTH-DEBUG] Claim {Stage} | ID: {RequestId} | Type: {ClaimType} | Value: {ClaimValue}",
                    stage,
                    requestId,
                    claim.Type,
                    claim.Value
                );
            }
        }

        private void LogAdditionalHeaders(IHeaderDictionary headers, string requestId)
        {
            var relevantHeaders = new[] { "User-Agent", "X-Forwarded-For", "X-Real-IP", "Accept", "Content-Type" };

            foreach (var headerName in relevantHeaders)
            {
                if (headers.ContainsKey(headerName))
                {
                    var value = headers[headerName].FirstOrDefault();
                    _logger.LogDebug(
                        "[AUTH-DEBUG] Header | ID: {RequestId} | Name: {HeaderName} | Value: {HeaderValue}",
                        requestId,
                        headerName,
                        value?.Length > 100 ? $"{value[..100]}..." : value
                    );
                }
            }
        }

        private void ValidateJwtFormat(string authHeader, string requestId)
        {
            try
            {
                var token = authHeader.Substring(7); // Remove "Bearer "
                var parts = token.Split('.');

                _logger.LogDebug(
                    "[AUTH-DEBUG] JWT Analysis | ID: {RequestId} | Parts: {PartsCount} | TokenLength: {TokenLength} | Valid Format: {IsValidFormat}",
                    requestId,
                    parts.Length,
                    token.Length,
                    parts.Length == 3
                );

                if (parts.Length != 3)
                {
                    _logger.LogWarning(
                        "[AUTH-DEBUG] Invalid JWT Format | ID: {RequestId} | Expected 3 parts, got {ActualParts}",
                        requestId,
                        parts.Length
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "[AUTH-DEBUG] JWT Validation Error | ID: {RequestId} | Error: {ErrorMessage}",
                    requestId,
                    ex.Message
                );
            }
        }

        private void LogRequestEnd(HttpContext context, string requestId, long elapsedMs)
        {
            _logger.LogInformation(
                "[AUTH-DEBUG] Request Completed | ID: {RequestId} | Status: {StatusCode} | Duration: {ElapsedMs}ms | Success: {IsSuccess}",
                requestId,
                context.Response.StatusCode,
                elapsedMs,
                IsSuccessStatusCode(context.Response.StatusCode)
            );
        }

        private void LogException(HttpContext context, string requestId, Exception ex, long elapsedMs)
        {
            _logger.LogError(ex,
                "[AUTH-DEBUG] Request Failed | ID: {RequestId} | Path: {Path} | Duration: {ElapsedMs}ms | Exception: {ExceptionType} | Message: {ExceptionMessage}",
                requestId,
                context.Request.Path,
                elapsedMs,
                ex.GetType().Name,
                ex.Message
            );
        }

        private static string MaskSensitiveData(string? authHeader)
        {
            if (string.IsNullOrEmpty(authHeader))
                return "NULL";

            if (authHeader.StartsWith("Bearer ") && authHeader.Length > 20)
            {
                var prefix = authHeader.Substring(0, 13); // "Bearer " + first 6 chars of token
                var suffix = authHeader.Substring(authHeader.Length - 6); // last 6 chars
                return $"{prefix}...{suffix} (Length: {authHeader.Length})";
            }

            return authHeader.Length > 20 ? $"{authHeader[..10]}...{authHeader[^6..]} (Length: {authHeader.Length})" : authHeader;
        }

        private static string MaskClaimValue(Claim claim)
        {
            var sensitiveClaims = new[] { "sub", "jti", "email", "phone_number" };

            if (sensitiveClaims.Contains(claim.Type.ToLowerInvariant()) ||
                claim.Type.Contains("id", StringComparison.OrdinalIgnoreCase))
            {
                return claim.Value.Length > 6 ? $"{claim.Value[..3]}***{claim.Value[^3..]}" : "***";
            }

            return claim.Value;
        }

        private static IEnumerable<string> GetUserRoles(ClaimsPrincipal user)
        {
            return user.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .Distinct();
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private static bool IsSuccessStatusCode(int statusCode)
        {
            return statusCode >= 200 && statusCode <= 299;
        }
    }

    public static class AuthDebugMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthDebugMiddleware(this IApplicationBuilder builder)
        {
            var environment = builder.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            // Only register in development environment
            if (environment.IsDevelopment())
                return builder.UseMiddleware<AuthDebugMiddleware>();

            return builder;
        }
    }
}
