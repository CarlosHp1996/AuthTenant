using AuthTenant.Domain.Entities;
using AuthTenant.Domain.Interfaces;
using AuthTenant.Infrastructure.Data.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Infrastructure.Repositories
{
    /// <summary>
    /// Repositório específico para operações de Product.
    /// Herda funcionalidades base e adiciona operações específicas como busca, filtros avançados
    /// e operações de negócio relacionadas a produtos.
    /// </summary>
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        private readonly ILogger<ProductRepository> _productLogger;

        /// <summary>
        /// Inicializa uma nova instância do ProductRepository.
        /// </summary>
        /// <param name="context">Contexto de banco de dados</param>
        /// <param name="baseLogger">Logger do repositório base</param>
        /// <param name="productLogger">Logger específico para produtos</param>
        /// <exception cref="ArgumentNullException">Lançada quando parâmetros obrigatórios são nulos</exception>
        public ProductRepository(
            ApplicationDbContext context,
            ILogger<BaseRepository<Product>> baseLogger,
            ILogger<ProductRepository> productLogger)
            : base(context, baseLogger)
        {
            _productLogger = productLogger ?? throw new ArgumentNullException(nameof(productLogger));
        }

        /// <summary>
        /// Obtém todos os produtos ativos (IsActive = true e não soft deleted).
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de produtos ativos</returns>
        public async Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _dbSet
                    .Where(p => p.IsActive && !p.IsDeleted)
                    .OrderBy(p => p.Name)
                    .ToListAsync(cancellationToken);

                _productLogger.LogDebug("Retrieved {Count} active products", products.Count);
                return products;
            }
            catch (Exception ex)
            {
                _productLogger.LogError(ex, "Error retrieving active products");
                throw;
            }
        }

        /// <summary>
        /// Busca produtos por termo de pesquisa nos campos: Nome, Descrição e SKU.
        /// Implementa busca case-insensitive e com trim automático.
        /// </summary>
        /// <param name="searchTerm">Termo de busca</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de produtos que correspondem à busca</returns>
        public async Task<IEnumerable<Product>> SearchProductsAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _productLogger.LogDebug("Empty search term provided, returning empty result");
                return Enumerable.Empty<Product>();
            }

            try
            {
                // Sanitizar termo de busca
                var sanitizedSearchTerm = searchTerm.Trim();

                var products = await _dbSet
                    .Where(p => !p.IsDeleted &&
                              (EF.Functions.ILike(p.Name, $"%{sanitizedSearchTerm}%") ||
                               (p.Description != null && EF.Functions.ILike(p.Description, $"%{sanitizedSearchTerm}%")) ||
                               (p.SKU != null && EF.Functions.ILike(p.SKU, $"%{sanitizedSearchTerm}%"))))
                    .OrderByDescending(p => p.IsActive)
                    .ThenBy(p => p.Name)
                    .ToListAsync(cancellationToken);

                _productLogger.LogInformation("Search for '{SearchTerm}' returned {Count} products",
                    sanitizedSearchTerm, products.Count);

                return products;
            }
            catch (Exception ex)
            {
                _productLogger.LogError(ex, "Error searching products with term '{SearchTerm}'", searchTerm);
                throw;
            }
        }

        /// <summary>
        /// Obtém produtos por categoria com paginação.
        /// </summary>
        /// <param name="category">Categoria dos produtos</param>
        /// <param name="pageNumber">Número da página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <param name="activeOnly">Se deve retornar apenas produtos ativos</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista paginada de produtos</returns>
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(
            string category,
            int pageNumber = 1,
            int pageSize = 20,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be null or empty", nameof(category));

            if (pageNumber <= 0)
                throw new ArgumentException("Page number must be greater than zero", nameof(pageNumber));

            if (pageSize <= 0 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            try
            {
                var query = _dbSet.Where(p => !p.IsDeleted);

                if (activeOnly)
                {
                    query = query.Where(p => p.IsActive);
                }

                // Assumindo que Category é uma propriedade string ou navigation property
                query = query.Where(p => p.Category == category);

                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                _productLogger.LogDebug("Retrieved {Count} products for category '{Category}' (page {PageNumber})",
                    products.Count, category, pageNumber);

                return products;
            }
            catch (Exception ex)
            {
                _productLogger.LogError(ex, "Error retrieving products by category '{Category}'", category);
                throw;
            }
        }

        /// <summary>
        /// Obtém produtos com preço em uma faixa específica.
        /// </summary>
        /// <param name="minPrice">Preço mínimo</param>
        /// <param name="maxPrice">Preço máximo</param>
        /// <param name="activeOnly">Se deve retornar apenas produtos ativos</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de produtos na faixa de preço</returns>
        public async Task<IEnumerable<Product>> GetProductsByPriceRangeAsync(
            decimal minPrice,
            decimal maxPrice,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            if (minPrice < 0)
                throw new ArgumentException("Minimum price cannot be negative", nameof(minPrice));

            if (maxPrice < minPrice)
                throw new ArgumentException("Maximum price must be greater than or equal to minimum price", nameof(maxPrice));

            try
            {
                var query = _dbSet.Where(p => !p.IsDeleted && p.Price >= minPrice && p.Price <= maxPrice);

                if (activeOnly)
                {
                    query = query.Where(p => p.IsActive);
                }

                var products = await query
                    .OrderBy(p => p.Price)
                    .ThenBy(p => p.Name)
                    .ToListAsync(cancellationToken);

                _productLogger.LogDebug("Retrieved {Count} products in price range {MinPrice}-{MaxPrice}",
                    products.Count, minPrice, maxPrice);

                return products;
            }
            catch (Exception ex)
            {
                _productLogger.LogError(ex, "Error retrieving products by price range {MinPrice}-{MaxPrice}",
                    minPrice, maxPrice);
                throw;
            }
        }

        /// <summary>
        /// Obtém produto por SKU (identificador único).
        /// </summary>
        /// <param name="sku">SKU do produto</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Produto com o SKU especificado ou null</returns>
        public async Task<Product?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            try
            {
                var product = await _dbSet
                    .FirstOrDefaultAsync(p => p.SKU == sku.Trim() && !p.IsDeleted, cancellationToken);

                if (product != null)
                {
                    _productLogger.LogDebug("Product found by SKU '{SKU}': {ProductId}", sku, product.Id);
                }
                else
                {
                    _productLogger.LogDebug("No product found with SKU '{SKU}'", sku);
                }

                return product;
            }
            catch (Exception ex)
            {
                _productLogger.LogError(ex, "Error retrieving product by SKU '{SKU}'", sku);
                throw;
            }
        }

        /// <summary>
        /// Verifica se um SKU já existe no sistema.
        /// </summary>
        /// <param name="sku">SKU a ser verificado</param>
        /// <param name="excludeProductId">ID do produto a ser excluído da verificação (para updates)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se o SKU já existe</returns>
        public async Task<bool> SkuExistsAsync(
            string sku,
            Guid? excludeProductId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;

            try
            {
                var query = _dbSet.Where(p => p.SKU == sku.Trim() && !p.IsDeleted);

                if (excludeProductId.HasValue)
                {
                    query = query.Where(p => p.Id != excludeProductId.Value);
                }

                var exists = await query.AnyAsync(cancellationToken);

                _productLogger.LogDebug("SKU '{SKU}' existence check: {Exists}", sku, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _productLogger.LogError(ex, "Error checking SKU existence for '{SKU}'", sku);
                throw;
            }
        }

        /// <summary>
        /// Atualiza o status ativo/inativo de múltiplos produtos em lote.
        /// </summary>
        /// <param name="productIds">IDs dos produtos a serem atualizados</param>
        /// <param name="isActive">Novo status ativo</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Número de produtos atualizados</returns>
        public async Task<int> UpdateActiveStatusAsync(
            IEnumerable<Guid> productIds,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            if (productIds == null || !productIds.Any())
                return 0;

            try
            {
                var ids = productIds.ToList();
                var updatedCount = await _dbSet
                    .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(x => x.IsActive, isActive)
                        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                _productLogger.LogInformation("Updated active status for {Count} products to {IsActive}",
                    updatedCount, isActive);

                return updatedCount;
            }
            catch (Exception ex)
            {
                _productLogger.LogError(ex, "Error updating active status for products");
                throw;
            }
        }

        /// <summary>
        /// Obtém estatísticas básicas dos produtos.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Objeto com estatísticas dos produtos</returns>
        public async Task<ProductStatistics> GetProductStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var statistics = await _dbSet
                    .Where(p => !p.IsDeleted)
                    .GroupBy(p => 1)
                    .Select(g => new ProductStatistics
                    {
                        TotalProducts = g.Count(),
                        ActiveProducts = g.Count(p => p.IsActive),
                        InactiveProducts = g.Count(p => !p.IsActive),
                        AveragePrice = g.Average(p => p.Price),
                        MinPrice = g.Min(p => p.Price),
                        MaxPrice = g.Max(p => p.Price)
                    })
                    .FirstOrDefaultAsync(cancellationToken) ?? new ProductStatistics();

                _productLogger.LogDebug("Retrieved product statistics: {TotalProducts} total, {ActiveProducts} active",
                    statistics.TotalProducts, statistics.ActiveProducts);

                return statistics;
            }
            catch (Exception ex)
            {
                _productLogger.LogError(ex, "Error retrieving product statistics");
                throw;
            }
        }

        /// <summary>
        /// Busca avançada com múltiplos critérios.
        /// </summary>
        /// <param name="filter">Objeto com critérios de filtro</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de produtos que atendem aos critérios</returns>
        public async Task<IEnumerable<Product>> AdvancedSearchAsync(
            ProductSearchFilter filter,
            CancellationToken cancellationToken = default)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            try
            {
                var query = GetBaseQuery();

                // Aplicar filtros condicionalmente
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.Trim();
                    query = query.Where(p =>
                        EF.Functions.ILike(p.Name, $"%{searchTerm}%") ||
                        (p.Description != null && EF.Functions.ILike(p.Description, $"%{searchTerm}%")) ||
                        (p.SKU != null && EF.Functions.ILike(p.SKU, $"%{searchTerm}%")));
                }

                if (!string.IsNullOrWhiteSpace(filter.Category))
                {
                    query = query.Where(p => p.Category == filter.Category);
                }

                if (filter.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= filter.MinPrice.Value);
                }

                if (filter.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= filter.MaxPrice.Value);
                }

                if (filter.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == filter.IsActive.Value);
                }

                if (filter.CreatedAfter.HasValue)
                {
                    query = query.Where(p => p.CreatedAt >= filter.CreatedAfter.Value);
                }

                if (filter.CreatedBefore.HasValue)
                {
                    query = query.Where(p => p.CreatedAt <= filter.CreatedBefore.Value);
                }

                // Aplicar ordenação
                query = filter.SortBy?.ToLower() switch
                {
                    "name" => filter.SortDescending
                        ? query.OrderByDescending(p => p.Name)
                        : query.OrderBy(p => p.Name),
                    "price" => filter.SortDescending
                        ? query.OrderByDescending(p => p.Price)
                        : query.OrderBy(p => p.Price),
                    "created" => filter.SortDescending
                        ? query.OrderByDescending(p => p.CreatedAt)
                        : query.OrderBy(p => p.CreatedAt),
                    _ => query.OrderBy(p => p.Name)
                };

                // Aplicar paginação
                if (filter.PageNumber > 0 && filter.PageSize > 0)
                {
                    query = query
                        .Skip((filter.PageNumber - 1) * filter.PageSize)
                        .Take(filter.PageSize);
                }

                var products = await query.ToListAsync(cancellationToken);

                _productLogger.LogInformation("Advanced search returned {Count} products with filter criteria",
                    products.Count);

                return products;
            }
            catch (Exception ex)
            {
                _productLogger.LogError(ex, "Error performing advanced product search");
                throw;
            }
        }

        /// <summary>
        /// Obtém o query base para produtos aplicando filtros padrão.
        /// </summary>
        /// <returns>Query configurado para produtos</returns>
        protected override IQueryable<Product> GetBaseQuery()
        {
            return _dbSet.Where(p => !p.IsDeleted);
        }
    }

    /// <summary>
    /// Classe para estatísticas de produtos.
    /// </summary>
    public class ProductStatistics
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
    }

    /// <summary>
    /// Classe para filtros de busca avançada de produtos.
    /// </summary>
    public class ProductSearchFilter
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public string? SortBy { get; set; } = "name";
        public bool SortDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
