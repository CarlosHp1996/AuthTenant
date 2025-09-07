namespace AuthTenant.Domain.Interfaces
{
    /// <summary>
    /// Interface para serviço de gerenciamento de contexto multi-tenant.
    /// Responsável por manter e fornecer informações do tenant atual.
    /// Focado apenas na resolução de tenant ID para evitar dependências circulares.
    /// </summary>
    public interface ICurrentTenantService
    {
        /// <summary>
        /// ID do tenant atual baseado no contexto da requisição
        /// </summary>
        string? TenantId { get; }

        /// <summary>
        /// Define o ID do tenant para o contexto atual
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        void SetTenant(string? tenantId);

        /// <summary>
        /// Verifica se existe um tenant válido no contexto atual
        /// </summary>
        bool HasValidTenant { get; }

        /// <summary>
        /// Verifica se o tenant atual é o tenant padrão
        /// </summary>
        bool IsDefaultTenant { get; }
    }
}