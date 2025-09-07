using System.Reflection;
using System.Security.Claims;

using AuthTenant.Domain.Entities;
using AuthTenant.Domain.Interfaces;
using AuthTenant.Infrastructure.Data.Configurations;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Infrastructure.Data.Context;

/// <summary>
/// Main Entity Framework DbContext for the AuthTenant application.
/// Extends IdentityDbContext to provide ASP.NET Core Identity integration
/// with multi-tenant support and automatic auditing capabilities.
/// </summary>
/// <remarks>
/// This DbContext implements several cross-cutting concerns:
/// - Multi-tenant data isolation through query filters
/// - Automatic auditing of entity changes
/// - Integration with ASP.NET Core Identity
/// - Tenant-aware entity creation
/// - Soft delete support through query filters
/// </remarks>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    #region Private Fields

    private readonly ICurrentTenantService _currentTenantService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApplicationDbContext> _logger;

    private static readonly MethodInfo SetGlobalQueryMethod = typeof(ApplicationDbContext)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(t => t.IsGenericMethod && t.Name == nameof(SetGlobalQuery));

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of ApplicationDbContext with required dependencies.
    /// </summary>
    /// <param name="options">Entity Framework configuration options</param>
    /// <param name="currentTenantService">Service for resolving current tenant context</param>
    /// <param name="httpContextAccessor">HTTP context accessor for user identification</param>
    /// <param name="logger">Logger for database operations</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentTenantService currentTenantService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApplicationDbContext> logger) : base(options)
    {
        _currentTenantService = currentTenantService ?? throw new ArgumentNullException(nameof(currentTenantService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region DbSets

    /// <summary>
    /// Tenants in the multi-tenant system.
    /// </summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>
    /// Products belonging to tenants.
    /// Automatically filtered by current tenant context.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();

    #endregion

    #region Model Configuration

    /// <summary>
    /// Configures the database schema and applies entity configurations.
    /// </summary>
    /// <param name="builder">Model builder for schema configuration</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        try
        {
            // Apply entity configurations
            ApplyEntityConfigurations(builder);

            // Apply global query filters for multi-tenancy
            ApplyMultiTenantQueryFilters(builder);

            _logger.LogDebug("Database model configuration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database model configuration");
            throw;
        }
    }

    /// <summary>
    /// Applies all entity configurations from the Configurations namespace.
    /// </summary>
    private static void ApplyEntityConfigurations(ModelBuilder builder)
    {
        // Apply individual configurations
        builder.ApplyConfiguration(new TenantConfiguration());
        builder.ApplyConfiguration(new ApplicationUserConfiguration());
        builder.ApplyConfiguration(new ProductConfiguration());

        // Alternatively, apply all configurations from assembly
        // builder.ApplyConfigurationsFromAssembly(typeof(TenantConfiguration).Assembly);
    }

    /// <summary>
    /// Applies global query filters for multi-tenant data isolation.
    /// </summary>
    private void ApplyMultiTenantQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = SetGlobalQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { builder });
            }
        }
    }

    /// <summary>
    /// Sets global query filter for tenant isolation on a specific entity type.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ITenantEntity</typeparam>
    /// <param name="builder">Model builder instance</param>
    public void SetGlobalQuery<T>(ModelBuilder builder) where T : class, ITenantEntity
    {
        builder.Entity<T>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
    }

    #endregion

    #region Change Tracking and Auditing

    /// <summary>
    /// Overrides SaveChangesAsync to implement automatic auditing and tenant assignment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Number of entities saved to database</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Apply tenant context to new entities
            ApplyTenantToNewEntities();

            // Apply audit information to tracked entities
            ApplyAuditInformation();

            var result = await base.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Saved {Count} entities to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving changes to database");
            throw;
        }
    }

    /// <summary>
    /// Applies current tenant ID to newly added entities that require tenant isolation.
    /// </summary>
    private void ApplyTenantToNewEntities()
    {
        var currentTenantId = _currentTenantService.TenantId;

        if (string.IsNullOrEmpty(currentTenantId))
        {
            throw new InvalidOperationException("Cannot save tenant-specific entities without a current tenant context");
        }

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
            {
                entry.Entity.TenantId = currentTenantId;
                _logger.LogDebug("Applied tenant ID {TenantId} to entity {EntityType}",
                    currentTenantId, entry.Entity.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Applies audit information (timestamps and user IDs) to tracked entities.
    /// </summary>
    private void ApplyAuditInformation()
    {
        var currentUserId = GetCurrentUserId();
        var currentTime = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = currentTime;
                    entry.Entity.CreatedBy = currentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = currentTime;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
            }
        }
    }

    /// <summary>
    /// Retrieves the current user ID from the HTTP context.
    /// </summary>
    /// <returns>Current user ID or null if not available</returns>
    private string? GetCurrentUserId()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? httpContext.User.FindFirst("sub")?.Value
                       ?? httpContext.User.FindFirst("user_id")?.Value;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve current user ID from HTTP context");
            return null;
        }
    }

    #endregion

    #region Database Operations

    /// <summary>
    /// Creates the database if it doesn't exist and applies pending migrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    public async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure database is created and migrated");
            throw;
        }
    }

    /// <summary>
    /// Checks if there are pending migrations that need to be applied.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if there are pending migrations</returns>
    public async Task<bool> HasPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingMigrations = await Database.GetPendingMigrationsAsync(cancellationToken);
            return pendingMigrations.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for pending migrations");
            throw;
        }
    }

    #endregion
}
