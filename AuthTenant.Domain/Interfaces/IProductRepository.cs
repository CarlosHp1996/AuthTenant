using AuthTenant.Domain.Entities;

namespace AuthTenant.Domain.Interfaces
{
    /// <summary>
    /// Interface para repositório de produtos - herda do repositório base.
    /// Define operações específicas para produtos.
    /// </summary>
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default);
    }
}
