using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AuthTenant.Application.Models.Dtos.Auth
{
    /// <summary>
    /// Data Transfer Object for user registration requests.
    /// Contains all necessary information for creating a new user account in a multi-tenant environment.
    /// Includes comprehensive validation rules to ensure data integrity, security, and business compliance.
    /// </summary>
    public sealed class RegisterRequestDto
    {
        /// <summary>
        /// Gets or sets the user's email address.
        /// Must be unique within the tenant and serve as the primary authentication identifier.
        /// Will be verified through an email confirmation process.
        /// </summary>
        /// <example>newuser@example.com</example>
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        [StringLength(256, ErrorMessage = "Email address cannot exceed 256 characters")]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's password.
        /// Must meet complexity requirements for security.
        /// Will be hashed before storage - never stored in plain text.
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
        [DataType(DataType.Password)]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password confirmation.
        /// Must match the password exactly to prevent typos.
        /// Only used for validation and not stored.
        /// </summary>
        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password and confirmation do not match")]
        [JsonPropertyName("confirmPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's first name.
        /// Used for personalization and display purposes.
        /// Should be a proper name without special characters.
        /// </summary>
        /// <example>John</example>
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 50 characters")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s\-'\.]+$", ErrorMessage = "First name contains invalid characters")]
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's last name.
        /// Used for personalization and display purposes.
        /// Should be a proper name without special characters.
        /// </summary>
        /// <example>Doe</example>
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 50 characters")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s\-'\.]+$", ErrorMessage = "Last name contains invalid characters")]
        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tenant identifier for the new user.
        /// Determines which tenant the user will belong to.
        /// Critical for multi-tenant data isolation.
        /// </summary>
        /// <example>tenant-123</example>
        [Required(ErrorMessage = "Tenant ID is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Tenant ID must be between 1 and 50 characters")]
        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's phone number for additional verification.
        /// Optional but recommended for security and communication.
        /// </summary>
        [Phone(ErrorMessage = "Please provide a valid phone number")]
        [StringLength(20)]
        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the user's preferred language/locale.
        /// Used for localization and communication preferences.
        /// </summary>
        /// <example>en-US</example>
        [StringLength(10)]
        [JsonPropertyName("preferredLanguage")]
        public string? PreferredLanguage { get; set; } = "en-US";

        /// <summary>
        /// Gets or sets the timezone preference for the user.
        /// Used for proper timestamp display and scheduling.
        /// </summary>
        /// <example>America/New_York</example>
        [StringLength(50)]
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user accepts the terms of service.
        /// Required for legal compliance and user agreement.
        /// </summary>
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms of service")]
        [JsonPropertyName("acceptTerms")]
        public bool AcceptTerms { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the user opts in to marketing communications.
        /// Optional preference for newsletter and promotional content.
        /// </summary>
        [JsonPropertyName("marketingOptIn")]
        public bool MarketingOptIn { get; set; } = false;

        /// <summary>
        /// Gets or sets the registration source for analytics.
        /// Tracks how users discover and register for the service.
        /// </summary>
        /// <example>website, mobile_app, referral</example>
        [StringLength(50)]
        [JsonPropertyName("registrationSource")]
        public string? RegistrationSource { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the registration.
        /// Can include referral codes, campaign tracking, etc.
        /// </summary>
        [JsonPropertyName("metadata")]
        public IDictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Gets the normalized email address for consistent processing.
        /// Converts to lowercase and trims whitespace.
        /// </summary>
        [JsonIgnore]
        public string NormalizedEmail => Email.Trim().ToLowerInvariant();

        /// <summary>
        /// Gets the user's full name for display purposes.
        /// Combines first and last name with proper formatting.
        /// </summary>
        [JsonIgnore]
        public string FullName => $"{FirstName.Trim()} {LastName.Trim()}".Trim();

        /// <summary>
        /// Gets a value indicating whether the password meets strength requirements.
        /// Checks for complexity beyond just length.
        /// </summary>
        [JsonIgnore]
        public bool HasStrongPassword => IsStrongPassword(Password);

        /// <summary>
        /// Validates the registration request for completeness and security.
        /// </summary>
        /// <returns>True if the request is valid, false otherwise</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(TenantId) &&
                   Password == ConfirmPassword &&
                   AcceptTerms &&
                   IsValidEmail(Email) &&
                   IsStrongPassword(Password) &&
                   IsValidName(FirstName) &&
                   IsValidName(LastName) &&
                   IsValidTenantId(TenantId);
        }

        /// <summary>
        /// Validates email format with comprehensive rules.
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
                return addr.Address == email && !email.Contains("..");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates password strength beyond basic length requirements.
        /// </summary>
        /// <param name="password">The password to validate</param>
        /// <returns>True if the password is strong enough</returns>
        private static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 128)
                return false;

            // Check for at least 3 of 4 character types
            var hasLower = password.Any(char.IsLower);
            var hasUpper = password.Any(char.IsUpper);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            var complexityScore = (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);

            return complexityScore >= 3 && !IsCommonPassword(password);
        }

        /// <summary>
        /// Checks against common weak passwords.
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <returns>True if the password is commonly used (weak)</returns>
        private static bool IsCommonPassword(string password)
        {
            var commonPasswords = new[]
            {
                "password", "123456", "12345678", "qwerty", "abc123",
                "password123", "admin", "letmein", "welcome", "monkey"
            };

            return commonPasswords.Contains(password.ToLowerInvariant());
        }

        /// <summary>
        /// Validates name format and content.
        /// </summary>
        /// <param name="name">The name to validate</param>
        /// <returns>True if the name is valid</returns>
        private static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
                return false;

            // Allow letters, spaces, hyphens, apostrophes, and periods
            return Regex.IsMatch(name, @"^[a-zA-ZÀ-ÿ\s\-'\.]+$");
        }

        /// <summary>
        /// Validates tenant ID format and constraints.
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
        /// Sanitizes the registration request for logging purposes.
        /// Removes sensitive information while preserving important context.
        /// </summary>
        /// <returns>Dictionary with safe-to-log information</returns>
        public IDictionary<string, object> GetLoggingInfo()
        {
            return new Dictionary<string, object>
            {
                ["email"] = Email.Length > 0 ? $"{Email[0]}***@{Email.Split('@').LastOrDefault()}" : "unknown",
                ["firstName"] = FirstName.Length > 0 ? $"{FirstName[0]}***" : "unknown",
                ["lastName"] = LastName.Length > 0 ? $"{LastName[0]}***" : "unknown",
                ["tenantId"] = TenantId,
                ["hasPhoneNumber"] = !string.IsNullOrWhiteSpace(PhoneNumber),
                ["preferredLanguage"] = PreferredLanguage ?? "not-specified",
                ["acceptTerms"] = AcceptTerms,
                ["marketingOptIn"] = MarketingOptIn,
                ["registrationSource"] = RegistrationSource ?? "unknown",
                ["hasStrongPassword"] = HasStrongPassword,
                ["timestamp"] = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a new registration request with essential information.
        /// Factory method to ensure proper initialization.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">User's password</param>
        /// <param name="confirmPassword">Password confirmation</param>
        /// <param name="firstName">User's first name</param>
        /// <param name="lastName">User's last name</param>
        /// <param name="tenantId">Tenant identifier</param>
        /// <returns>Configured RegisterRequestDto instance</returns>
        public static RegisterRequestDto Create(
            string email,
            string password,
            string confirmPassword,
            string firstName,
            string lastName,
            string tenantId)
        {
            return new RegisterRequestDto
            {
                Email = email?.Trim() ?? string.Empty,
                Password = password ?? string.Empty,
                ConfirmPassword = confirmPassword ?? string.Empty,
                FirstName = firstName?.Trim() ?? string.Empty,
                LastName = lastName?.Trim() ?? string.Empty,
                TenantId = tenantId?.Trim() ?? string.Empty
            };
        }
    }
}
