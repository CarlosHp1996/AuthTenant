using System.ComponentModel.DataAnnotations;

namespace AuthTenant.Domain.Interfaces
{
    /// <summary>
    /// Interface que define o contrato para entidades multi-tenant.
    /// Garante isolamento de dados por tenant em sistemas SaaS.
    /// Implementa padrões de segurança e compliance para multi-tenancy.
    /// </summary>
    public interface ITenantEntity
    {
        /// <summary>
        /// Identificador único do tenant proprietário desta entidade.
        /// Obrigatório para garantir isolamento de dados entre organizações.
        /// Usado em filtros automáticos de segurança e queries.
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        string TenantId { get; set; }

        /// <summary>
        /// Indica se a entidade pertence ao tenant especificado.
        /// Método de conveniência para validações de segurança.
        /// </summary>
        /// <param name="tenantId">ID do tenant a verificar</param>
        /// <returns>True se a entidade pertence ao tenant</returns>
        bool BelongsToTenant(string tenantId);

        /// <summary>
        /// Valida se o TenantId está em formato válido.
        /// Verifica se não é null, vazio ou apenas espaços.
        /// </summary>
        /// <returns>True se o TenantId é válido</returns>
        bool HasValidTenantId();

        /// <summary>
        /// Define o tenant da entidade com validação.
        /// Garante que o tenant seja válido antes da atribuição.
        /// </summary>
        /// <param name="tenantId">ID do tenant a definir</param>
        /// <exception cref="ArgumentException">Quando o tenantId é inválido</exception>
        void SetTenantId(string tenantId);

        /// <summary>
        /// Obtém informações de diagnóstico sobre o tenant da entidade.
        /// Útil para debugging e auditoria de multi-tenancy.
        /// </summary>
        /// <returns>String com informações do tenant</returns>
        string GetTenantInfo();
    }

    /// <summary>
    /// Interface estendida para entidades tenant-aware com metadados adicionais.
    /// Fornece informações extras sobre o contexto de tenant.
    /// </summary>
    public interface ITenantAwareEntity : ITenantEntity
    {
        /// <summary>
        /// Nome de exibição do tenant para interfaces de usuário.
        /// Pode ser null se não foi carregado ou não é necessário.
        /// </summary>
        string? TenantName { get; set; }

        /// <summary>
        /// Timestamp de quando a entidade foi associada ao tenant atual.
        /// Útil para auditoria de migração entre tenants.
        /// </summary>
        DateTime? TenantAssignedAt { get; set; }

        /// <summary>
        /// ID do tenant anterior, se houve migração.
        /// Usado para auditoria e rollback de migrações.
        /// </summary>
        string? PreviousTenantId { get; set; }

        /// <summary>
        /// Indica se a entidade foi migrada entre tenants.
        /// Baseado na existência de PreviousTenantId.
        /// </summary>
        bool WasMigratedBetweenTenants { get; }

        /// <summary>
        /// Tempo decorrido desde a atribuição ao tenant atual.
        /// Null se TenantAssignedAt não foi definido.
        /// </summary>
        TimeSpan? TimeSinceTenantAssignment { get; }

        /// <summary>
        /// Migra a entidade para um novo tenant com auditoria.
        /// Preserva informações do tenant anterior para rastreabilidade.
        /// </summary>
        /// <param name="newTenantId">ID do novo tenant</param>
        /// <param name="newTenantName">Nome do novo tenant (opcional)</param>
        /// <exception cref="ArgumentException">Quando newTenantId é inválido</exception>
        void MigrateToTenant(string newTenantId, string? newTenantName = null);

        /// <summary>
        /// Obtém histórico de tenant da entidade.
        /// Inclui tenant atual e anterior se houver.
        /// </summary>
        /// <returns>Informações de histórico de tenant</returns>
        ITenantHistory GetTenantHistory();
    }

    /// <summary>
    /// Interface para histórico de tenant de uma entidade.
    /// Fornece informações sobre mudanças de tenant ao longo do tempo.
    /// </summary>
    public interface ITenantHistory
    {
        /// <summary>
        /// ID do tenant atual.
        /// </summary>
        string CurrentTenantId { get; set; }

