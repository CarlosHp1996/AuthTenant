using AuthTenant.API.Extensions;
using AuthTenant.API.Middleware;
using AuthTenant.Infrastructure.Data.Context;
using AuthTenant.Infrastructure.MultiTenant;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Add services to the container
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthTenant API V1");
        c.RoutePrefix = ""; // Set Swagger UI as default page
        c.DisplayRequestDuration();
        c.DefaultModelsExpandDepth(-1); // Hide models section by default
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Don't expand operations by default
    });

    logger.LogInformation("🚀 Swagger UI enabled at: /swagger");
}

// Security and middleware pipeline (order matters!)
app.UseHttpsRedirection();
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "AllowSpecificOrigins");

// Exception handling must be first
app.UseMiddleware<ExceptionMiddleware>();

// Tenant resolution before authentication
app.UseMiddleware<TenantMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health checks and API endpoints
app.UseHealthChecks("/health");
app.MapControllers();

// Redirect root to Swagger in development
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

// Database initialization and health check
await InitializeDatabaseAsync(app, logger);

logger.LogInformation("AuthTenant API started successfully");
app.Run();

// Database initialization method
static async Task InitializeDatabaseAsync(WebApplication app, ILogger logger)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Test database connection
        var canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("Database connection established successfully");

            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("Checking for pending migrations...");
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database schema is up to date");
            }
        }
        else
        {
            logger.LogWarning("Could not connect to database");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed: {ErrorMessage}", ex.Message);

        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}