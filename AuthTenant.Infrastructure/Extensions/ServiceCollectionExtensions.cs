using AuthTenant.Domain.Interfaces;
using AuthTenant.Infrastructure.Data.Context;
using AuthTenant.Infrastructure.MultiTenant;
using AuthTenant.Infrastructure.Repositories;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure Infrastructure layer dependencies.
/// Provides a centralized way to register all infrastructure services including
/// database context, repositories, multi-tenant services, and cross-cutting concerns.
/// </summary>
/// <remarks>
/// This class follows the Extension Methods pattern and Service Locator pattern
/// to provide a clean API for dependency injection configuration.
/// Supports multiple configuration overloads for different scenarios.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all infrastructure services to the service collection with full configuration.
    /// This is the primary method that should be used in production scenarios.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">Configuration instance for connection strings and settings</param>
    /// <returns>The configured service collection for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configuration is null</exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure Entity Framework and Database
        AddDatabaseServices(services, configuration);

        // Configure Multi-Tenant Services
        AddMultiTenantServices(services);

        // Configure Repository Services
        AddRepositoryServices(services);

        // Configure Cross-Cutting Concerns
        AddCrossCuttingServices(services);

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with minimal configuration for testing scenarios.
    /// This overload should primarily be used for unit testing or when database
    /// configuration is handled separately.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The configured service collection for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    public static IServiceCollection AddInfrastructureWithoutDatabase(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure Multi-Tenant Services
        AddMultiTenantServices(services);

        // Configure Repository Services (without database context)
        AddRepositoryServicesWithoutDatabase(services);

        // Configure Cross-Cutting Concerns
        AddCrossCuttingServices(services);

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with custom database configuration.
    /// Useful for scenarios where database options need to be customized.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configureDb">Action to configure database options</param>
    /// <returns>The configured service collection for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configureDb is null</exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDb);

        // Configure Entity Framework with custom options
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            configureDb(options);
        });

        // Configure Multi-Tenant Services
        AddMultiTenantServices(services);

        // Configure Repository Services
        AddRepositoryServices(services);

        // Configure Cross-Cutting Concerns
        AddCrossCuttingServices(services);

        return services;
    }

    #region Private Configuration Methods

    /// <summary>
    /// Configures Entity Framework DbContext and database-related services.
    /// </summary>
    private static void AddDatabaseServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'DefaultConnection' is required but was not found in configuration.");
        }

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("AuthTenant.Infrastructure");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Configure logging and diagnostics based on environment
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                options.LogTo(Console.WriteLine, LogLevel.Information);
            }

            // Configure performance optimizations
            options.EnableServiceProviderCaching();
        });

        // Note: Health checks can be added by installing Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
        // services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "db", "sql" });
    }

    /// <summary>
    /// Configures multi-tenant services for tenant resolution and isolation.
    /// </summary>
    private static void AddMultiTenantServices(IServiceCollection services)
    {
        // HTTP Context Accessor for tenant resolution
        services.AddHttpContextAccessor();

        // Tenant resolution service
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();

        // Add tenant repository with caching
        services.AddScoped<ITenantRepository, TenantRepository>();

        // Optional: Add memory cache for tenant information
        services.AddMemoryCache();
    }

    /// <summary>
    /// Configures repository services and data access patterns.
    /// </summary>
    private static void AddRepositoryServices(IServiceCollection services)
    {
        // Base repository pattern
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

        // Specific entity repositories
        services.AddScoped<IProductRepository, ProductRepository>();

        // Unit of Work pattern (if implemented)
        // services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add repository decorators for cross-cutting concerns
        // services.Decorate<IProductRepository, CachedProductRepository>();
        // services.Decorate<IProductRepository, LoggingProductRepository>();
    }

    /// <summary>
    /// Configures repository services without database context for testing.
    /// </summary>
    private static void AddRepositoryServicesWithoutDatabase(IServiceCollection services)
    {
        // Register repository interfaces without implementations
        // This is useful for testing scenarios where you provide mocks
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IProductRepository, ProductRepository>();
    }

    /// <summary>
    /// Configures cross-cutting concerns like logging, caching, and monitoring.
    /// </summary>
    private static void AddCrossCuttingServices(IServiceCollection services)
    {
        // Add memory cache as default caching mechanism
        services.AddMemoryCache();

        // Note: For Redis cache, install Microsoft.Extensions.Caching.StackExchangeRedis
        // services.AddStackExchangeRedisCache(options => {
        //     options.Configuration = configuration.GetConnectionString("Redis");
        // });

        // Add background services
        // services.AddHostedService<TenantCacheRefreshService>();

        // Add monitoring and metrics
        // services.AddApplicationInsightsTelemetry();

        // Add custom services
        // services.AddScoped<IEmailService, EmailService>();
        // services.AddScoped<IAuditService, AuditService>();
    }

    #endregion

    #region Development and Testing Helpers

    /// <summary>
    /// Adds infrastructure services configured for in-memory database testing.
    /// Should only be used in test environments.
    /// Note: Requires Microsoft.EntityFrameworkCore.InMemory package.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="databaseName">Optional database name for isolation between tests</param>
    /// <returns>The configured service collection for method chaining</returns>
    public static IServiceCollection AddInfrastructureForTesting(
        this IServiceCollection services,
        string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var dbName = databaseName ?? Guid.NewGuid().ToString();

        // Note: This requires Microsoft.EntityFrameworkCore.InMemory package
        // Uncomment when package is installed:
        // services.AddDbContext<ApplicationDbContext>(options =>
        // {
        //     options.UseInMemoryDatabase(dbName);
        //     options.EnableSensitiveDataLogging();
        //     options.EnableDetailedErrors();
        // });

        // Configure minimal services for testing without database
        AddMultiTenantServices(services);
        AddRepositoryServicesWithoutDatabase(services);

        // Add test-specific services
        services.AddSingleton<IHttpContextAccessor, TestHttpContextAccessor>();

        return services;
    }

    /// <summary>
    /// Validates that all required infrastructure services are properly registered.
    /// Should be called during application startup in development environment.
    /// </summary>
    /// <param name="services">The service collection to validate</param>
    /// <returns>True if all services are properly configured</returns>
    public static bool ValidateInfrastructureConfiguration(this IServiceCollection services)
    {
        var requiredServices = new[]
        {
            typeof(ApplicationDbContext),
            typeof(ICurrentTenantService),
            typeof(ITenantRepository),
            typeof(IProductRepository),
            typeof(IRepository<>),
            typeof(IHttpContextAccessor)
        };

        var serviceProvider = services.BuildServiceProvider();

        foreach (var serviceType in requiredServices)
        {
            try
            {
                if (serviceType.IsGenericTypeDefinition)
                {
                    // Skip generic type definitions for this validation
                    continue;
                }

                var service = serviceProvider.GetService(serviceType);
                if (service == null)
                {
                    Console.WriteLine($"Warning: Required service {serviceType.Name} is not registered.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating service {serviceType.Name}: {ex.Message}");
                return false;
            }
        }

        return true;
    }

    #endregion
}

/// <summary>
/// Test implementation of IHttpContextAccessor for testing scenarios.
/// </summary>
internal class TestHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}
