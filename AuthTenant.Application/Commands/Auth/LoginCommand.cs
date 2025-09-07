using System.ComponentModel.DataAnnotations;

using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Auth;

using MediatR;

namespace AuthTenant.Application.Commands.Auth
{
    /// <summary>
    /// Command for user authentication and login process.
    /// Represents a request to authenticate a user with email, password, and tenant context.
    /// </summary>
    /// <param name="Email">User's email address for authentication</param>
    /// <param name="Password">User's password for authentication</param>
    /// <param name="TenantId">Tenant identifier for multi-tenant context</param>
    public sealed record LoginCommand(
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
        string Email,

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        string Password,

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
        /// Validates the command data and returns validation results
        /// </summary>
        /// <returns>True if the command is valid, false otherwise</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(TenantId) &&
                   Email.Contains('@') &&
                   Password.Length >= 6;
        }

        /// <summary>
        /// Gets a string representation for logging purposes (excludes sensitive data)
        /// </summary>
        /// <returns>Safe string representation for logging</returns>
        public override string ToString()
        {
            return $"LoginCommand(Email={Email}, TenantId={TenantId})";
        }
    }
}
