using AuthTenant.Domain.Entities;
using AuthTenant.Domain.Interfaces;
using AuthTenant.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AuthTenant.Infrastructure.Repositories
{
    /// <summary>
    /// Repositório base genérico que implementa operações CRUD comuns para todas as entidades.
    /// Inclui suporte a soft delete, multi-tenancy, logging e operações assíncronas otimizadas.
    /// </summary>
    /// <typeparam name="T">Tipo da entidade que herda de BaseEntity</typeparam>
    public class BaseRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<BaseRepository<T>> _logger;

        /// <summary>
        /// Inicializa uma nova instância do BaseRepository.
        /// </summary>
        /// <param name="context">Contexto de banco de dados</param>
        /// <param name="logger">Logger para auditoria e debugging</param>
        /// <exception cref="ArgumentNullException">Lançada quando parâmetros obrigatórios são nulos</exception>
        public BaseRepository(ApplicationDbContext context, ILogger<BaseRepository<T>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// Obtém uma entidade por ID, excluindo registros soft deleted.
        /// </summary>
        /// <param name="id">ID da entidade</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Entidade encontrada ou null</returns>
        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _dbSet
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

                if (entity != null)
                {
                    _logger.LogDebug("Entity {EntityType} with ID {EntityId} found", typeof(T).Name, id);
                }
                else
                {
                    _logger.LogDebug("Entity {EntityType} with ID {EntityId} not found", typeof(T).Name, id);
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving {EntityType} with ID {EntityId}", typeof(T).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Obtém todas as entidades não soft deleted.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de entidades</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _dbSet
                    .Where(e => !e.IsDeleted)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved {Count} entities of type {EntityType}", entities.Count, typeof(T).Name);
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all {EntityType} entities", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Obtém entidades paginadas não soft deleted.
        /// </summary>
        /// <param name="pageNumber">Número da página (base 1)</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de entidades paginadas</returns>
        public virtual async Task<IEnumerable<T>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber <= 0)
                throw new ArgumentException("Page number must be greater than zero", nameof(pageNumber));

            if (pageSize <= 0)
                throw new ArgumentException("Page size must be greater than zero", nameof(pageSize));

            try
            {
                var entities = await _dbSet
                    .Where(e => !e.IsDeleted)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved page {PageNumber} with {Count} entities of type {EntityType}",
                    pageNumber, entities.Count, typeof(T).Name);

                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged {EntityType} entities (page {PageNumber}, size {PageSize})",
                    typeof(T).Name, pageNumber, pageSize);
                throw;
            }
        }

        /// <summary>
        /// Busca entidades baseado em um predicado, excluindo soft deleted.
        /// </summary>
        /// <param name="predicate">Expressão de filtro</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de entidades que atendem ao critério</returns>
        public virtual async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            try
            {
                var entities = await _dbSet
                    .Where(predicate)
                    .Where(e => !e.IsDeleted)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Found {Count} entities of type {EntityType} matching predicate",
                    entities.Count, typeof(T).Name);

                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding {EntityType} entities with predicate", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Busca uma única entidade baseado em um predicado.
        /// </summary>
        /// <param name="predicate">Expressão de filtro</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Primeira entidade que atende ao critério ou null</returns>
        public virtual async Task<T?> FindFirstAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            try
            {
                var entity = await _dbSet
                    .Where(predicate)
                    .Where(e => !e.IsDeleted)
                    .FirstOrDefaultAsync(cancellationToken);

                if (entity != null)
                {
                    _logger.LogDebug("Found entity {EntityType} with ID {EntityId} matching predicate",
                        typeof(T).Name, entity.Id);
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding first {EntityType} entity with predicate", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Adiciona uma nova entidade ao repositório.
        /// </summary>
        /// <param name="entity">Entidade a ser adicionada</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Entidade adicionada</returns>
        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                // Configurar timestamps
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                await _dbSet.AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Added new {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding {EntityType} entity", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Adiciona múltiplas entidades em lote.
        /// </summary>
        /// <param name="entities">Lista de entidades a serem adicionadas</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de entidades adicionadas</returns>
        public virtual async Task<IEnumerable<T>> AddRangeAsync(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (!entityList.Any())
                return entityList;

            try
            {
                var now = DateTime.UtcNow;
                foreach (var entity in entityList)
                {
                    entity.CreatedAt = now;
                    entity.UpdatedAt = now;
                }

                await _dbSet.AddRangeAsync(entityList, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Added {Count} {EntityType} entities in batch",
                    entityList.Count, typeof(T).Name);

                return entityList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding {Count} {EntityType} entities in batch",
                    entityList.Count, typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Atualiza uma entidade existente.
        /// </summary>
        /// <param name="entity">Entidade a ser atualizada</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                entity.UpdatedAt = DateTime.UtcNow;
                _dbSet.Update(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
                throw;
            }
        }

        /// <summary>
        /// Realiza soft delete de uma entidade.
        /// </summary>
        /// <param name="entity">Entidade a ser excluída</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                _dbSet.Update(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Soft deleted {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
                throw;
            }
        }

        /// <summary>
        /// Realiza soft delete por ID.
        /// </summary>
        /// <param name="id">ID da entidade a ser excluída</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se a entidade foi encontrada e excluída</returns>
        public virtual async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetByIdAsync(id, cancellationToken);
                if (entity == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent {EntityType} with ID {EntityId}",
                        typeof(T).Name, id);
                    return false;
                }

                await DeleteAsync(entity, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {EntityType} by ID {EntityId}", typeof(T).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Verifica se uma entidade existe por ID.
        /// </summary>
        /// <param name="id">ID da entidade</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se a entidade existe e não está soft deleted</returns>
        public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var exists = await _dbSet.AnyAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
                _logger.LogDebug("Existence check for {EntityType} with ID {EntityId}: {Exists}",
                    typeof(T).Name, id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of {EntityType} with ID {EntityId}",
                    typeof(T).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Conta o número de entidades, opcionalmente com um predicado.
        /// </summary>
        /// <param name="predicate">Filtro opcional</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Número de entidades</returns>
        public virtual async Task<int> CountAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet.Where(e => !e.IsDeleted);
                if (predicate != null)
                    query = query.Where(predicate);

                var count = await query.CountAsync(cancellationToken);
                _logger.LogDebug("Count for {EntityType}: {Count}", typeof(T).Name, count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting {EntityType} entities", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Conta o número total de entidades incluindo soft deleted.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Número total de entidades</returns>
        public virtual async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var count = await _dbSet.CountAsync(cancellationToken);
                _logger.LogDebug("Total count (including deleted) for {EntityType}: {Count}", typeof(T).Name, count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting all {EntityType} entities", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Obtém o query base aplicando filtros de soft delete e tenant (se aplicável).
        /// </summary>
        /// <returns>IQueryable configurado</returns>
        protected virtual IQueryable<T> GetBaseQuery()
        {
            return _dbSet.Where(e => !e.IsDeleted);
        }

        /// <summary>
        /// Executa SaveChanges com tratamento de erro e logging.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Número de entidades afetadas</returns>
        protected virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("SaveChanges completed. {AffectedRows} rows affected", result);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict during SaveChanges for {EntityType}", typeof(T).Name);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error during SaveChanges for {EntityType}", typeof(T).Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during SaveChanges for {EntityType}", typeof(T).Name);
                throw;
            }
        }
    }
}