        /// <summary>
        /// Nome do tenant atual.
        /// </summary>
        string? CurrentTenantName { get; set; }

        /// <summary>
        /// ID do tenant anterior.
        /// </summary>
        string? PreviousTenantId { get; set; }

        /// <summary>
        /// Quando foi atribuído ao tenant atual.
        /// </summary>
        DateTime? TenantAssignedAt { get; set; }

        /// <summary>
        /// Se houve migração entre tenants.
        /// </summary>
        bool WasMigrated { get; set; }

        /// <summary>
        /// Tempo desde a atribuição ao tenant atual.
        /// </summary>
        TimeSpan? TimeSinceAssignment { get; set; }
    }

    /// <summary>
    /// Implementação concreta de ITenantHistory.
    /// </summary>
    public class TenantHistory : ITenantHistory
    {
        public string CurrentTenantId { get; set; } = string.Empty;
        public string? CurrentTenantName { get; set; }
        public string? PreviousTenantId { get; set; }
        public DateTime? TenantAssignedAt { get; set; }
        public bool WasMigrated { get; set; }
        public TimeSpan? TimeSinceAssignment { get; set; }

        public override string ToString()
        {
            var info = $"Current: {CurrentTenantId}";

            if (!string.IsNullOrWhiteSpace(CurrentTenantName))
                info += $" ({CurrentTenantName})";

            if (WasMigrated && !string.IsNullOrWhiteSpace(PreviousTenantId))
                info += $" | Previous: {PreviousTenantId}";

            if (TenantAssignedAt.HasValue)
                info += $" | Assigned: {TenantAssignedAt.Value:yyyy-MM-dd HH:mm:ss} UTC";

            return info;
        }
    }

    /// <summary>
    /// Interface para validação de multi-tenancy em entidades.
    /// Fornece métodos para verificar compliance e segurança.
    /// </summary>
    public interface ITenantValidator
    {
        /// <summary>
        /// Valida se uma entidade está em conformidade com regras de multi-tenancy.
        /// </summary>
        /// <param name="entity">Entidade a validar</param>
        /// <returns>Resultado da validação</returns>
        ITenantValidationResult ValidateEntity(ITenantEntity entity);

        /// <summary>
        /// Valida se uma operação é permitida no contexto do tenant.
        /// </summary>
        /// <param name="entity">Entidade envolvida</param>
        /// <param name="operation">Tipo de operação</param>
        /// <param name="userTenantId">Tenant do usuário executando a operação</param>
        /// <returns>Resultado da validação</returns>
        ITenantValidationResult ValidateOperation(
            ITenantEntity entity,
            TenantOperation operation,
            string userTenantId);
    }

    /// <summary>
    /// Interface para resultado de validação de tenant.
    /// </summary>
    public interface ITenantValidationResult
    {
        /// <summary>
        /// Se a validação passou.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Lista de erros encontrados.
        /// </summary>
        IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Lista de warnings.
        /// </summary>
        IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Nível de severidade do resultado.
        /// </summary>
        TenantValidationSeverity Severity { get; }
    }

    /// <summary>
    /// Tipos de operação em contexto de tenant.
    /// </summary>
    public enum TenantOperation
    {
        /// <summary>
        /// Operação de leitura.
        /// </summary>
        Read,

        /// <summary>
        /// Operação de criação.
        /// </summary>
        Create,

        /// <summary>
        /// Operação de atualização.
        /// </summary>
        Update,

        /// <summary>
        /// Operação de exclusão.
        /// </summary>
        Delete,

        /// <summary>
        /// Migração entre tenants.
        /// </summary>
        Migrate
    }

    /// <summary>
    /// Níveis de severidade para validação de tenant.
    /// </summary>
    public enum TenantValidationSeverity
    {
        /// <summary>
        /// Informacional - sem problemas.
        /// </summary>
        Info,

        /// <summary>
        /// Warning - potencial problema.
        /// </summary>
        Warning,

        /// <summary>
        /// Erro - operação deve ser bloqueada.
        /// </summary>
        Error,

        /// <summary>
        /// Crítico - violação grave de segurança.
        /// </summary>
        Critical
    }
}
