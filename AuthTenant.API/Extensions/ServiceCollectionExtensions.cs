using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using AuthTenant.Domain.Entities;
using AuthTenant.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AuthTenant.API.Extensions
{

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationServices();
            services.AddInfrastructureServices(configuration);

            services.AddIdentityServices();
            services.AddJwtAuthentication(configuration);

            services.AddApiControllers();
            services.AddCorsConfiguration();
            services.AddSwaggerDocumentation();
            services.AddHealthCheckServices();

            return services;
        }

        private static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            AuthTenant.Application.Extensions.ServiceCollectionExtensions.AddApplication(services);
            return services;
        }

        private static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            AuthTenant.Infrastructure.Extensions.ServiceCollectionExtensions.AddInfrastructure(services, configuration);
            return services;
        }

        private static IServiceCollection AddIdentityServices(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(ConfigureIdentityOptions)
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }

        private static void ConfigureIdentityOptions(IdentityOptions options)
        {
            // Password policy
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 1;

            // User policy
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Lockout policy
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // Sign-in policy
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        }

        private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var jwtConfig = ValidateJwtConfiguration(jwtSettings);

            services.AddAuthentication(ConfigureAuthenticationOptions)
                .AddJwtBearer(options => ConfigureJwtBearerOptions(options, jwtConfig));

            return services;
        }

        private static JwtConfiguration ValidateJwtConfiguration(IConfigurationSection jwtSettings)
        {
            var secret = jwtSettings["Secret"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expireMinutes = jwtSettings["ExpireMinutes"];

            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("JWT Secret is not configured in appsettings");

            if (string.IsNullOrEmpty(issuer))
                throw new InvalidOperationException("JWT Issuer is not configured in appsettings");

            if (string.IsNullOrEmpty(audience))
                throw new InvalidOperationException("JWT Audience is not configured in appsettings");

            if (secret.Length < 32)
                throw new InvalidOperationException("JWT Secret must be at least 32 characters long for security");

            return new JwtConfiguration
            {
                Secret = secret,
                Issuer = issuer,
                Audience = audience,
                Key = Encoding.UTF8.GetBytes(secret),
                ExpireMinutes = int.TryParse(expireMinutes, out var minutes) ? minutes : 60
            };
        }

        private static void ConfigureAuthenticationOptions(AuthenticationOptions options)
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }

        private static void ConfigureJwtBearerOptions(JwtBearerOptions options, JwtConfiguration jwtConfig)
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidIssuer = jwtConfig.Issuer,
                ValidAudience = jwtConfig.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(jwtConfig.Key),
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            options.Events = CreateJwtBearerEvents();
        }

        private static JwtBearerEvents CreateJwtBearerEvents()
        {
            return new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT Authentication failed: {Message}", context.Exception.Message);

                    if (context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                        logger.LogDebug("JWT Exception details: {Exception}", context.Exception);

                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("JWT Token validated for user: {UserName}",
                        context.Principal?.Identity?.Name ?? "Unknown");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT Challenge triggered: {Error} - {ErrorDescription}",
                        context.Error, context.ErrorDescription);
                    return Task.CompletedTask;
                }
            };
        }

        private static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.SuppressAsyncSuffixInActionNames = false;
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            return services;
        }

        private static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                // Production CORS policy - restrictive
                options.AddPolicy("Production", policy =>
                {
                    policy.WithOrigins("https://mydomain.com", "https://www.mydomain.com")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                });

                // Development CORS policy - specific origins
                options.AddPolicy("Development", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:3000", "https://localhost:3000", // React
                            "http://localhost:4200", "https://localhost:4200", // Angular
                            "http://localhost:8080", "https://localhost:8080"  // Vue
                          )
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });

                // Testing CORS policy - permissive (only for testing)
                options.AddPolicy("Testing", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            return services;
        }

        private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "AuthTenant API",
                    Version = "v1.0",
                    Description = "A robust multi-tenant API built with .NET 8, implementing CQRS, DDD, and Clean Architecture patterns",
                    Contact = new OpenApiContact
                    {
                        Name = "AuthTenant Development Team",
                        Email = "your-email@example.com",
                        Url = new Uri("https://github.com/your-repo")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                // Configure JWT security scheme
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = """
                        JWT Authorization header using the Bearer scheme.
                        
                        Enter 'Bearer' [space] and then your valid token in the text input below.
                        
                        Example: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                        """
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                IncludeXmlDocumentation(c);

                c.DescribeAllParametersInCamelCase();
                c.UseInlineDefinitionsForEnums();
            });

            return services;
        }

        private static void IncludeXmlDocumentation(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
        {
            var assemblies = new[]
            {
                Assembly.GetExecutingAssembly(),
                Assembly.GetAssembly(typeof(Application.Extensions.ServiceCollectionExtensions)),
                Assembly.GetAssembly(typeof(Domain.Entities.ApplicationUser))
            };

            foreach (var assembly in assemblies.Where(a => a != null))
            {
                var xmlFile = $"{assembly!.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath);
            }
        }

        private static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>("database", tags: ["db", "sql"])
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["self"]);

            return services;
        }

        private sealed class JwtConfiguration
        {
            public required string Secret { get; init; }
            public required string Issuer { get; init; }
            public required string Audience { get; init; }
            public required byte[] Key { get; init; }
            public int ExpireMinutes { get; init; } = 60;
        }
    }
}
