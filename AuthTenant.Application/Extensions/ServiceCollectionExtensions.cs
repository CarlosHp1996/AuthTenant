
using System.Reflection;
using AutoMapper;
using AuthTenant.Application.Behaviors;
using AuthTenant.Application.Mappings;
using AuthTenant.Application.Services;
using AuthTenant.Application.Services.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthTenant.Application.Extensions
{
    /// <summary>
    /// Extension methods for configuring Application layer services in the dependency injection container.
    /// Implements comprehensive service registration following Clean Architecture principles and SOLID design patterns.
    /// Provides centralized configuration for MediatR, AutoMapper, FluentValidation, and application-specific services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Application layer services and dependencies in the dependency injection container.
        /// Configures MediatR for CQRS pattern, AutoMapper for object mapping, FluentValidation for input validation,
        /// pipeline behaviors for cross-cutting concerns, and application-specific services.
        /// </summary>
        /// <param name="services">The service collection to extend</param>
        /// <returns>The service collection for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when services parameter is null</exception>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            var assembly = Assembly.GetExecutingAssembly();

            // Log the start of application services registration
            LogRegistrationStart(services);

            try
            {
                // Register MediatR for CQRS pattern implementation
                RegisterMediatR(services, assembly);

                // Register AutoMapper for object-to-object mapping
                RegisterAutoMapper(services);

                // Register FluentValidation for comprehensive input validation
                RegisterFluentValidation(services, assembly);

                // Register pipeline behaviors for cross-cutting concerns
                RegisterPipelineBehaviors(services);

                // Register application-specific services
                RegisterApplicationServices(services);

                // Log successful registration completion
                LogRegistrationCompletion(services);

                return services;
            }
            catch (Exception ex)
            {
                LogRegistrationError(services, ex);
                throw new InvalidOperationException(
                    "Failed to register Application layer services. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Registers MediatR services for implementing CQRS (Command Query Responsibility Segregation) pattern.
        /// Configures automatic discovery and registration of all command handlers, query handlers, and notification handlers.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="assembly">The assembly containing the handlers</param>
        private static void RegisterMediatR(IServiceCollection services, Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            services.AddMediatR(cfg =>
            {
                // Register all handlers from the current assembly
                cfg.RegisterServicesFromAssembly(assembly);

                // Configure additional MediatR settings if needed
                cfg.Lifetime = ServiceLifetime.Transient; // Ensure handlers are transient for thread safety
            });

            LogServiceRegistration(services, "MediatR", "CQRS pattern implementation with command/query handlers");
        }

        /// <summary>
        /// Registers AutoMapper services for object-to-object mapping functionality.
        /// Configures mapping profiles for converting between domain entities, DTOs, and view models.
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void RegisterAutoMapper(IServiceCollection services)
        {
            services.AddAutoMapper(config =>
            {
                // Add the main mapping profile
                config.AddProfile<MappingProfile>();

                // Configure AutoMapper settings for better performance and safety
                config.AllowNullCollections = true;
                config.AllowNullDestinationValues = true;
            });

            LogServiceRegistration(services, "AutoMapper", "Object-to-object mapping with MappingProfile");
        }

        /// <summary>
        /// Registers FluentValidation services for comprehensive input validation.
        /// Automatically discovers and registers all validators in the assembly for command and query validation.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="assembly">The assembly containing the validators</param>
        private static void RegisterFluentValidation(IServiceCollection services, Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            // Register all validators from the assembly
            services.AddValidatorsFromAssembly(assembly, lifetime: ServiceLifetime.Transient);

            // Configure FluentValidation settings
            ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
            ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Continue;

            LogServiceRegistration(services, "FluentValidation", $"Input validation with {GetValidatorCount(assembly)} validators");
        }

        /// <summary>
        /// Registers MediatR pipeline behaviors for implementing cross-cutting concerns.
        /// Configures validation and logging behaviors that execute before and after request handling.
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void RegisterPipelineBehaviors(IServiceCollection services)
        {
            // Register ValidationBehavior - executes validation before request handling
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            LogServiceRegistration(services, "ValidationBehavior", "Pre-request validation pipeline");

            // Register LoggingBehavior - provides comprehensive request/response logging
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            LogServiceRegistration(services, "LoggingBehavior", "Request/response logging pipeline");

            // Note: Order of registration matters - ValidationBehavior should execute before LoggingBehavior
            // The order will be: LoggingBehavior (start) -> ValidationBehavior -> Handler -> ValidationBehavior -> LoggingBehavior (end)
        }

        /// <summary>
        /// Registers application-specific services and their interfaces.
        /// Configures service lifetimes and dependencies for business logic services.
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void RegisterApplicationServices(IServiceCollection services)
        {
            // Register TokenService for JWT token generation and validation
            services.AddScoped<ITokenService, TokenService>();
            LogServiceRegistration(services, "ITokenService", "JWT token generation and validation service");

            // Future application services can be registered here:
            // services.AddScoped<IEmailService, EmailService>();
            // services.AddScoped<INotificationService, NotificationService>();
            // services.AddScoped<ICacheService, CacheService>();
        }

        /// <summary>
        /// Logs the start of application services registration process
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void LogRegistrationStart(IServiceCollection services)
        {
            var logger = CreateLogger(services);
            logger?.LogInformation("🚀 Starting Application layer services registration...");
        }

        /// <summary>
        /// Logs the successful completion of application services registration
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void LogRegistrationCompletion(IServiceCollection services)
        {
            var logger = CreateLogger(services);
            var serviceCount = services.Count;
            logger?.LogInformation("✅ Application layer services registration completed successfully. Total services: {ServiceCount}", serviceCount);
        }

        /// <summary>
        /// Logs errors that occur during service registration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="exception">The exception that occurred</param>
        private static void LogRegistrationError(IServiceCollection services, Exception exception)
        {
            var logger = CreateLogger(services);
            logger?.LogError(exception, "❌ Failed to register Application layer services: {ErrorMessage}", exception.Message);
        }

        /// <summary>
        /// Logs the registration of individual services for debugging and monitoring
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="serviceName">The name of the service being registered</param>
        /// <param name="description">Description of the service functionality</param>
        private static void LogServiceRegistration(IServiceCollection services, string serviceName, string description)
        {
            var logger = CreateLogger(services);
            logger?.LogDebug("📦 Registered {ServiceName}: {Description}", serviceName, description);
        }

        /// <summary>
        /// Creates a logger instance for service registration logging
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>Logger instance or null if not available</returns>
        private static ILogger? CreateLogger(IServiceCollection services)
        {
            try
            {
                var serviceProvider = services.BuildServiceProvider();
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                return loggerFactory?.CreateLogger(typeof(ServiceCollectionExtensions));
            }
            catch
            {
                // Ignore errors when creating logger during registration
                return null;
            }
        }

        /// <summary>
        /// Gets the count of validators in the specified assembly
        /// </summary>
        /// <param name="assembly">The assembly to scan</param>
        /// <returns>Number of validator classes found</returns>
        private static int GetValidatorCount(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes()
                    .Count(type => type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)));
            }
            catch
            {
                return 0; // Return 0 if unable to count validators
            }
        }

        /// <summary>
        /// Validates that all required services are properly registered (useful for testing)
        /// </summary>
        /// <param name="services">The service collection to validate</param>
        /// <returns>True if all required services are registered</returns>
        public static bool ValidateRequiredServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            var requiredServices = new[]
            {
                typeof(IMediator),
                typeof(IMapper),
                typeof(ITokenService)
            };

            return requiredServices.All(serviceType =>
                services.Any(descriptor =>
                    descriptor.ServiceType == serviceType ||
                    serviceType.IsAssignableFrom(descriptor.ServiceType)));
        }

        /// <summary>
        /// Gets service registration information for diagnostics and monitoring
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>Dictionary containing service registration details</returns>
        public static Dictionary<string, object> GetRegistrationInfo(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            return new Dictionary<string, object>
            {
                { "TotalServices", services.Count },
                { "HasMediatR", services.Any(s => s.ServiceType == typeof(IMediator)) },
                { "HasAutoMapper", services.Any(s => s.ServiceType == typeof(IMapper)) },
                { "HasTokenService", services.Any(s => s.ServiceType == typeof(ITokenService)) },
                { "PipelineBehaviors", services.Count(s => s.ServiceType.IsGenericType &&
                    s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)) },
                { "RegistrationTimestamp", DateTime.UtcNow }
            };
        }
    }
}
