using System.ComponentModel.DataAnnotations;

using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Auth;

using MediatR;

namespace AuthTenant.Application.Commands.Auth
{
    /// <summary>
    /// Command for user registration in a multi-tenant environment.
    /// Represents a request to create a new user account with personal information and tenant context.
    /// Follows DDD Command pattern and Clean Architecture principles.
    /// </summary>
    /// <param name="Email">User's email address (unique identifier)</param>
    /// <param name="Password">User's password for authentication</param>
    /// <param name="FirstName">User's first name</param>
    /// <param name="LastName">User's last name</param>
    /// <param name="TenantId">Tenant identifier for multi-tenant context</param>
    public sealed record RegisterCommand(
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
        string Email,

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
        string Password,

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s'-]+$", ErrorMessage = "First name contains invalid characters")]
        string FirstName,

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s'-]+$", ErrorMessage = "Last name contains invalid characters")]
        string LastName,

        [Required(ErrorMessage = "TenantId is required")]
        [StringLength(50, ErrorMessage = "TenantId cannot exceed 50 characters")]
        string TenantId
    ) : IRequest<Result<AuthResponseDto>>
    {
        /// <summary>
        /// Gets the email in lowercase for consistent processing
        /// </summary>
        public string NormalizedEmail => Email?.ToLowerInvariant() ?? string.Empty;

        /// <summary>
        /// Gets the full name combining first and last names
        /// </summary>
        public string FullName => $"{FirstName?.Trim()} {LastName?.Trim()}".Trim();

        /// <summary>
        /// Gets the username (using email as username)
        /// </summary>
        public string UserName => NormalizedEmail;

        /// <summary>
        /// Validates the command data according to business rules
        /// </summary>
        /// <returns>True if the command is valid according to domain rules</returns>
        public bool IsValid()
        {
            return IsEmailValid() &&
                   IsPasswordValid() &&
                   IsFirstNameValid() &&
                   IsLastNameValid() &&
                   IsTenantIdValid();
        }

        /// <summary>
        /// Validates email format and business rules
        /// </summary>
        private bool IsEmailValid()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   Email.Contains('@') &&
                   Email.Length <= 256 &&
                   Email.Split('@').Length == 2 &&
                   !Email.Split('@')[0].StartsWith('.') &&
                   !Email.Split('@')[0].EndsWith('.');
        }

        /// <summary>
        /// Validates password strength according to security policies
        /// </summary>
        private bool IsPasswordValid()
        {
            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 8)
                return false;

            var hasUpper = Password.Any(char.IsUpper);
            var hasLower = Password.Any(char.IsLower);
            var hasDigit = Password.Any(char.IsDigit);
            var hasSpecial = Password.Any(c => "@$!%*?&".Contains(c));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        /// <summary>
        /// Validates first name according to business rules
        /// </summary>
        private bool IsFirstNameValid()
        {
            return !string.IsNullOrWhiteSpace(FirstName) &&
                   FirstName.Trim().Length >= 2 &&
                   FirstName.Length <= 50 &&
                   FirstName.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c == '\'' || c == '-');
        }

        /// <summary>
        /// Validates last name according to business rules
        /// </summary>
        private bool IsLastNameValid()
        {
            return !string.IsNullOrWhiteSpace(LastName) &&
                   LastName.Trim().Length >= 2 &&
                   LastName.Length <= 50 &&
                   LastName.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c == '\'' || c == '-');
        }

        /// <summary>
        /// Validates tenant ID according to business rules
        /// </summary>
        private bool IsTenantIdValid()
        {
            return !string.IsNullOrWhiteSpace(TenantId) &&
                   TenantId.Length <= 50;
        }

        /// <summary>
        /// Gets a domain-specific validation result with detailed error information
        /// </summary>
        /// <returns>Validation result with specific error messages</returns>
        public ValidationResult GetValidationResult()
        {
            var errors = new List<string>();

            if (!IsEmailValid())
                errors.Add("Email format is invalid or does not meet business requirements");

            if (!IsPasswordValid())
                errors.Add("Password does not meet security requirements (8+ chars, uppercase, lowercase, digit, special character)");

            if (!IsFirstNameValid())
                errors.Add("First name must be 2-50 characters and contain only letters, spaces, apostrophes, or hyphens");

            if (!IsLastNameValid())
                errors.Add("Last name must be 2-50 characters and contain only letters, spaces, apostrophes, or hyphens");

            if (!IsTenantIdValid())
                errors.Add("TenantId is required and cannot exceed 50 characters");

            return new ValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Gets a safe string representation for logging purposes (excludes sensitive data)
        /// </summary>
        /// <returns>Safe string representation for logging and audit</returns>
        public override string ToString()
        {
            return $"RegisterCommand(Email={Email}, FullName={FullName}, TenantId={TenantId})";
        }
    }

    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    /// <param name="IsValid">Indicates if the validation passed</param>
    /// <param name="Errors">Collection of validation error messages</param>
    public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
}
