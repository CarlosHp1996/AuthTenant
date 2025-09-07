using AutoMapper;
using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Auth;
using AuthTenant.Application.Services.Interfaces;
using AuthTenant.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Application.Commands.Auth.Handlers
{
    /// <summary>
    /// Handler for processing user login commands.
    /// Manages user authentication, validation, and token generation in a multi-tenant environment.
    /// </summary>
    public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<LoginHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the LoginHandler
        /// </summary>
        /// <param name="userManager">User manager for identity operations</param>
        /// <param name="tokenService">Service for JWT token generation</param>
        /// <param name="mapper">AutoMapper instance for object mapping</param>
        /// <param name="logger">Logger instance for diagnostic information</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
        public LoginHandler(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            IMapper mapper,
            ILogger<LoginHandler> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the login command and authenticates the user
        /// </summary>
        /// <param name="request">The login command containing user credentials</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Result containing authentication response or failure message</returns>
        public async Task<Result<AuthResponseDto>> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation(
                "Processing login attempt for user: {Email} in tenant: {TenantId}",
                request.Email,
                request.TenantId
            );

            try
            {
                // Validate command data
                if (!request.IsValid())
                {
                    _logger.LogWarning(
                        "Invalid login request data for user: {Email}",
                        request.Email
                    );
                    return Result<AuthResponseDto>.Failure("Invalid request data");
                }

                // Find user by email
                var user = await FindUserByEmailAsync(request.NormalizedEmail, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning(
                        "User not found: {Email} in tenant: {TenantId}",
                        request.Email,
                        request.TenantId
                    );
                    return Result<AuthResponseDto>.Failure("Invalid email or password");
                }

                // Validate tenant context
                if (!ValidateTenantContext(user, request.TenantId))
                {
                    _logger.LogWarning(
                        "Tenant mismatch for user: {Email}. Expected: {ExpectedTenant}, Provided: {ProvidedTenant}",
                        request.Email,
                        user.TenantId,
                        request.TenantId
                    );
                    return Result<AuthResponseDto>.Failure("Invalid email or password");
                }

                // Check user account status
                var accountStatusResult = ValidateAccountStatus(user);
                if (!accountStatusResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Account validation failed for user: {Email}. Reason: {Reason}",
                        request.Email,
                        accountStatusResult.Error
                    );
                    return accountStatusResult;
                }

                // Validate password
                var passwordValid = await ValidatePasswordAsync(user, request.Password);
                if (!passwordValid)
                {
                    _logger.LogWarning(
                        "Invalid password attempt for user: {Email}",
                        request.Email
                    );
                    return Result<AuthResponseDto>.Failure("Invalid email or password");
                }

                // Update user login information
                await UpdateLastLoginAsync(user, cancellationToken);

                // Generate authentication response
                var authResponse = GenerateAuthResponse(user);

                _logger.LogInformation(
                    "Successful login for user: {Email} in tenant: {TenantId}",
                    request.Email,
                    request.TenantId
                );

                return Result<AuthResponseDto>.Success(authResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error during login for user: {Email} in tenant: {TenantId}",
                    request.Email,
                    request.TenantId
                );
                return Result<AuthResponseDto>.Failure("An unexpected error occurred during login");
            }
        }

        /// <summary>
        /// Finds a user by their email address
        /// </summary>
        /// <param name="normalizedEmail">The normalized email address</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The user if found, null otherwise</returns>
        private async Task<ApplicationUser?> FindUserByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _userManager.FindByEmailAsync(normalizedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error finding user by email: {Email}",
                    normalizedEmail
                );
                return null;
            }
        }

        /// <summary>
        /// Validates that the user belongs to the correct tenant
        /// </summary>
        /// <param name="user">The user to validate</param>
        /// <param name="requestedTenantId">The requested tenant ID</param>
        /// <returns>True if the tenant context is valid</returns>
        private static bool ValidateTenantContext(ApplicationUser user, string requestedTenantId)
        {
            return string.Equals(user.TenantId, requestedTenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates the user account status and permissions
        /// </summary>
        /// <param name="user">The user to validate</param>
        /// <returns>Result indicating if the account is valid</returns>
        private static Result<AuthResponseDto> ValidateAccountStatus(ApplicationUser user)
        {
            if (!user.IsActive)
            {
                return Result<AuthResponseDto>.Failure("User account is inactive");
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                return Result<AuthResponseDto>.Failure("User account is temporarily locked");
            }

            if (!user.EmailConfirmed)
            {
                return Result<AuthResponseDto>.Failure("Email address is not confirmed");
            }

            return Result<AuthResponseDto>.Success(null!);
        }

        /// <summary>
        /// Validates the user's password
        /// </summary>
        /// <param name="user">The user to validate</param>
        /// <param name="password">The password to check</param>
        /// <returns>True if the password is valid</returns>
        private async Task<bool> ValidatePasswordAsync(ApplicationUser user, string password)
        {
            try
            {
                return await _userManager.CheckPasswordAsync(user, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error validating password for user: {UserId}",
                    user.Id
                );
                return false;
            }
        }

        /// <summary>
        /// Updates the user's last login timestamp
        /// </summary>
        /// <param name="user">The user to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task UpdateLastLoginAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            try
            {
                user.LastLogin = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogDebug(
                    "Updated last login for user: {UserId}",
                    user.Id
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to update last login for user: {UserId}",
                    user.Id
                );
                // Non-critical error, continue with login process
            }
        }

        /// <summary>
        /// Generates the authentication response with token and user information
        /// </summary>
        /// <param name="user">The authenticated user</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Complete authentication response</returns>
        private AuthResponseDto GenerateAuthResponse(ApplicationUser user)
        {
            try
            {
                var token = _tokenService.GenerateJwtToken(user);
                var userDto = _mapper.Map<UserDto>(user);

                return new AuthResponseDto
                {
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // Should match token expiry configuration
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error generating authentication response for user: {UserId}",
                    user.Id
                );
                throw;
            }
        }
    }
}
