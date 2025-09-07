using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthTenant.Application.Models.Dtos.Auth
{
    /// <summary>
    /// Data Transfer Object for authentication responses.
    /// Contains all necessary information for a successful authentication, including tokens and user data.
    /// Designed for secure token-based authentication with JWT and refresh token support.
    /// </summary>
    public sealed class AuthResponseDto
    {
        /// <summary>
        /// Gets or sets the JWT access token for API authentication.
        /// This token should be included in the Authorization header for authenticated requests.
        /// Has a limited lifespan and should be refreshed using the refresh token when expired.
        /// </summary>
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        [Required]
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the refresh token for obtaining new access tokens.
        /// Used to maintain user sessions without requiring re-authentication.
        /// Should be stored securely and has a longer lifespan than access tokens.
        /// </summary>
        /// <example>550e8400-e29b-41d4-a716-446655440000</example>
        [Required]
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exact timestamp when the access token expires.
        /// Clients should refresh the token before this time to maintain authentication.
        /// Stored in UTC format for consistency across timezones.
        /// </summary>
        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the authenticated user's information.
        /// Contains safe user data excluding sensitive information like passwords.
        /// Used by clients to display user information and manage user context.
        /// </summary>
        [Required]
        [JsonPropertyName("user")]
        public UserDto User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the token type (typically "Bearer").
        /// Indicates how the token should be used in API requests.
        /// </summary>
        /// <example>Bearer</example>
        [JsonPropertyName("tokenType")]
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Gets or sets the scope of permissions granted by this token.
        /// Defines what actions the authenticated user can perform.
        /// </summary>
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = "api";

        /// <summary>
        /// Gets or sets additional metadata about the authentication session.
        /// Can include information like login method, device info, etc.
        /// </summary>
        [JsonPropertyName("metadata")]
        public IDictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets the number of seconds until the token expires.
        /// Useful for client-side token management and automatic refresh logic.
        /// </summary>
        [JsonPropertyName("expiresIn")]
        public long ExpiresIn => Math.Max(0, (long)(ExpiresAt - DateTime.UtcNow).TotalSeconds);

        /// <summary>
        /// Gets a value indicating whether the token is still valid (not expired).
        /// Useful for validation before making API calls.
        /// </summary>
        [JsonPropertyName("isValid")]
        public bool IsValid => DateTime.UtcNow < ExpiresAt;

        /// <summary>
        /// Gets a value indicating whether the token will expire soon (within 5 minutes).
        /// Useful for proactive token refresh to prevent authentication failures.
        /// </summary>
        [JsonPropertyName("expiresSoon")]
        public bool ExpiresSoon => (ExpiresAt - DateTime.UtcNow).TotalMinutes <= 5;

        /// <summary>
        /// Gets the timestamp when this authentication response was created.
        /// Useful for tracking authentication events and session management.
        /// </summary>
        [JsonPropertyName("issuedAt")]
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a successful authentication response.
        /// Factory method to ensure all required fields are properly set.
        /// </summary>
        /// <param name="token">The JWT access token</param>
        /// <param name="refreshToken">The refresh token</param>
        /// <param name="expiresAt">Token expiration timestamp</param>
        /// <param name="user">Authenticated user information</param>
        /// <param name="metadata">Optional additional metadata</param>
        /// <returns>Configured AuthResponseDto instance</returns>
        public static AuthResponseDto CreateSuccess(
            string token,
            string refreshToken,
            DateTime expiresAt,
            UserDto user,
            IDictionary<string, object>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty", nameof(token));

            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = user,
                Metadata = metadata,
                IssuedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Validates the authentication response for consistency and completeness.
        /// </summary>
        /// <returns>True if the response is valid, false otherwise</returns>
        public bool IsValidResponse()
        {
            return !string.IsNullOrWhiteSpace(Token) &&
                   !string.IsNullOrWhiteSpace(RefreshToken) &&
                   ExpiresAt > DateTime.UtcNow &&
                   User != null &&
                   User.IsValid() &&
                   !string.IsNullOrWhiteSpace(TokenType);
        }

        /// <summary>
        /// Gets a sanitized version of the response for logging purposes.
        /// Excludes sensitive token information while preserving important metadata.
        /// </summary>
        /// <returns>Dictionary with safe-to-log information</returns>
        public IDictionary<string, object> GetLoggingInfo()
        {
            return new Dictionary<string, object>
            {
                ["userId"] = User?.Id ?? "unknown",
                ["userEmail"] = User?.Email ?? "unknown",
                ["tenantId"] = User?.TenantId ?? "unknown",
                ["expiresAt"] = ExpiresAt,
                ["expiresIn"] = ExpiresIn,
                ["tokenType"] = TokenType,
                ["issuedAt"] = IssuedAt,
                ["isValid"] = IsValid
            };
        }
    }
}
