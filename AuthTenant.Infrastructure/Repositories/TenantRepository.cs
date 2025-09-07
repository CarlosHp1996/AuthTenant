using AuthTenant.Domain.Entities;
using AuthTenant.Domain.Interfaces;
using AuthTenant.Infrastructure.Data.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Infrastructure.Repositories
{
    /// <summary>
    /// Repositório específico para operações de Tenant.
    /// Implementa cache em memória, operações de validação e gerenciamento completo de tenants.
    /// </summary>
    public class TenantRepository : ITenantRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TenantRepository> _logger;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Inicializa uma nova instância do TenantRepository.
        /// </summary>
        /// <param name="context">Contexto de banco de dados</param>
        /// <param name="cache">Cache em memória para otimização</param>
        /// <param name="logger">Logger para auditoria e debugging</param>
        /// <exception cref="ArgumentNullException">Lançada quando parâmetros obrigatórios são nulos</exception>
        public TenantRepository(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<TenantRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém um tenant por ID com cache otimizado.
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Tenant encontrado ou null</returns>
        public async Task<Tenant?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                _logger.LogDebug("Empty tenant ID provided");
                return null;
            }

            var cacheKey = GetCacheKey("id", tenantId);

            try
            {
                // Verificar cache primeiro
                if (_cache.TryGetValue(cacheKey, out Tenant? cachedTenant))
                {
                    _logger.LogDebug("Tenant {TenantId} found in cache", tenantId);
                    return cachedTenant;
                }

                // Buscar no banco de dados
                var tenant = await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

                // Armazenar em cache se encontrado
                if (tenant != null)
                {
                    _cache.Set(cacheKey, tenant, CacheDuration);
                    _logger.LogDebug("Tenant {TenantId} loaded from database and cached", tenantId);
                }
                else
                {
                    _logger.LogDebug("Tenant {TenantId} not found", tenantId);
                }

                return tenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Obtém um tenant por nome com cache otimizado.
        /// </summary>
        /// <param name="name">Nome do tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Tenant encontrado ou null</returns>
        public async Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogDebug("Empty tenant name provided");
                return null;
            }

            var cacheKey = GetCacheKey("name", name.ToLowerInvariant());

            try
            {
                // Verificar cache primeiro
                if (_cache.TryGetValue(cacheKey, out Tenant? cachedTenant))
                {
                    _logger.LogDebug("Tenant with name '{TenantName}' found in cache", name);
                    return cachedTenant;
                }

                // Buscar no banco de dados (case-insensitive)
                var tenant = await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => EF.Functions.ILike(t.Name, name), cancellationToken);

                // Armazenar em cache se encontrado
                if (tenant != null)
                {
                    _cache.Set(cacheKey, tenant, CacheDuration);
                    _cache.Set(GetCacheKey("id", tenant.Id), tenant, CacheDuration); // Cache duplo por ID também
                    _logger.LogDebug("Tenant '{TenantName}' loaded from database and cached", name);
                }
                else
                {
                    _logger.LogDebug("Tenant with name '{TenantName}' not found", name);
                }

                return tenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant by name '{TenantName}'", name);
                throw;
            }
        }

        /// <summary>
        /// Obtém todos os tenants ativos.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de tenants ativos</returns>
        public async Task<IEnumerable<Tenant>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        {
            const string cacheKey = "all_active_tenants";

            try
            {
                // Verificar cache primeiro
                if (_cache.TryGetValue(cacheKey, out IEnumerable<Tenant>? cachedTenants))
                {
                    _logger.LogDebug("Active tenants found in cache");
                    return cachedTenants!;
                }

                // Buscar no banco de dados
                var tenants = await _context.Tenants
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync(cancellationToken);

                // Armazenar em cache
                _cache.Set(cacheKey, tenants, TimeSpan.FromMinutes(15)); // Cache mais curto para listas

                _logger.LogInformation("Retrieved {Count} active tenants", tenants.Count);
                return tenants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active tenants");
                throw;
            }
        }

        /// <summary>
        /// Obtém todos os tenants com paginação.
        /// </summary>
        /// <param name="pageNumber">Número da página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <param name="activeOnly">Se deve retornar apenas tenants ativos</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista paginada de tenants</returns>
        public async Task<IEnumerable<Tenant>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 20,
            bool activeOnly = false,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber <= 0)
                throw new ArgumentException("Page number must be greater than zero", nameof(pageNumber));

            if (pageSize <= 0 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            try
            {
                var query = _context.Tenants.AsNoTracking();

                if (activeOnly)
                {
                    query = query.Where(t => t.IsActive);
                }

                var tenants = await query
                    .OrderBy(t => t.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved page {PageNumber} with {Count} tenants (activeOnly: {ActiveOnly})",
                    pageNumber, tenants.Count, activeOnly);

                return tenants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged tenants (page {PageNumber}, size {PageSize})",
                    pageNumber, pageSize);
                throw;
            }
        }

        /// <summary>
        /// Cria um novo tenant.
        /// </summary>
        /// <param name="tenant">Tenant a ser criado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Tenant criado</returns>
        public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            // Validar se ID ou nome já existem
            await ValidateUniqueConstraintsAsync(tenant, cancellationToken);

            try
            {
                // Configurar timestamps
                tenant.CreatedAt = DateTime.UtcNow;
                tenant.UpdatedAt = DateTime.UtcNow;

                await _context.Tenants.AddAsync(tenant, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // Invalidar caches relacionados
                InvalidateRelatedCaches(tenant);

                _logger.LogInformation("Created new tenant {TenantId} with name '{TenantName}'",
                    tenant.Id, tenant.Name);

                return tenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tenant {TenantId}", tenant.Id);
                throw;
            }
        }

        /// <summary>
        /// Atualiza um tenant existente.
        /// </summary>
        /// <param name="tenant">Tenant a ser atualizado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Tenant atualizado</returns>
        public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            // Verificar se tenant existe
            var existingTenant = await GetByIdAsync(tenant.Id, cancellationToken);
            if (existingTenant == null)
            {
                throw new InvalidOperationException($"Tenant {tenant.Id} not found");
            }

            try
            {
                tenant.UpdatedAt = DateTime.UtcNow;
                _context.Tenants.Update(tenant);
                await _context.SaveChangesAsync(cancellationToken);

                // Invalidar caches relacionados
                InvalidateRelatedCaches(tenant);

                _logger.LogInformation("Updated tenant {TenantId}", tenant.Id);
                return tenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant {TenantId}", tenant.Id);
                throw;
            }
        }

        /// <summary>
        /// Ativa ou desativa um tenant.
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <param name="isActive">Status ativo</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se foi atualizado com sucesso</returns>
        public async Task<bool> SetActiveStatusAsync(
            string tenantId,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return false;

            try
            {
                var rowsAffected = await _context.Tenants
                    .Where(t => t.Id == tenantId)
                    .ExecuteUpdateAsync(t => t
                        .SetProperty(x => x.IsActive, isActive)
                        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                if (rowsAffected > 0)
                {
                    // Invalidar caches
                    _cache.Remove(GetCacheKey("id", tenantId));
                    _cache.Remove("all_active_tenants");

                    _logger.LogInformation("Updated active status for tenant {TenantId} to {IsActive}",
                        tenantId, isActive);
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating active status for tenant {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Verifica se um tenant existe.
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se o tenant existe</returns>
        public async Task<bool> ExistsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return false;

            try
            {
                // Verificar cache primeiro
                var cacheKey = GetCacheKey("id", tenantId);
                if (_cache.TryGetValue(cacheKey, out Tenant? cachedTenant))
                {
                    var exists = cachedTenant != null;
                    _logger.LogDebug("Tenant {TenantId} existence check from cache: {Exists}", tenantId, exists);
                    return exists;
                }

                // Verificar no banco de dados
                var tenantExists = await _context.Tenants
                    .AnyAsync(t => t.Id == tenantId, cancellationToken);

                _logger.LogDebug("Tenant {TenantId} existence check from database: {Exists}", tenantId, tenantExists);
                return tenantExists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of tenant {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Verifica se um nome de tenant já existe.
        /// </summary>
        /// <param name="name">Nome do tenant</param>
        /// <param name="excludeTenantId">ID do tenant a ser excluído da verificação</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se o nome já existe</returns>
        public async Task<bool> NameExistsAsync(
            string name,
            string? excludeTenantId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            try
            {
                var query = _context.Tenants.Where(t => EF.Functions.ILike(t.Name, name));

                if (!string.IsNullOrWhiteSpace(excludeTenantId))
                {
                    query = query.Where(t => t.Id != excludeTenantId);
                }

                var exists = await query.AnyAsync(cancellationToken);
                _logger.LogDebug("Tenant name '{TenantName}' existence check: {Exists}", name, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of tenant name '{TenantName}'", name);
                throw;
            }
        }

        /// <summary>
        /// Obtém estatísticas de tenants.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Estatísticas dos tenants</returns>
        public async Task<TenantStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var statistics = await _context.Tenants
                    .GroupBy(t => 1)
                    .Select(g => new TenantStatistics
                    {
                        TotalTenants = g.Count(),
                        ActiveTenants = g.Count(t => t.IsActive),
                        InactiveTenants = g.Count(t => !t.IsActive)
                    })
                    .FirstOrDefaultAsync(cancellationToken) ?? new TenantStatistics();

                _logger.LogDebug("Retrieved tenant statistics: {Total} total, {Active} active",
                    statistics.TotalTenants, statistics.ActiveTenants);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant statistics");
                throw;
            }
        }

        /// <summary>
        /// Exclui um tenant permanentemente (hard delete).
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se foi excluído com sucesso</returns>
        public async Task<bool> DeleteAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return false;

            try
            {
                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

                if (tenant == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent tenant {TenantId}", tenantId);
                    return false;
                }

                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync(cancellationToken);

                // Invalidar caches
                InvalidateRelatedCaches(tenant);

                _logger.LogWarning("Permanently deleted tenant {TenantId}", tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Valida restrições de unicidade antes de criar/atualizar.
        /// </summary>
        /// <param name="tenant">Tenant a ser validado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        private async Task ValidateUniqueConstraintsAsync(Tenant tenant, CancellationToken cancellationToken)
        {
            // Verificar ID único
            if (await ExistsAsync(tenant.Id, cancellationToken))
            {
                throw new InvalidOperationException($"Tenant with ID '{tenant.Id}' already exists");
            }

            // Verificar nome único
            if (await NameExistsAsync(tenant.Name, cancellationToken: cancellationToken))
            {
                throw new InvalidOperationException($"Tenant with name '{tenant.Name}' already exists");
            }
        }

        /// <summary>
        /// Invalida caches relacionados ao tenant.
        /// </summary>
        /// <param name="tenant">Tenant para invalidar caches</param>
        private void InvalidateRelatedCaches(Tenant tenant)
        {
            _cache.Remove(GetCacheKey("id", tenant.Id));
            _cache.Remove(GetCacheKey("name", tenant.Name.ToLowerInvariant()));
            _cache.Remove("all_active_tenants");
        }

        /// <summary>
        /// Gera chave de cache consistente.
        /// </summary>
        /// <param name="type">Tipo de chave (id, name)</param>
        /// <param name="value">Valor da chave</param>
        /// <returns>Chave de cache formatada</returns>
        private static string GetCacheKey(string type, string value)
        {
            return $"tenant_{type}_{value}";
        }
    }

    /// <summary>
    /// Classe para estatísticas de tenants.
    /// </summary>
    public class TenantStatistics
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int InactiveTenants { get; set; }
    }
}
