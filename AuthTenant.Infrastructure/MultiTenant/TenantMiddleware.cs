using AuthTenant.Domain.Exceptions;
using AuthTenant.Domain.Interfaces;
using AuthTenant.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AuthTenant.Infrastructure.MultiTenant
{
    /// <summary>
    /// Middleware responsável pela validação e configuração do contexto de tenant.
    /// Valida a existência do tenant e configura o contexto para toda a pipeline de requisição.
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;
        private readonly TenantMiddlewareOptions _options;
        private const string TenantIdContextKey = "TenantId";

        /// <summary>
        /// Inicializa uma nova instância do TenantMiddleware.
        /// </summary>
        /// <param name="next">Próximo middleware na pipeline</param>
        /// <param name="logger">Logger para auditoria e debugging</param>
        /// <param name="options">Opções de configuração do middleware</param>
        /// <exception cref="ArgumentNullException">Lançada quando parâmetros obrigatórios são nulos</exception>
        public TenantMiddleware(
            RequestDelegate next,
            ILogger<TenantMiddleware> logger,
            IOptions<TenantMiddlewareOptions>? options = null)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new TenantMiddlewareOptions();
        }

        /// <summary>
        /// Executa o middleware para validação e configuração do tenant.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição</param>
        /// <param name="tenantService">Serviço de resolução de tenant</param>
        /// <param name="tenantRepository">Repositório para operações de tenant</param>
        /// <exception cref="TenantNotFoundException">Lançada quando tenant não é encontrado</exception>
        public async Task InvokeAsync(
            HttpContext context,
            ICurrentTenantService tenantService,
            ITenantRepository tenantRepository)
        {
            if (context == null)
            {
                _logger.LogError("HttpContext is null");
                throw new ArgumentNullException(nameof(context));
            }

            using var activity = new Activity("TenantMiddleware");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var tenantId = tenantService.TenantId;

                // Log da requisição com informações de tenant
                LogTenantContext(context, tenantId);

                // Pular validação para rotas excluídas
                if (ShouldSkipTenantValidation(context.Request.Path))
                {
                    _logger.LogDebug("Skipping tenant validation for path: {Path}", context.Request.Path);
                    await _next(context);
                    return;
                }

                // Validar tenant se não for o default
                if (!string.IsNullOrEmpty(tenantId) && tenantId != TenantConstants.DefaultTenantId)
                {
                    await ValidateTenantAsync(tenantId, tenantRepository, context.RequestAborted);
                }

                // Adicionar informações de tenant aos headers de resposta (se configurado)
                if (_options.AddTenantToResponseHeaders && !string.IsNullOrEmpty(tenantId))
                {
                    context.Response.Headers.TryAdd("X-Tenant-Id", tenantId);
                }

                // Adicionar tenant ID ao contexto para uso posterior
                context.Items[TenantIdContextKey] = tenantId;

                await _next(context);
            }
            catch (TenantNotFoundException ex)
            {
                _logger.LogWarning(ex, "Tenant not found: {TenantId}", ex.TenantId);
                await HandleTenantNotFoundAsync(context, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in tenant middleware");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogDebug("Tenant middleware completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Valida se o tenant existe e está ativo.
        /// </summary>
        /// <param name="tenantId">ID do tenant a ser validado</param>
        /// <param name="tenantRepository">Repositório para consultas de tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <exception cref="TenantNotFoundException">Tenant não encontrado</exception>
        private async Task ValidateTenantAsync(
            string tenantId,
            ITenantRepository tenantRepository,
            CancellationToken cancellationToken)
        {
            var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);

            if (tenant == null)
            {
                throw new TenantNotFoundException(tenantId);
            }

            if (!tenant.IsActive)
            {
                _logger.LogWarning("Tenant {TenantId} is inactive", tenantId);
                throw new TenantNotFoundException($"Tenant '{tenantId}' is inactive");
            }

            _logger.LogDebug("Tenant validation successful: {TenantId}", tenantId);
        }

        /// <summary>
        /// Verifica se a validação de tenant deve ser pulada para determinadas rotas.
        /// </summary>
        /// <param name="path">Caminho da requisição</param>
        /// <returns>True se deve pular a validação</returns>
        private bool ShouldSkipTenantValidation(PathString path)
        {
            var pathString = path.Value?.ToLowerInvariant() ?? string.Empty;

            return _options.ExcludedPaths.Any(excludedPath =>
                pathString.StartsWith(excludedPath.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Loga informações sobre o contexto do tenant para a requisição atual.
        /// </summary>
        /// <param name="context">Contexto HTTP</param>
        /// <param name="tenantId">ID do tenant</param>
        private void LogTenantContext(HttpContext context, string? tenantId)
        {
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Processing request for tenant {TenantId} from {ClientIp} - {Method} {Path} - User-Agent: {UserAgent}",
                tenantId ?? "null",
                clientIp ?? "unknown",
                context.Request.Method,
                context.Request.Path,
                userAgent ?? "unknown"
            );
        }

        /// <summary>
        /// Manipula exceções de tenant não encontrado.
        /// </summary>
        /// <param name="context">Contexto HTTP</param>
        /// <param name="ex">Exceção de tenant não encontrado</param>
        private async Task HandleTenantNotFoundAsync(HttpContext context, TenantNotFoundException ex)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Tenant not found",
                    tenantId = ex.TenantId,
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        }
    }

    /// <summary>
    /// Opções de configuração para o TenantMiddleware.
    /// </summary>
    public class TenantMiddlewareOptions
    {
        /// <summary>
        /// Rotas que devem ser excluídas da validação de tenant.
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new()
        {
            "/health",
            "/metrics",
            "/swagger",
            "/api/auth/login",
            "/api/auth/register"
        };

        /// <summary>
        /// Se deve adicionar o tenant ID aos headers de resposta.
        /// </summary>
        public bool AddTenantToResponseHeaders { get; set; } = true;

        /// <summary>
        /// Timeout para operações de validação de tenant.
        /// </summary>
        public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}
