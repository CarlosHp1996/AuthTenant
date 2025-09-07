using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthTenant.Application.Models.Dtos.Auth
{
    /// <summary>
    /// Data Transfer Object for user information.
    /// Represents user data in a safe format for API responses, excluding sensitive information.
    /// Supports multi-tenant architecture with tenant isolation.
    /// </summary>
    public sealed class UserDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user.
        /// This is typically a GUID string representation.
        /// </summary>
        /// <example>550e8400-e29b-41d4-a716-446655440000</example>
        [Required]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address.
        /// Used for authentication and communication purposes.
        /// Must be unique within the tenant scope.
        /// </summary>
        /// <example>user@example.com</example>
        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's first name.
        /// Used for personalization and display purposes.
        /// </summary>
        /// <example>John</example>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's last name.
        /// Used for personalization and display purposes.
        /// </summary>
        /// <example>Doe</example>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tenant identifier this user belongs to.
        /// Ensures proper tenant isolation in multi-tenant architecture.
        /// </summary>
        /// <example>tenant-123</example>
        [Required]
        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the user account is active.
        /// Inactive users cannot authenticate or perform operations.
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the timestamp when the user account was created.
        /// Stored in UTC format for consistency across timezones.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets the user's full name by combining first and last names.
        /// This is a computed property that formats the name for display purposes.
        /// Handles edge cases where names might be empty or null.
        /// </summary>
        /// <example>John Doe</example>
        [JsonPropertyName("fullName")]
        public string FullName => CreateFullName(FirstName, LastName);

        /// <summary>
        /// Gets or sets the user's preferred display name.
        /// If not set, falls back to the full name.
        /// Useful for nicknames or professional names.
        /// </summary>
        [StringLength(100)]
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the user's last login.
        /// Used for security auditing and user activity tracking.
        /// Null if the user has never logged in.
        /// </summary>
        [JsonPropertyName("lastLoginAt")]
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user's email has been verified.
        /// Important for security and ensuring valid communication channels.
        /// </summary>
        [JsonPropertyName("emailVerified")]
        public bool EmailVerified { get; set; } = false;

        /// <summary>
        /// Gets or sets user-specific roles within the tenant.
        /// Used for authorization and access control.
        /// </summary>
        [JsonPropertyName("roles")]
        public IReadOnlyList<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Creates a properly formatted full name from first and last name components.
        /// Handles null, empty, and whitespace-only values gracefully.
        /// </summary>
        /// <param name="firstName">The user's first name</param>
        /// <param name="lastName">The user's last name</param>
        /// <returns>Formatted full name or appropriate fallback</returns>
        private static string CreateFullName(string? firstName, string? lastName)
        {
            // Normalize input values
            var first = string.IsNullOrWhiteSpace(firstName) ? string.Empty : firstName.Trim();
            var last = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();

            // Handle all possible combinations
            return (first, last) switch
            {
                ("", "") => "Unknown User",
                (var f, "") => f,
                ("", var l) => l,
                (var f, var l) => $"{f} {l}"
            };
        }

        /// <summary>
        /// Gets the effective display name for the user.
        /// Returns DisplayName if set, otherwise falls back to FullName.
        /// </summary>
        [JsonPropertyName("effectiveDisplayName")]
        public string EffectiveDisplayName => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : FullName;

        /// <summary>
        /// Gets a value indicating whether this is a new user (never logged in).
        /// Useful for UI logic and onboarding flows.
        /// </summary>
        [JsonPropertyName("isNewUser")]
        public bool IsNewUser => LastLoginAt == null;

        /// <summary>
        /// Gets the number of days since the user was created.
        /// Useful for user lifecycle management and analytics.
        /// </summary>
        [JsonPropertyName("daysSinceCreation")]
        public int DaysSinceCreation => (DateTime.UtcNow - CreatedAt).Days;

        /// <summary>
        /// Validates the user DTO for consistency and business rules.
        /// </summary>
        /// <returns>True if the DTO is valid, false otherwise</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(TenantId) &&
                   IsValidEmail(Email);
        }

        /// <summary>
        /// Validates email format using a simple but effective pattern.
        /// </summary>
        /// <param name="email">The email to validate</param>
        /// <returns>True if the email format is valid</returns>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
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
    }
}
