using AuthTenant.Domain.Entities;
using AuthTenant.Domain.Interfaces;
using AuthTenant.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Infrastructure.Data;

/// <summary>
/// Factory for creating ApplicationDbContext instances at design-time (migrations, etc.).
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AuthTenant.API"))
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        // Create design-time services
        var designTimeTenantService = new DesignTimeCurrentTenantService();
        var designTimeHttpContextAccessor = new DesignTimeHttpContextAccessor();
        var designTimeLogger = new DesignTimeLogger();

        return new ApplicationDbContext(
            optionsBuilder.Options,
            designTimeTenantService,
            designTimeHttpContextAccessor,
            designTimeLogger);
    }
}

/// <summary>
/// Simple implementation of ICurrentTenantService for design-time operations.
/// </summary>
public class DesignTimeCurrentTenantService : ICurrentTenantService
{
    public string? TenantId => "default";

    public void SetTenant(string? tenantId)
    {
        // Not implemented for design-time
    }

    public bool HasValidTenant => true;

    public bool IsDefaultTenant => true;
}

/// <summary>
/// Simple implementation of IHttpContextAccessor for design-time operations.
/// </summary>
public class DesignTimeHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; } = null;
}

/// <summary>
/// Simple implementation of ILogger for design-time operations.
/// </summary>
public class DesignTimeLogger : ILogger<ApplicationDbContext>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // No-op for design-time
    }
}
