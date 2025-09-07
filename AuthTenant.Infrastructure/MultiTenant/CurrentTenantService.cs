using AuthTenant.Domain.Interfaces;
using AuthTenant.Shared.Constants;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Infrastructure.MultiTenant
{
    /// <summary>
    /// Serviço responsável por resolver e gerenciar o contexto do tenant atual.
    /// Implementa estratégias múltiplas de resolução: JWT claims, headers HTTP e override manual.
    /// Focado na resolução de tenant ID sem dependências de banco de dados para evitar dependências circulares.
    /// </summary>
    public class CurrentTenantService : ICurrentTenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CurrentTenantService> _logger;

        private string? _tenantIdOverride;

        /// <summary>
        /// Inicializa uma nova instância do CurrentTenantService.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor para o contexto HTTP atual</param>
        /// <param name="logger">Logger para auditoria e debugging</param>
        /// <exception cref="ArgumentNullException">Lançada quando parâmetros obrigatórios são nulos</exception>
        public CurrentTenantService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CurrentTenantService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém o ID do tenant atual usando múltiplas estratégias de resolução.
        /// Ordem de prioridade: Override manual → Context Items → JWT Claims → HTTP Headers → Default
        /// </summary>
        public string? TenantId
        {
            get
            {
                try
                {
                    // 1. Verifica override manual (maior prioridade)
                    if (!string.IsNullOrWhiteSpace(_tenantIdOverride))
                    {
                        _logger.LogDebug("Using tenant override: {TenantId}", _tenantIdOverride);
                        return TenantConstants.NormalizeTenantId(_tenantIdOverride);
                    }

                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext == null)
                    {
                        _logger.LogDebug("No HTTP context available, using default tenant");
                        return TenantConstants.DefaultTenantId;
                    }

                    // 2. Tenta resolver via Context Items (cache de request)
                    if (httpContext.Items.TryGetValue(TenantConstants.TenantIdContextKey, out var contextTenantId) &&
                        contextTenantId is string tenantFromContext &&
                        TenantConstants.IsValidTenantId(tenantFromContext))
                    {
                        _logger.LogDebug("Using tenant from context: {TenantId}", tenantFromContext);
                        return TenantConstants.NormalizeTenantId(tenantFromContext);
                    }

                    // 3. Tenta resolver via JWT Claims
                    var user = httpContext.User;
                    if (user?.Identity?.IsAuthenticated == true)
                    {
                        var tenantClaim = user.FindFirst(TenantConstants.TenantIdClaimType);
                        if (tenantClaim?.Value != null && TenantConstants.IsValidTenantId(tenantClaim.Value))
                        {
                            var normalizedTenantId = TenantConstants.NormalizeTenantId(tenantClaim.Value);
                            _logger.LogDebug("Resolved tenant from JWT claim: {TenantId}", normalizedTenantId);

                            // Cache no context para próximas chamadas na mesma request
                            httpContext.Items[TenantConstants.TenantIdContextKey] = normalizedTenantId;
                            return normalizedTenantId;
                        }
                    }

                    // 4. Tenta resolver via HTTP Header
                    if (httpContext.Request.Headers.TryGetValue(TenantConstants.TenantIdHeaderName, out var headerValues))
                    {
                        var headerValue = headerValues.FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(headerValue) && TenantConstants.IsValidTenantId(headerValue))
                        {
                            var normalizedTenantId = TenantConstants.NormalizeTenantId(headerValue);
                            _logger.LogDebug("Resolved tenant from header: {TenantId}", normalizedTenantId);

                            // Cache no context para próximas chamadas na mesma request
                            httpContext.Items[TenantConstants.TenantIdContextKey] = normalizedTenantId;
                            return normalizedTenantId;
                        }
                    }

                    // 5. Fallback para tenant padrão
                    _logger.LogDebug("No tenant resolved, using default: {TenantId}", TenantConstants.DefaultTenantId);
                    httpContext.Items[TenantConstants.TenantIdContextKey] = TenantConstants.DefaultTenantId;
                    return TenantConstants.DefaultTenantId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolving current tenant, using default");
                    return TenantConstants.DefaultTenantId;
                }
            }
        }

        /// <summary>
        /// Define manualmente o tenant (usado principalmente para testes e overrides).
        /// </summary>
        /// <param name="tenantId">ID do tenant a ser definido</param>
        public void SetTenant(string? tenantId)
        {
            if (tenantId != null && !TenantConstants.IsValidTenantId(tenantId))
            {
                throw new ArgumentException($"Invalid tenant ID format: {tenantId}", nameof(tenantId));
            }

            _tenantIdOverride = tenantId != null ? TenantConstants.NormalizeTenantId(tenantId) : null;
            _logger.LogInformation("Tenant override set to: {TenantId}", _tenantIdOverride ?? "null");
        }

        /// <summary>
        /// Limpa o override manual do tenant.
        /// </summary>
        public void ClearTenantOverride()
        {
            var previousTenant = _tenantIdOverride;
            _tenantIdOverride = null;
            _logger.LogInformation("Tenant override cleared. Previous: {PreviousTenant}", previousTenant ?? "null");
        }

        /// <summary>
        /// Verifica se existe um tenant válido atual.
        /// </summary>
        public bool HasValidTenant => !string.IsNullOrEmpty(TenantId) && TenantId != TenantConstants.DefaultTenantId;

        /// <summary>
        /// Verifica se o tenant atual é o tenant padrão.
        /// </summary>
        public bool IsDefaultTenant => TenantConstants.IsDefaultTenant(TenantId);

        /// <summary>
        /// Obtém informações básicas sobre a resolução do tenant atual.
        /// </summary>
        /// <returns>Informações de diagnóstico</returns>
        public TenantResolutionInfo GetResolutionInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var currentTenantId = TenantId ?? TenantConstants.DefaultTenantId;

            string method = "Unknown";
            bool hasOverride = !string.IsNullOrWhiteSpace(_tenantIdOverride);
            bool hasContext = httpContext?.Items.ContainsKey(TenantConstants.TenantIdContextKey) == true;
            bool hasJwtClaim = httpContext?.User?.FindFirst(TenantConstants.TenantIdClaimType) != null;
            bool hasHeader = httpContext?.Request.Headers.ContainsKey(TenantConstants.TenantIdHeaderName) == true;

            if (hasOverride) method = "Override";
            else if (hasContext) method = "Context";
            else if (hasJwtClaim) method = "JWT";
            else if (hasHeader) method = "Header";
            else method = "Default";

            return new TenantResolutionInfo
            {
                TenantId = currentTenantId,
                ResolutionMethod = method,
                IsDefault = TenantConstants.IsDefaultTenant(currentTenantId),
                HasOverride = hasOverride,
                HasJwtClaim = hasJwtClaim,
                HasHttpHeader = hasHeader
            };
        }
    }

    /// <summary>
    /// Informações sobre a resolução do tenant atual.
    /// </summary>
    public record TenantResolutionInfo
    {
        public required string TenantId { get; init; }
        public required string ResolutionMethod { get; init; }
        public required bool IsDefault { get; init; }
        public required bool HasOverride { get; init; }
        public required bool HasJwtClaim { get; init; }
        public required bool HasHttpHeader { get; init; }
    }
}
