using AuthTenant.Application.Common;
using AuthTenant.Application.Services.Interfaces;
using AuthTenant.Domain.Entities;
using AuthTenant.Shared.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthTenant.Application.Services
{
    /// <summary>
    /// Serviço enterprise responsável pela geração, validação e manipulação de tokens JWT.
    /// Implementa operações seguras de autenticação com suporte a multi-tenancy e refresh tokens.
    /// Segue padrões de segurança enterprise e práticas recomendadas de JWT.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        // Cache para as configurações JWT para melhor performance
        private readonly Lazy<JwtSettings> _jwtSettings;

        public TokenService(
            IConfiguration configuration,
            ILogger<TokenService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenHandler = new JwtSecurityTokenHandler();

            _jwtSettings = new Lazy<JwtSettings>(() => GetJwtSettings());
        }

        /// <summary>
        /// Gera um token JWT para o usuário especificado com claims personalizados e validações de segurança.
        /// </summary>
        public async Task<Result<string>> GenerateJwtTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validação de entrada
                if (user == null)
                {
                    _logger.LogWarning("Attempted to generate JWT token for null user");
                    return Result<string>.Failure("User cannot be null");
                }

                if (string.IsNullOrWhiteSpace(user.Id))
                {
                    _logger.LogWarning("Attempted to generate JWT token for user without ID");
                    return Result<string>.Failure("User ID is required");
                }

                if (string.IsNullOrWhiteSpace(user.TenantId))
                {
                    _logger.LogWarning("Attempted to generate JWT token for user {UserId} without tenant", user.Id);
                    return Result<string>.Failure("Tenant ID is required");
                }

                _logger.LogDebug("Generating JWT token for user {UserId} in tenant {TenantId}", user.Id, user.TenantId);

                // Construção dos claims com validação
                var claims = await BuildUserClaimsAsync(user, cancellationToken);
                var settings = _jwtSettings.Value;

                // Criação do token descriptor
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(settings.ExpiryInMinutes),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(settings.SecretKey),
                        SecurityAlgorithms.HmacSha256Signature),
                    Issuer = settings.Issuer,
                    Audience = settings.Audience,
                    IssuedAt = DateTime.UtcNow,
                    NotBefore = DateTime.UtcNow
                };

                // Geração do token
                var token = _tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = _tokenHandler.WriteToken(token);

                _logger.LogInformation("JWT token successfully generated for user {UserId}", user.Id);

                return Result<string>.Success(tokenString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", user?.Id ?? "null");
                return Result<string>.Failure("Failed to generate JWT token");
            }
        }

        /// <summary>
        /// Extrai o ClaimsPrincipal de um token JWT expirado para operações de refresh.
        /// </summary>
        public Task<Result<ClaimsPrincipal>> GetPrincipalFromExpiredTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validação de entrada
                var tokenValidationResult = ValidateTokenInput(token);
                if (!tokenValidationResult.IsSuccess)
                {
                    return Task.FromResult(Result<ClaimsPrincipal>.Failure(tokenValidationResult.Error ?? "Invalid token input"));
                }

                _logger.LogDebug("Extracting principal from expired token");

                var settings = _jwtSettings.Value;
                var tokenValidationParameters = CreateTokenValidationParameters(settings, validateLifetime: false);

                // Validação do token sem verificar expiração
                var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                // Validação adicional do algoritmo de segurança
                if (!IsValidSecurityToken(securityToken))
                {
                    _logger.LogWarning("Invalid token algorithm detected");
                    return Task.FromResult(Result<ClaimsPrincipal>.Failure("Invalid token algorithm"));
                }

                _logger.LogDebug("Principal successfully extracted from expired token");

                return Task.FromResult(Result<ClaimsPrincipal>.Success(principal));
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Security token validation failed");
                return Task.FromResult(Result<ClaimsPrincipal>.Failure("Invalid token format"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting principal from expired token");
                return Task.FromResult(Result<ClaimsPrincipal>.Failure("Failed to process token"));
            }
        }

        /// <summary>
        /// Valida um token JWT ativo verificando assinatura, expiração e estrutura.
        /// </summary>
        public Task<Result<ClaimsPrincipal>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validação de entrada
                var tokenValidationResult = ValidateTokenInput(token);
                if (!tokenValidationResult.IsSuccess)
                {
                    return Task.FromResult(Result<ClaimsPrincipal>.Failure(tokenValidationResult.Error ?? "Invalid token input"));
                }

                _logger.LogDebug("Validating active JWT token");

                var settings = _jwtSettings.Value;
                var tokenValidationParameters = CreateTokenValidationParameters(settings, validateLifetime: true);

                // Validação completa do token
                var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                // Validação adicional do algoritmo
                if (!IsValidSecurityToken(securityToken))
                {
                    _logger.LogWarning("Invalid token algorithm detected during validation");
                    return Task.FromResult(Result<ClaimsPrincipal>.Failure("Invalid token algorithm"));
                }

                _logger.LogDebug("Token successfully validated");

                return Task.FromResult(Result<ClaimsPrincipal>.Success(principal));
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogDebug("Token validation failed: token expired");
                return Task.FromResult(Result<ClaimsPrincipal>.Failure("Token has expired"));
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Token validation failed: security token exception");
                return Task.FromResult(Result<ClaimsPrincipal>.Failure("Invalid token"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return Task.FromResult(Result<ClaimsPrincipal>.Failure("Failed to validate token"));
            }
        }

        /// <summary>
        /// Gera um refresh token criptograficamente seguro para renovação de autenticação.
        /// </summary>
        public Task<Result<string>> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Generating refresh token");

                // Geração de token aleatório seguro
                var randomBytes = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomBytes);

                var refreshToken = Convert.ToBase64String(randomBytes);

                _logger.LogDebug("Refresh token successfully generated");

                return Task.FromResult(Result<string>.Success(refreshToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token");
                return Task.FromResult(Result<string>.Failure("Failed to generate refresh token"));
            }
        }

        /// <summary>
        /// Extrai informações específicas do tenant do token JWT.
        /// </summary>
        public async Task<Result<string>> GetTenantIdFromTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var principalResult = await ValidateTokenAsync(token, cancellationToken);
                if (!principalResult.IsSuccess)
                {
                    return Result<string>.Failure(principalResult.Error ?? "Failed to validate token");
                }

                var tenantId = principalResult.Data?.FindFirst(TenantConstants.TenantIdClaimType)?.Value;

                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    _logger.LogWarning("Tenant ID not found in token");
                    return Result<string>.Failure("Tenant ID not found in token");
                }

                return Result<string>.Success(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting tenant ID from token");
                return Result<string>.Failure("Failed to extract tenant ID");
            }
        }

        /// <summary>
        /// Extrai o ID do usuário do token JWT.
        /// </summary>
        public async Task<Result<string>> GetUserIdFromTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var principalResult = await ValidateTokenAsync(token, cancellationToken);
                if (!principalResult.IsSuccess)
                {
                    return Result<string>.Failure(principalResult.Error ?? "Failed to validate token");
                }

                var userId = principalResult.Data?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID not found in token");
                    return Result<string>.Failure("User ID not found in token");
                }

                return Result<string>.Success(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user ID from token");
                return Result<string>.Failure("Failed to extract user ID");
            }
        }

        /// <summary>
        /// Verifica se um token JWT está expirado sem validar outras propriedades.
        /// </summary>
        public Task<Result<bool>> IsTokenExpiredAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var tokenValidationResult = ValidateTokenInput(token);
                if (!tokenValidationResult.IsSuccess)
                {
                    return Task.FromResult(Result<bool>.Failure(tokenValidationResult.Error ?? "Invalid token input"));
                }

                var jwtToken = _tokenHandler.ReadJwtToken(token);
                var isExpired = jwtToken.ValidTo < DateTime.UtcNow;

                _logger.LogDebug("Token expiration check: {IsExpired} (Expires: {ExpiryTime})", isExpired, jwtToken.ValidTo);

                return Task.FromResult(Result<bool>.Success(isExpired));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token expiration");
                return Task.FromResult(Result<bool>.Failure("Failed to check token expiration"));
            }
        }

        #region Legacy Support (Backward Compatibility)

        /// <summary>
        /// Gera um token JWT para o usuário especificado.
        /// [DEPRECATED] Use GenerateJwtTokenAsync instead.
        /// </summary>
        [Obsolete("Use GenerateJwtTokenAsync para melhor controle de erro e operações assíncronas.", false)]
        public string GenerateJwtToken(ApplicationUser user)
        {
            var result = GenerateJwtTokenAsync(user).GetAwaiter().GetResult();

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.Error ?? "Failed to generate JWT token");
            }

            return result.Data ?? throw new InvalidOperationException("Generated token is null");
        }

        /// <summary>
        /// Extrai o ClaimsPrincipal de um token JWT expirado.
        /// [DEPRECATED] Use GetPrincipalFromExpiredTokenAsync instead.
        /// </summary>
        [Obsolete("Use GetPrincipalFromExpiredTokenAsync para melhor controle de erro e operações assíncronas.", false)]
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var result = GetPrincipalFromExpiredTokenAsync(token).GetAwaiter().GetResult();

            if (!result.IsSuccess)
            {
                throw new SecurityTokenException(result.Error ?? "Failed to extract principal from expired token");
            }

            return result.Data;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Constrói a lista de claims do usuário com validação e logs de auditoria.
        /// </summary>
        private Task<IList<Claim>> BuildUserClaimsAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
                new(ClaimTypes.Surname, user.LastName ?? string.Empty),
                new(TenantConstants.TenantIdClaimType, user.TenantId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            // Log para auditoria (sem dados sensíveis)
            _logger.LogDebug("Built {ClaimCount} claims for user {UserId}", claims.Count, user.Id);

            return Task.FromResult<IList<Claim>>(claims);
        }

        /// <summary>
        /// Obtém e valida as configurações JWT do appsettings.
        /// </summary>
        private JwtSettings GetJwtSettings()
        {
            var jwtSection = _configuration.GetSection("JwtSettings");

            var secret = jwtSection["Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("JWT Secret not configured in appsettings");
            }

            var issuer = jwtSection["Issuer"];
            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new InvalidOperationException("JWT Issuer not configured in appsettings");
            }

            var audience = jwtSection["Audience"];
            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException("JWT Audience not configured in appsettings");
            }

            if (!int.TryParse(jwtSection["ExpiryInMinutes"], out var expiryInMinutes) || expiryInMinutes <= 0)
            {
                expiryInMinutes = 60; // Fallback para 1 hora
                _logger.LogWarning("Invalid or missing JWT ExpiryInMinutes, using default value: {DefaultExpiry}", expiryInMinutes);
            }

            return new JwtSettings
            {
                SecretKey = Encoding.UTF8.GetBytes(secret),
                Issuer = issuer,
                Audience = audience,
                ExpiryInMinutes = expiryInMinutes
            };
        }

        /// <summary>
        /// Cria parâmetros de validação de token com configurações de segurança apropriadas.
        /// </summary>
        private TokenValidationParameters CreateTokenValidationParameters(JwtSettings settings, bool validateLifetime)
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(settings.SecretKey),
                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.FromMinutes(5), // Tolerância de 5 minutos para diferenças de relógio
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };
        }

        /// <summary>
        /// Valida a entrada do token verificando nulidade e formato básico.
        /// </summary>
        private Result<bool> ValidateTokenInput(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token validation attempted with null or empty token");
                return Result<bool>.Failure("Token cannot be null or empty");
            }

            // Verificação básica de formato JWT (3 partes separadas por pontos)
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                _logger.LogWarning("Token validation attempted with invalid JWT format");
                return Result<bool>.Failure("Invalid token format");
            }

            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Valida se o token de segurança usa o algoritmo apropriado.
        /// </summary>
        private static bool IsValidSecurityToken(SecurityToken securityToken)
        {
            return securityToken is JwtSecurityToken jwtSecurityToken &&
                   jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion

        #region Inner Classes

        /// <summary>
        /// Classe interna para configurações JWT tipadas e validadas.
        /// </summary>
        private sealed class JwtSettings
        {
            public byte[] SecretKey { get; init; } = Array.Empty<byte>();
            public string Issuer { get; init; } = string.Empty;
            public string Audience { get; init; } = string.Empty;
            public int ExpiryInMinutes { get; init; }
        }

        #endregion
    }
}
