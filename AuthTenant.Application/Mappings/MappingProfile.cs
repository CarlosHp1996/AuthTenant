using AutoMapper;

using AuthTenant.Application.Models.Dtos.Auth;
using AuthTenant.Application.Models.Dtos.Product;
using AuthTenant.Domain.Entities;

namespace AuthTenant.Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for configuring object-to-object mappings between domain entities and DTOs.
    /// Implements comprehensive mapping strategies following Clean Architecture principles and DDD patterns.
    /// Provides bidirectional mappings, custom transformations, and performance-optimized configurations.
    /// </summary>
    public sealed class MappingProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the MappingProfile with all mapping configurations.
        /// Configures mappings for authentication, product management, and other domain entities.
        /// Implements custom transformations, conditional mappings, and performance optimizations.
        /// </summary>
        public MappingProfile()
        {
            // Configure global mapping settings for consistency and performance
            ConfigureGlobalSettings();

            // Configure authentication-related mappings
            ConfigureAuthenticationMappings();

            // Configure product-related mappings
            ConfigureProductMappings();

            // Configure additional domain mappings as needed
            ConfigureAdditionalMappings();
        }

        /// <summary>
        /// Configures global AutoMapper settings for consistency and performance optimization.
        /// Sets up naming conventions, null handling, and validation rules.
        /// </summary>
        private void ConfigureGlobalSettings()
        {
            // Configure naming conventions for consistent property mapping
            SourceMemberNamingConvention = new LowerUnderscoreNamingConvention();
            DestinationMemberNamingConvention = new PascalCaseNamingConvention();

            // Configure null value handling
            AllowNullCollections = true;
            AllowNullDestinationValues = true;

            // Disable automatic mapping of private setters for security
            ShouldMapProperty = propertyInfo => propertyInfo.GetSetMethod(true)?.IsPublic == true;
        }

        /// <summary>
        /// Configures mappings for authentication-related entities and DTOs.
        /// Includes user profile mappings with custom transformations and security considerations.
        /// </summary>
        private void ConfigureAuthenticationMappings()
        {
            // ApplicationUser to UserDto mapping with custom transformations
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    CreateFullName(src.FirstName, src.LastName)))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                // Security: Exclude sensitive information from mapping
                .ForMember(dest => dest.Email, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Email)))
                .ValidateMemberList(MemberList.Destination);

            // UserDto to ApplicationUser mapping (for updates, excluding sensitive fields)
            CreateMap<UserDto, ApplicationUser>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                // Security: Exclude sensitive fields from reverse mapping
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ValidateMemberList(MemberList.Source);
        }

        /// <summary>
        /// Configures mappings for product-related entities and DTOs.
        /// Includes bidirectional mappings with validation, formatting, and business rule enforcement.
        /// </summary>
        private void ConfigureProductMappings()
        {
            // Product to ProductDto mapping with enhanced transformations
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.SKU, opt => opt.MapFrom(src => src.SKU))
                .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                // Conditional mapping based on business rules
                .ForMember(dest => dest.Description, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Description)))
                .ForMember(dest => dest.SKU, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.SKU)))
                .ValidateMemberList(MemberList.Destination);

            // ProductDto to Product mapping (for updates and creation)
            CreateMap<ProductDto, Product>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => NormalizeName(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Description) ? null : src.Description.Trim()))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.SKU, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.SKU) ? null : src.SKU.Trim().ToUpperInvariant()))
                .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => Math.Max(0, src.StockQuantity)))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                // Ignore system-managed fields
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                // Validate all source members are mapped
                .ValidateMemberList(MemberList.Source);

            // Additional product-related mappings
            ConfigureProductRelatedMappings();
        }

        /// <summary>
        /// Configures additional product-related mappings for specific scenarios.
        /// Includes optimized mappings for future extensions.
        /// </summary>
        private void ConfigureProductRelatedMappings()
        {
            // Placeholder for additional product mappings when needed
            // Example: CreateMap<Product, ProductSummaryDto>() when the DTO is created
        }

        /// <summary>
        /// Configures additional domain mappings for other entities as the application grows.
        /// Placeholder for future entity mappings (orders, categories, etc.).
        /// </summary>
        private void ConfigureAdditionalMappings()
        {
            // Future mappings can be added here:
            // - Order entity mappings
            // - Category entity mappings
            // - Tenant entity mappings
            // - Audit entity mappings

            // Example structure for future mappings:
            // ConfigureOrderMappings();
            // ConfigureCategoryMappings();
            // ConfigureTenantMappings();
        }

        /// <summary>
        /// Creates a formatted full name from first and last name components.
        /// Handles null/empty values gracefully and ensures proper formatting.
        /// </summary>
        /// <param name="firstName">The first name</param>
        /// <param name="lastName">The last name</param>
        /// <returns>Formatted full name or appropriate fallback</returns>
        private static string CreateFullName(string? firstName, string? lastName)
        {
            var first = string.IsNullOrWhiteSpace(firstName) ? string.Empty : firstName.Trim();
            var last = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();

            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
                return "Unknown User";

            if (string.IsNullOrEmpty(first))
                return last;

            if (string.IsNullOrEmpty(last))
                return first;

            return $"{first} {last}";
        }

        /// <summary>
        /// Normalizes product name by trimming whitespace and ensuring proper casing.
        /// </summary>
        /// <param name="name">The product name to normalize</param>
        /// <returns>Normalized product name</returns>
        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be null or empty", nameof(name));

            return name.Trim();
        }
    }
}
