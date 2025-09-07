using System.ComponentModel.DataAnnotations;

namespace AuthTenant.Domain.Exceptions
{
    /// <summary>
    /// Exceção lançada quando um tenant solicitado não é encontrado no sistema.
    /// Indica que o identificador do tenant é inválido, não existe ou foi removido.
    /// Usada em operações que requerem validação de existência de tenant.
    /// </summary>
    public sealed class TenantNotFoundException : DomainException
    {
        /// <summary>
        /// Identificador do tenant que não foi encontrado.
        /// Preservado para auditoria e debugging.
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string RequestedTenantId { get; }

        /// <summary>
        /// Tipo de busca que foi realizada (ById, ByName, ByDomain, etc.).
        /// Útil para diagnosticar diferentes cenários de falha.
        /// </summary>
        [StringLength(20)]
        public string SearchType { get; }

        /// <summary>
        /// Valor utilizado na busca (pode ser diferente do TenantId).
        /// Exemplo: nome do tenant, domínio customizado, etc.
        /// </summary>
        [StringLength(100)]
        public string? SearchValue { get; }

        /// <summary>
        /// Indica se o tenant pode ter sido removido (soft delete).
        /// Ajuda a distinguir entre tenant inexistente e removido.
        /// </summary>
        public bool MayBeDeleted { get; }

        /// <summary>
        /// Timestamp da última verificação de existência (se aplicável).
        /// Útil para cache invalidation e debugging.
        /// </summary>
        public DateTime? LastCheckedAt { get; }

        /// <summary>
        /// Construtor principal para tenant não encontrado por ID.
        /// </summary>
        /// <param name="tenantId">ID do tenant que não foi encontrado</param>
        /// <param name="userId">ID do usuário que solicitou o tenant</param>
        /// <param name="mayBeDeleted">Se o tenant pode ter sido removido</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        public TenantNotFoundException(
            string tenantId,
            string? userId = null,
            bool mayBeDeleted = false,
            Guid? correlationId = null)
            : base(
                message: BuildMessage(tenantId, "ID", mayBeDeleted),
                tenantId: tenantId,
                userId: userId,
                errorCode: "TENANT_NOT_FOUND",
                category: "NotFound",
                severity: ExceptionSeverity.Medium,
                correlationId: correlationId,
                isRetryable: false,
                shouldLog: true)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("TenantId não pode ser nulo ou vazio", nameof(tenantId));

