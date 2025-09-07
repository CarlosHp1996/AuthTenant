namespace AuthTenant.Shared.Constants
{
    /// <summary>
    /// Constantes relacionadas ao sistema de multi-tenancy.
    /// Centraliza todas as configurações e identificadores utilizados
    /// para resolução, validação e gerenciamento de tenants.
    /// </summary>
    public static class TenantConstants
    {
        #region Tenant Identification

        /// <summary>
        /// ID do tenant padrão usado quando nenhum tenant específico é identificado.
        /// Utilizado como fallback em cenários onde tenant resolution falha.
        /// </summary>
        public const string DefaultTenantId = "default";

        /// <summary>
        /// Nome do header HTTP usado para passar o ID do tenant nas requisições.
        /// Padrão: X-Tenant-Id
        /// </summary>
        public const string TenantIdHeaderName = "X-Tenant-Id";

        /// <summary>
        /// Tipo do claim JWT que contém o ID do tenant.
        /// Usado para extrair tenant de tokens de autenticação.
        /// </summary>
        public const string TenantIdClaimType = "tenant_id";

        /// <summary>
        /// Chave usada para armazenar o tenant ID no HttpContext.Items.
        /// Permite acesso ao tenant atual durante toda a pipeline de requisição.
        /// </summary>
        public const string TenantIdContextKey = "TenantId";

        /// <summary>
        /// Nome do claim que contém o nome do tenant (opcional).
        /// Útil para logging e auditoria.
        /// </summary>
        public const string TenantNameClaimType = "tenant_name";

        #endregion

        #region Tenant Validation

        /// <summary>
        /// Comprimento mínimo permitido para ID de tenant.
        /// </summary>
        public const int MinTenantIdLength = 1;

        /// <summary>
        /// Comprimento máximo permitido para ID de tenant.
        /// </summary>
        public const int MaxTenantIdLength = 50;

        /// <summary>
        /// Comprimento mínimo permitido para nome de tenant.
        /// </summary>
        public const int MinTenantNameLength = 1;

        /// <summary>
        /// Comprimento máximo permitido para nome de tenant.
        /// </summary>
        public const int MaxTenantNameLength = 100;

        /// <summary>
        /// Pattern regex para validação de ID de tenant.
        /// Permite apenas caracteres alfanuméricos, hífen e underscore.
        /// </summary>
        public const string TenantIdValidationPattern = @"^[a-zA-Z0-9\-_]+$";

        /// <summary>
        /// Pattern regex para validação de nome de tenant.
        /// Permite caracteres alfanuméricos, espaços, hífen e underscore.
        /// </summary>
        public const string TenantNameValidationPattern = @"^[a-zA-Z0-9\s\-_]+$";

        #endregion

        #region Cache Keys

        /// <summary>
        /// Prefixo para chaves de cache relacionadas a tenants.
        /// </summary>
        public const string CacheKeyPrefix = "tenant";

        /// <summary>
        /// Template para chave de cache de tenant por ID.
        /// Uso: string.Format(TenantByIdCacheKeyTemplate, tenantId)
        /// </summary>
        public const string TenantByIdCacheKeyTemplate = "tenant_id_{0}";

        /// <summary>
        /// Template para chave de cache de tenant por nome.
        /// Uso: string.Format(TenantByNameCacheKeyTemplate, tenantName)
        /// </summary>
        public const string TenantByNameCacheKeyTemplate = "tenant_name_{0}";

        /// <summary>
        /// Chave de cache para lista de tenants ativos.
        /// </summary>
        public const string ActiveTenantsCacheKey = "active_tenants";

        /// <summary>
        /// Chave de cache para estatísticas de tenants.
        /// </summary>
        public const string TenantStatisticsCacheKey = "tenant_statistics";

        #endregion

        #region Configuration Keys

        /// <summary>
        /// Chave de configuração para habilitar/desabilitar multi-tenancy.
        /// </summary>
        public const string MultiTenancyEnabledConfigKey = "MultiTenancy:Enabled";

        /// <summary>
        /// Chave de configuração para estratégia de resolução de tenant.
        /// Valores: "Header", "Claim", "Subdomain", "Path"
        /// </summary>
        public const string TenantResolutionStrategyConfigKey = "MultiTenancy:ResolutionStrategy";

        /// <summary>
        /// Chave de configuração para duração do cache de tenant em minutos.
        /// </summary>
        public const string TenantCacheDurationConfigKey = "MultiTenancy:CacheDurationMinutes";

        /// <summary>
        /// Chave de configuração para habilitar logging de operações de tenant.
        /// </summary>
        public const string TenantLoggingEnabledConfigKey = "MultiTenancy:LoggingEnabled";

        #endregion

        #region Error Messages

        /// <summary>
        /// Mensagem de erro quando tenant não é encontrado.
        /// </summary>
        public const string TenantNotFoundErrorMessage = "Tenant '{0}' was not found.";

        /// <summary>
        /// Mensagem de erro quando tenant está inativo.
        /// </summary>
        public const string TenantInactiveErrorMessage = "Tenant '{0}' is currently inactive.";

        /// <summary>
        /// Mensagem de erro quando ID de tenant é inválido.
        /// </summary>
        public const string InvalidTenantIdErrorMessage = "Tenant ID '{0}' is invalid. Must match pattern: {1}";

        /// <summary>
        /// Mensagem de erro quando nome de tenant é inválido.
        /// </summary>
        public const string InvalidTenantNameErrorMessage = "Tenant name '{0}' is invalid. Must match pattern: {1}";

        /// <summary>
        /// Mensagem de erro quando tenant ID já existe.
        /// </summary>
        public const string TenantIdExistsErrorMessage = "Tenant with ID '{0}' already exists.";

        /// <summary>
        /// Mensagem de erro quando nome de tenant já existe.
        /// </summary>
        public const string TenantNameExistsErrorMessage = "Tenant with name '{0}' already exists.";

        #endregion

        #region HTTP Headers

        /// <summary>
        /// Header personalizado para retornar informações de tenant na resposta.
        /// </summary>
        public const string TenantInfoResponseHeader = "X-Tenant-Info";

        /// <summary>
        /// Header para indicar se o tenant foi resolvido com sucesso.
        /// </summary>
        public const string TenantResolvedHeader = "X-Tenant-Resolved";

        /// <summary>
        /// Header para informar a estratégia usada para resolver o tenant.
        /// </summary>
        public const string TenantResolutionMethodHeader = "X-Tenant-Resolution-Method";

        #endregion

        #region Roles and Permissions

        /// <summary>
        /// Role para administradores de tenant.
        /// </summary>
        public const string TenantAdminRole = "TenantAdmin";

        /// <summary>
        /// Role para usuários normais de tenant.
        /// </summary>
        public const string TenantUserRole = "TenantUser";

        /// <summary>
        /// Role para administradores do sistema (acesso a todos os tenants).
        /// </summary>
        public const string SystemAdminRole = "SystemAdmin";

        /// <summary>
        /// Permission para criar tenants.
        /// </summary>
        public const string CreateTenantPermission = "tenant:create";

        /// <summary>
        /// Permission para ler dados de tenant.
        /// </summary>
        public const string ReadTenantPermission = "tenant:read";

        /// <summary>
        /// Permission para atualizar tenant.
        /// </summary>
        public const string UpdateTenantPermission = "tenant:update";

        /// <summary>
        /// Permission para deletar tenant.
        /// </summary>
        public const string DeleteTenantPermission = "tenant:delete";

        #endregion

        #region Database

        /// <summary>
        /// Nome da tabela de tenants no banco de dados.
        /// </summary>
        public const string TenantsTableName = "Tenants";

        /// <summary>
        /// Nome da coluna de tenant ID nas tabelas multi-tenant.
        /// </summary>
        public const string TenantIdColumnName = "TenantId";

        /// <summary>
        /// Índice para tenant ID nas tabelas multi-tenant.
        /// </summary>
        public const string TenantIdIndexName = "IX_TenantId";

        #endregion

        #region Resolution Strategies

        /// <summary>
        /// Estratégia de resolução via header HTTP.
        /// </summary>
        public const string HeaderResolutionStrategy = "Header";

        /// <summary>
        /// Estratégia de resolução via JWT claim.
        /// </summary>
        public const string ClaimResolutionStrategy = "Claim";

        /// <summary>
        /// Estratégia de resolução via subdomínio.
        /// </summary>
        public const string SubdomainResolutionStrategy = "Subdomain";

        /// <summary>
        /// Estratégia de resolução via path da URL.
        /// </summary>
        public const string PathResolutionStrategy = "Path";

        /// <summary>
        /// Estratégia de resolução via query string.
        /// </summary>
        public const string QueryStringResolutionStrategy = "QueryString";

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gera uma chave de cache para tenant por ID.
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <returns>Chave de cache formatada</returns>
        public static string GetTenantByIdCacheKey(string tenantId)
        {
            return string.Format(TenantByIdCacheKeyTemplate, tenantId?.ToLowerInvariant() ?? string.Empty);
        }

        /// <summary>
        /// Gera uma chave de cache para tenant por nome.
        /// </summary>
        /// <param name="tenantName">Nome do tenant</param>
        /// <returns>Chave de cache formatada</returns>
        public static string GetTenantByNameCacheKey(string tenantName)
        {
            return string.Format(TenantByNameCacheKeyTemplate, tenantName?.ToLowerInvariant() ?? string.Empty);
        }

        /// <summary>
        /// Valida se um ID de tenant está no formato correto.
        /// </summary>
        /// <param name="tenantId">ID do tenant a ser validado</param>
        /// <returns>True se válido, false caso contrário</returns>
        public static bool IsValidTenantId(string? tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return false;

            if (tenantId.Length < MinTenantIdLength || tenantId.Length > MaxTenantIdLength)
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(tenantId, TenantIdValidationPattern);
        }

        /// <summary>
        /// Valida se um nome de tenant está no formato correto.
        /// </summary>
        /// <param name="tenantName">Nome do tenant a ser validado</param>
        /// <returns>True se válido, false caso contrário</returns>
        public static bool IsValidTenantName(string? tenantName)
        {
            if (string.IsNullOrWhiteSpace(tenantName))
                return false;

            if (tenantName.Length < MinTenantNameLength || tenantName.Length > MaxTenantNameLength)
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(tenantName, TenantNameValidationPattern);
        }

        /// <summary>
        /// Verifica se é o tenant padrão.
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <returns>True se for o tenant padrão</returns>
        public static bool IsDefaultTenant(string? tenantId)
        {
            return string.Equals(tenantId, DefaultTenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Normaliza um ID de tenant para formato consistente.
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <returns>ID normalizado</returns>
        public static string? NormalizeTenantId(string? tenantId)
        {
            return tenantId?.Trim().ToLowerInvariant();
        }

        #endregion
    }
}
