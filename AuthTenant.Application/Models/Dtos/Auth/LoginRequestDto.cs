using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthTenant.Application.Models.Dtos.Auth
{
    /// <summary>
    /// Data Transfer Object for user login requests.
    /// Contains all necessary information for authenticating a user in a multi-tenant environment.
    /// Includes comprehensive validation rules to ensure data integrity and security.
    /// </summary>
    public sealed class LoginRequestDto
    {
        /// <summary>
        /// Gets or sets the user's email address for authentication.
        /// Must be a valid email format and is case-insensitive.
        /// Used as the primary identifier for user authentication.
        /// </summary>
        /// <example>user@example.com</example>
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        [StringLength(256, ErrorMessage = "Email address cannot exceed 256 characters")]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's password for authentication.
        /// Should meet the application's password complexity requirements.
        /// Never logged or stored in plain text.
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 128 characters")]
        [DataType(DataType.Password)]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenant authentication.
        /// Ensures users can only access their respective tenant's data.
        /// Critical for maintaining data isolation in multi-tenant architecture.
        /// </summary>
        /// <example>tenant-123</example>
        [Required(ErrorMessage = "Tenant ID is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Tenant ID must be between 1 and 50 characters")]
        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the user wants to stay logged in.
        /// When true, extends the session duration and refresh token lifetime.
        /// </summary>
        [JsonPropertyName("rememberMe")]
        public bool RememberMe { get; set; } = false;

        /// <summary>
        /// Gets or sets the client information for security tracking.
        /// Used for logging, security monitoring, and device management.
        /// </summary>
        [StringLength(200)]
        [JsonPropertyName("clientInfo")]
        public string? ClientInfo { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the client making the login request.
        /// Used for security monitoring, rate limiting, and fraud detection.
        /// </summary>
        [JsonPropertyName("ipAddress")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent string from the client.
        /// Used for device tracking and security analysis.
        /// </summary>
        [JsonPropertyName("userAgent")]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets additional security context for the login attempt.
        /// Can include device fingerprints, location data, etc.
        /// </summary>
        [JsonPropertyName("securityContext")]
        public IDictionary<string, string>? SecurityContext { get; set; }

        /// <summary>
        /// Gets the normalized email address for consistent lookups.
        /// Converts to lowercase and trims whitespace.
        /// </summary>
        [JsonIgnore]
        public string NormalizedEmail => Email.Trim().ToLowerInvariant();

        /// <summary>
        /// Gets a value indicating whether this appears to be a potentially suspicious login attempt.
        /// Based on various security indicators.
        /// </summary>
        [JsonIgnore]
        public bool IsSuspicious =>
            string.IsNullOrWhiteSpace(ClientInfo) ||
            string.IsNullOrWhiteSpace(UserAgent) ||
            Password.Length < 8; // Basic heuristics

        /// <summary>
        /// Validates the login request for completeness and security requirements.
        /// </summary>
        /// <returns>True if the request is valid, false otherwise</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(TenantId) &&
                   IsValidEmail(Email) &&
                   IsValidPassword(Password) &&
                   IsValidTenantId(TenantId);
        }

        /// <summary>
        /// Validates the email format using comprehensive rules.
        /// </summary>
        /// <param name="email">The email to validate</param>
        /// <returns>True if the email is valid</returns>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates the password meets minimum security requirements.
        /// </summary>
        /// <param name="password">The password to validate</param>
        /// <returns>True if the password meets requirements</returns>
        private static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) &&
                   password.Length >= 6 &&
                   password.Length <= 128;
        }

        /// <summary>
        /// Validates the tenant ID format and constraints.
        /// </summary>
        /// <param name="tenantId">The tenant ID to validate</param>
        /// <returns>True if the tenant ID is valid</returns>
        private static bool IsValidTenantId(string tenantId)
        {
            return !string.IsNullOrWhiteSpace(tenantId) &&
                   tenantId.Length <= 50 &&
                   tenantId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }

        /// <summary>
        /// Sanitizes the login request for logging purposes.
        /// Removes sensitive information while preserving important context.
        /// </summary>
        /// <returns>Dictionary with safe-to-log information</returns>
        public IDictionary<string, object> GetLoggingInfo()
        {
            return new Dictionary<string, object>
            {
                ["email"] = Email.Length > 0 ? $"{Email[0]}***@{Email.Split('@').LastOrDefault()}" : "unknown",
                ["tenantId"] = TenantId,
                ["rememberMe"] = RememberMe,
                ["hasClientInfo"] = !string.IsNullOrWhiteSpace(ClientInfo),
                ["hasUserAgent"] = !string.IsNullOrWhiteSpace(UserAgent),
                ["ipAddress"] = IpAddress ?? "unknown",
                ["isSuspicious"] = IsSuspicious,
                ["timestamp"] = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a new login request with essential information.
        /// Factory method to ensure proper initialization.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">User's password</param>
        /// <param name="tenantId">Tenant identifier</param>
        /// <param name="rememberMe">Whether to remember the login</param>
        /// <returns>Configured LoginRequestDto instance</returns>
        public static LoginRequestDto Create(string email, string password, string tenantId, bool rememberMe = false)
        {
            return new LoginRequestDto
            {
                Email = email?.Trim() ?? string.Empty,
                Password = password ?? string.Empty,
                TenantId = tenantId?.Trim() ?? string.Empty,
                RememberMe = rememberMe
            };
        }
    }
}
