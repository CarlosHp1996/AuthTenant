using AuthTenant.Domain.Entities;
using System.Linq.Expressions;

namespace AuthTenant.Domain.Interfaces
{
    /// <summary>
    /// Interface para repositório base - compatível com BaseRepository existente.
    /// Define operações básicas CRUD para entidades que herdam de BaseEntity.
    /// </summary>
    /// <typeparam name="T">Tipo da entidade que herda de BaseEntity</typeparam>
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    }
}