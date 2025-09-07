using AuthTenant.Domain.Entities;

namespace AuthTenant.Domain.Interfaces
{
    /// <summary>
    /// Interface para repositório de Tenant.
    /// Define operações específicas para gerenciamento de tenants.
    /// </summary>
    public interface ITenantRepository
    {
        /// <summary>
        /// Obtém um tenant por ID
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Tenant encontrado ou null</returns>
        Task<Tenant?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém um tenant por nome
        /// </summary>
        /// <param name="name">Nome do tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Tenant encontrado ou null</returns>
        Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém todos os tenants ativos
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de tenants ativos</returns>
        Task<IEnumerable<Tenant>> GetAllActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cria um novo tenant
        /// </summary>
        /// <param name="tenant">Dados do tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Tenant criado</returns>
        Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atualiza um tenant existente
        /// </summary>
        /// <param name="tenant">Dados do tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Tenant atualizado</returns>
        Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se um tenant existe
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se existir, false caso contrário</returns>
        Task<bool> ExistsAsync(string tenantId, CancellationToken cancellationToken = default);
    }
}