using AutoMapper;
using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Auth;
using AuthTenant.Application.Services.Interfaces;
using AuthTenant.Domain.Entities;
using AuthTenant.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Application.Commands.Auth.Handlers
{
    /// <summary>
    /// Handler for processing user registration commands in a multi-tenant environment.
    /// Implements Clean Architecture and DDD principles for user registration business logic.
    /// Follows SOLID principles with single responsibility and dependency inversion.
    /// </summary>
    public sealed class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<RegisterHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the RegisterHandler with required dependencies
        /// </summary>
        /// <param name="userManager">User manager for identity operations</param>
        /// <param name="tenantRepository">Repository for tenant operations</param>
        /// <param name="tokenService">Service for JWT token generation</param>
        /// <param name="mapper">AutoMapper instance for object mapping</param>
        /// <param name="logger">Logger instance for diagnostic information</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
        public RegisterHandler(
            UserManager<ApplicationUser> userManager,
            ITenantRepository tenantRepository,
            ITokenService tokenService,
            IMapper mapper,
            ILogger<RegisterHandler> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the user registration command following domain-driven design principles
        /// </summary>
        /// <param name="request">The registration command containing user data</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Result containing authentication response or failure message</returns>
        public async Task<Result<AuthResponseDto>> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation(
                "🆕 Processing registration request for user: {Email} in tenant: {TenantId}",
                request.Email,
                request.TenantId
            );

            try
            {
                // Domain validation: Validate command data according to business rules
                var validationResult = await ValidateRegistrationRequestAsync(request, cancellationToken);
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "❌ Registration validation failed for user: {Email}. Reason: {Reason}",
                        request.Email,
                        validationResult.Error
                    );
                    return validationResult;
                }

                // Domain logic: Verify tenant exists and is active
                var tenantValidationResult = await ValidateTenantAsync(request.TenantId, cancellationToken);
                if (!tenantValidationResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "🏢 Tenant validation failed for: {TenantId}. Reason: {Reason}",
                        request.TenantId,
                        tenantValidationResult.Error
                    );
                    return tenantValidationResult;
                }

                // Domain logic: Check for existing user in tenant context
                var userExistsResult = await CheckUserExistsAsync(request.NormalizedEmail, request.TenantId, cancellationToken);
                if (!userExistsResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "👤 User existence check failed for: {Email} in tenant: {TenantId}. Reason: {Reason}",
                        request.Email,
                        request.TenantId,
                        userExistsResult.Error
                    );
                    return userExistsResult;
                }

                // Domain entity creation: Create application user according to domain rules
                var user = CreateApplicationUser(request);

                // Infrastructure operation: Persist user via Identity
                var creationResult = await CreateUserAsync(user, request.Password, cancellationToken);
                if (!creationResult.IsSuccess)
                {
                    _logger.LogError(
                        "🚫 User creation failed for: {Email}. Errors: {Errors}",
                        request.Email,
                        creationResult.Error
                    );
                    return creationResult;
                }

                // Post-registration operations
                await PerformPostRegistrationOperationsAsync(user, cancellationToken);

                // Generate authentication response
                var authResponse = GenerateAuthenticationResponse(user);

                _logger.LogInformation(
                    "✅ User registration completed successfully for: {Email} in tenant: {TenantId}",
                    request.Email,
                    request.TenantId
                );

                return Result<AuthResponseDto>.Success(authResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Unexpected error during registration for user: {Email} in tenant: {TenantId}",
                    request.Email,
                    request.TenantId
                );
                return Result<AuthResponseDto>.Failure("An unexpected error occurred during registration");
            }
        }

        /// <summary>
        /// Validates the registration request according to domain business rules
        /// </summary>
        /// <param name="request">The registration command to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        private async Task<Result<AuthResponseDto>> ValidateRegistrationRequestAsync(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            // Domain validation using command's business rules
            var validationResult = request.GetValidationResult();
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                return Result<AuthResponseDto>.Failure($"Validation failed: {errorMessage}");
            }

            // Additional domain-specific validations can be added here
            await Task.CompletedTask; // Placeholder for future async validations

            return Result<AuthResponseDto>.Success(null!);
        }

        /// <summary>
        /// Validates tenant existence and status according to domain rules
        /// </summary>
        /// <param name="tenantId">The tenant identifier to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        private async Task<Result<AuthResponseDto>> ValidateTenantAsync(
            string tenantId,
            CancellationToken cancellationToken)
        {
            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);

                if (tenant == null)
                {
                    return Result<AuthResponseDto>.Failure("Tenant not found");
                }

                if (!tenant.IsActive)
                {
                    return Result<AuthResponseDto>.Failure("Tenant is not active");
                }

                // Additional tenant validations (subscription status, limits, etc.)
                // This follows DDD principle of keeping business rules in the domain layer

                return Result<AuthResponseDto>.Success(null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tenant: {TenantId}", tenantId);
                return Result<AuthResponseDto>.Failure("Error validating tenant");
            }
        }

        /// <summary>
        /// Checks if user already exists in the tenant context
        /// </summary>
        /// <param name="normalizedEmail">The normalized email to check</param>
        /// <param name="tenantId">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating if user can be created</returns>
        private async Task<Result<AuthResponseDto>> CheckUserExistsAsync(
            string normalizedEmail,
            string tenantId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

                if (existingUser != null &&
                    string.Equals(existingUser.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<AuthResponseDto>.Failure("User already exists in this tenant");
                }

                // User might exist in other tenants, which is allowed in multi-tenant architecture
                return Result<AuthResponseDto>.Success(null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user existence: {Email}", normalizedEmail);
                return Result<AuthResponseDto>.Failure("Error validating user existence");
            }
        }

        /// <summary>
        /// Creates an ApplicationUser entity according to domain rules
        /// </summary>
        /// <param name="request">The registration command</param>
        /// <returns>Configured ApplicationUser entity</returns>
        private static ApplicationUser CreateApplicationUser(RegisterCommand request)
        {
            return new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.NormalizedEmail,
                FirstName = request.FirstName?.Trim() ?? string.Empty,
                LastName = request.LastName?.Trim() ?? string.Empty,
                TenantId = request.TenantId,
                IsActive = true,
                EmailConfirmed = false, // Requires email confirmation
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// Creates the user in the identity system with proper error handling
        /// </summary>
        /// <param name="user">The user entity to create</param>
        /// <param name="password">The user's password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the creation operation</returns>
        private async Task<Result<AuthResponseDto>> CreateUserAsync(
            ApplicationUser user,
            string password,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    var errorMessage = string.Join("; ", errors);

                    _logger.LogWarning(
                        "Identity creation failed for user: {Email}. Errors: {Errors}",
                        user.Email,
                        errorMessage
                    );

                    return Result<AuthResponseDto>.Failure(errorMessage);
                }

                _logger.LogDebug(
                    "User created successfully in Identity system: {UserId}",
                    user.Id
                );

                return Result<AuthResponseDto>.Success(null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user in Identity system: {Email}", user.Email);
                return Result<AuthResponseDto>.Failure("Error creating user account");
            }
        }

        /// <summary>
        /// Performs post-registration operations (email confirmation, welcome emails, etc.)
        /// </summary>
        /// <param name="user">The created user</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task PerformPostRegistrationOperationsAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            try
            {
                // Future implementations:
                // - Send email confirmation
                // - Send welcome email
                // - Create user profile
                // - Assign default roles
                // - Log registration event

                _logger.LogDebug(
                    "Post-registration operations completed for user: {UserId}",
                    user.Id
                );

                await Task.CompletedTask; // Placeholder for future async operations
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Non-critical error in post-registration operations for user: {UserId}",
                    user.Id
                );
                // Non-critical errors shouldn't fail the registration process
            }
        }

        /// <summary>
        /// Generates the authentication response with token and user information
        /// </summary>
        /// <param name="user">The registered user</param>
        /// <returns>Complete authentication response</returns>
        private AuthResponseDto GenerateAuthenticationResponse(ApplicationUser user)
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