            RequestedTenantId = tenantId;
            SearchType = "ById";
            SearchValue = tenantId;
            MayBeDeleted = mayBeDeleted;
        }

        /// <summary>
        /// Construtor para busca por diferentes critérios.
        /// </summary>
        /// <param name="searchValue">Valor usado na busca</param>
        /// <param name="searchType">Tipo de busca realizada</param>
        /// <param name="tenantId">ID do tenant contexto (se conhecido)</param>
        /// <param name="userId">ID do usuário que solicitou</param>
        /// <param name="mayBeDeleted">Se o tenant pode ter sido removido</param>
        /// <param name="lastCheckedAt">Última verificação de existência</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        public TenantNotFoundException(
            string searchValue,
            string searchType,
            string? tenantId = null,
            string? userId = null,
            bool mayBeDeleted = false,
            DateTime? lastCheckedAt = null,
            Guid? correlationId = null)
            : base(
                message: BuildMessage(searchValue, searchType, mayBeDeleted),
                tenantId: tenantId,
                userId: userId,
                errorCode: "TENANT_NOT_FOUND",
                category: "NotFound",
                severity: ExceptionSeverity.Medium,
                correlationId: correlationId,
                isRetryable: false,
                shouldLog: true)
        {
            if (string.IsNullOrWhiteSpace(searchValue))
                throw new ArgumentException("SearchValue não pode ser nulo ou vazio", nameof(searchValue));

            if (string.IsNullOrWhiteSpace(searchType))
                throw new ArgumentException("SearchType não pode ser nulo ou vazio", nameof(searchType));

            RequestedTenantId = tenantId ?? searchValue;
            SearchType = searchType;
            SearchValue = searchValue;
            MayBeDeleted = mayBeDeleted;
            LastCheckedAt = lastCheckedAt;

            // Adiciona contexto específico
            this.WithContext(new
            {
                RequestedTenantId,
                SearchType,
                SearchValue,
                MayBeDeleted,
                LastCheckedAt,
                PossibleCauses = GetPossibleCauses(searchType, mayBeDeleted)
            });
        }

        /// <summary>
        /// Constrói a mensagem de erro baseada nos parâmetros.
        /// </summary>
        /// <param name="value">Valor buscado</param>
        /// <param name="type">Tipo de busca</param>
        /// <param name="mayBeDeleted">Se pode ter sido removido</param>
        /// <returns>Mensagem de erro formatada</returns>
        private static string BuildMessage(string value, string type, bool mayBeDeleted)
        {
            var baseMessage = $"Tenant não encontrado. {type}: '{value}'.";

            if (mayBeDeleted)
            {
                baseMessage += " O tenant pode ter sido removido ou desativado.";
            }

            return baseMessage + " Verifique se o identificador está correto e se você tem permissões adequadas.";
        }

        /// <summary>
        /// Retorna possíveis causas baseadas no tipo de busca.
        /// </summary>
        /// <param name="searchType">Tipo de busca</param>
        /// <param name="mayBeDeleted">Se pode ter sido removido</param>
        /// <returns>Lista de possíveis causas</returns>
        private static List<string> GetPossibleCauses(string searchType, bool mayBeDeleted)
        {
            var causes = new List<string>();

            switch (searchType.ToLowerInvariant())
            {
                case "byid":
                    causes.Add("ID do tenant inválido ou inexistente");
                    causes.Add("Erro de digitação no identificador");
                    break;
                case "byname":
                    causes.Add("Nome do tenant incorreto");
                    causes.Add("Tenant renomeado recentemente");
                    break;
                case "bydomain":
                    causes.Add("Domínio customizado não configurado");
                    causes.Add("DNS não resolvido corretamente");
                    break;
                case "bysubdomain":
                    causes.Add("Subdomínio não configurado");
                    causes.Add("URL de acesso incorreta");
                    break;
                default:
                    causes.Add("Critério de busca inválido");
                    break;
            }

            if (mayBeDeleted)
            {
                causes.Add("Tenant removido ou desativado");
                causes.Add("Assinatura expirada ou suspensa");
            }

            causes.Add("Permissões insuficientes para acessar o tenant");
            causes.Add("Cache desatualizado - tente novamente");

            return causes;
        }

        /// <summary>
        /// Cria uma exceção para tenant não encontrado por ID com contexto mínimo.
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <param name="userId">ID do usuário (opcional)</param>
        /// <returns>Nova instância da exceção</returns>
        public static TenantNotFoundException ForId(string tenantId, string? userId = null)
        {
            return new TenantNotFoundException(
                tenantId: tenantId,
                userId: userId,
                mayBeDeleted: false,
                correlationId: null);
        }

        /// <summary>
        /// Cria uma exceção para tenant não encontrado por nome.
        /// </summary>
        /// <param name="tenantName">Nome do tenant</param>
        /// <param name="userId">ID do usuário (opcional)</param>
        /// <returns>Nova instância da exceção</returns>
        public static TenantNotFoundException ForName(string tenantName, string? userId = null)
        {
            return new TenantNotFoundException(
                searchValue: tenantName,
                searchType: "ByName",
                tenantId: null,
                userId: userId,
                mayBeDeleted: false,
                lastCheckedAt: null,
                correlationId: null);
        }

        /// <summary>
        /// Cria uma exceção para tenant não encontrado por domínio.
        /// </summary>
        /// <param name="domain">Domínio do tenant</param>
        /// <param name="userId">ID do usuário (opcional)</param>
        /// <returns>Nova instância da exceção</returns>
        public static TenantNotFoundException ForDomain(string domain, string? userId = null)
        {
            return new TenantNotFoundException(
                searchValue: domain,
                searchType: "ByDomain",
                tenantId: null,
                userId: userId,
                mayBeDeleted: false,
                lastCheckedAt: null,
                correlationId: null);
        }

        /// <summary>
        /// Cria uma exceção para tenant removido ou desativado.
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <param name="userId">ID do usuário (opcional)</param>
        /// <param name="lastCheckedAt">Última verificação</param>
        /// <returns>Nova instância da exceção</returns>
        public static TenantNotFoundException ForDeletedTenant(
            string tenantId,
            string? userId = null,
            DateTime? lastCheckedAt = null)
        {
            return new TenantNotFoundException(
                searchValue: tenantId,
                searchType: "ById",
                tenantId: tenantId,
                userId: userId,
                mayBeDeleted: true,
                lastCheckedAt: lastCheckedAt,
                correlationId: null);
        }

        /// <summary>
        /// Representação string detalhada da exceção para logging.
        /// </summary>
        /// <returns>String formatada com informações específicas</returns>
        public override string ToString()
        {
            var baseString = base.ToString();
            var specificInfo = $"RequestedTenantId: {RequestedTenantId} | SearchType: {SearchType}";

            if (!string.IsNullOrWhiteSpace(SearchValue) && SearchValue != RequestedTenantId)
                specificInfo += $" | SearchValue: {SearchValue}";

            if (MayBeDeleted)
                specificInfo += " | MayBeDeleted: true";

            if (LastCheckedAt.HasValue)
                specificInfo += $" | LastCheckedAt: {LastCheckedAt:yyyy-MM-dd HH:mm:ss} UTC";

            return $"{baseString}\nTenant Details: {specificInfo}";
        }
    }
}
