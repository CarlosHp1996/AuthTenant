using System.Security.Claims;

using AuthTenant.Application.Common;
using AuthTenant.Domain.Entities;

namespace AuthTenant.Application.Services.Interfaces
{
    /// <summary>
    /// Interface que define os contratos para serviços de geração e validação de tokens JWT.
    /// Responsável por operações de autenticação e autorização baseadas em tokens seguros.
    /// Implementa padrões enterprise para multi-tenancy e gestão de refresh tokens.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Gera um token JWT seguro para o usuário especificado.
        /// </summary>
        /// <param name="user">O usuário para o qual o token será gerado</param>
        /// <param name="cancellationToken">Token de cancelamento para operações assíncronas</param>
        /// <returns>Result contendo o token JWT gerado ou falha em caso de erro</returns>
        /// <exception cref="ArgumentNullException">Quando o usuário é nulo</exception>
        /// <exception cref="InvalidOperationException">Quando a configuração JWT está inválida</exception>
        Task<Result<string>> GenerateJwtTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extrai o ClaimsPrincipal de um token JWT expirado para fins de refresh.
        /// </summary>
        /// <param name="token">O token JWT expirado</param>
        /// <param name="cancellationToken">Token de cancelamento para operações assíncronas</param>
        /// <returns>Result contendo o ClaimsPrincipal extraído ou falha em caso de erro</returns>
        /// <exception cref="ArgumentNullException">Quando o token é nulo ou vazio</exception>
        /// <exception cref="SecurityTokenException">Quando o token é inválido</exception>
        Task<Result<ClaimsPrincipal>> GetPrincipalFromExpiredTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida um token JWT ativo verificando assinatura, expiração e estrutura.
        /// </summary>
        /// <param name="token">O token JWT a ser validado</param>
        /// <param name="cancellationToken">Token de cancelamento para operações assíncronas</param>
        /// <returns>Result contendo o ClaimsPrincipal se válido ou falha em caso de erro</returns>
        /// <exception cref="ArgumentNullException">Quando o token é nulo ou vazio</exception>
        /// <exception cref="SecurityTokenValidationException">Quando o token é inválido ou expirado</exception>
        Task<Result<ClaimsPrincipal>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gera um refresh token criptograficamente seguro para renovação de autenticação.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento para operações assíncronas</param>
        /// <returns>Result contendo o refresh token gerado</returns>
        Task<Result<string>> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Extrai informações específicas do tenant do token JWT para operações multi-tenant.
        /// </summary>
        /// <param name="token">O token JWT</param>
        /// <param name="cancellationToken">Token de cancelamento para operações assíncronas</param>
        /// <returns>Result contendo o ID do tenant extraído</returns>
        /// <exception cref="ArgumentNullException">Quando o token é nulo ou vazio</exception>
        Task<Result<string>> GetTenantIdFromTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extrai o ID do usuário do token JWT.
        /// </summary>
        /// <param name="token">O token JWT</param>
        /// <param name="cancellationToken">Token de cancelamento para operações assíncronas</param>
        /// <returns>Result contendo o ID do usuário extraído</returns>
        /// <exception cref="ArgumentNullException">Quando o token é nulo ou vazio</exception>
        Task<Result<string>> GetUserIdFromTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se um token JWT está expirado sem validar outras propriedades.
        /// </summary>
        /// <param name="token">O token JWT a ser verificado</param>
        /// <param name="cancellationToken">Token de cancelamento para operações assíncronas</param>
        /// <returns>Result contendo true se o token está expirado, false caso contrário</returns>
        Task<Result<bool>> IsTokenExpiredAsync(string token, CancellationToken cancellationToken = default);

        #region Legacy Support (Manter compatibilidade temporariamente)

        /// <summary>
        /// Gera um token JWT para o usuário especificado.
        /// [DEPRECATED] Use GenerateJwtTokenAsync instead.
        /// </summary>
        [Obsolete("Use GenerateJwtTokenAsync para melhor controle de erro e operações assíncronas.", false)]
        string GenerateJwtToken(ApplicationUser user);

        /// <summary>
        /// Extrai o ClaimsPrincipal de um token JWT expirado.
        /// [DEPRECATED] Use GetPrincipalFromExpiredTokenAsync instead.
        /// </summary>
        [Obsolete("Use GetPrincipalFromExpiredTokenAsync para melhor controle de erro e operações assíncronas.", false)]
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

        #endregion
    }
}
