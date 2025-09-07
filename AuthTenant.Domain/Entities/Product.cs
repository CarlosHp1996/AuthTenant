using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthTenant.Domain.Entities
{
    /// <summary>
    /// Business rule exception for domain-specific validation errors.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
        public BusinessRuleException(string message, Exception innerException) : base(message, innerException) { }
    }
    /// <summary>
    /// Product entity representing a product in the e-commerce catalog.
    /// Implements rich domain model with business logic and validation.
    /// Follows DDD patterns with value objects and domain events.
    /// </summary>
    public class Product : BaseEntity
    {
        #region Private Fields

        private string _name = string.Empty;
        private decimal _price;
        private int _stockQuantity;

        #endregion

        #region Properties

        /// <summary>
        /// Product name with validation and formatting.
        /// Must be between 2 and 200 characters and contain valid characters.
        /// </summary>
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
        public string Name
        {
            get => _name;
            set => SetName(value);
        }

        /// <summary>
        /// Optional product description with detailed information.
        /// Limited to 2000 characters to ensure reasonable storage and display.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Product price with business validation.
        /// Must be positive and within reasonable commercial limits.
        /// Stored with high precision for accurate financial calculations.
        /// </summary>
        [Required(ErrorMessage = "Price is required")]
        [Column(TypeName = "decimal(18,4)")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99")]
        public decimal Price
        {
            get => _price;
            set => SetPrice(value);
        }

        /// <summary>
        /// Stock Keeping Unit - unique product identifier for inventory management.
        /// Optional but recommended for professional inventory systems.
        /// </summary>
        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        [RegularExpression(@"^[A-Z0-9\-_]*$", ErrorMessage = "SKU can only contain uppercase letters, numbers, hyphens, and underscores")]
        public string? SKU { get; set; }

        /// <summary>
        /// Indicates whether the product is active and available for sale.
        /// Inactive products are hidden from catalogs but maintain data integrity.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Current stock quantity with business validation.
        /// Cannot be negative and has reasonable upper limits.
        /// </summary>
        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be non-negative")]
        public int StockQuantity
        {
            get => _stockQuantity;
            set => SetStockQuantity(value);
        }

        /// <summary>
        /// Product category for organization and filtering.
        /// Helps with catalog navigation and inventory management.
        /// </summary>
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string? Category { get; set; }

        /// <summary>
        /// Product tags for search and filtering capabilities.
        /// Stored as JSON array for flexibility and query performance.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Product weight in kilograms for shipping calculations.
        /// Optional but important for logistics and shipping cost estimation.
        /// </summary>
        [Column(TypeName = "decimal(8,3)")]
        [Range(0.001, 99999.999, ErrorMessage = "Weight must be between 0.001 and 99,999.999 kg")]
        public decimal? Weight { get; set; }

        /// <summary>
        /// Product dimensions for packaging and shipping optimization.
        /// All dimensions in centimeters for consistency.
        /// </summary>
        [Column(TypeName = "decimal(8,2)")]
        [Range(0.1, 99999.99, ErrorMessage = "Length must be between 0.1 and 99,999.99 cm")]
        public decimal? Length { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [Range(0.1, 99999.99, ErrorMessage = "Width must be between 0.1 and 99,999.99 cm")]
        public decimal? Width { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [Range(0.1, 99999.99, ErrorMessage = "Height must be between 0.1 and 99,999.99 cm")]
        public decimal? Height { get; set; }

        /// <summary>
        /// Minimum stock level threshold for automatic reorder alerts.
        /// When stock falls below this level, notifications should be triggered.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock level must be non-negative")]
        public int MinimumStockLevel { get; set; } = 0;

        /// <summary>
        /// Maximum stock level for inventory optimization.
        /// Helps prevent overstocking and optimize warehouse space.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Maximum stock level must be non-negative")]
        public int MaximumStockLevel { get; set; } = int.MaxValue;

        /// <summary>
        /// Indicates if the product is currently featured or promoted.
        /// Used for marketing displays and special promotions.
        /// </summary>
        public bool IsFeatured { get; set; } = false;

        /// <summary>
        /// Number of times this product has been viewed.
        /// Useful for analytics and popularity tracking.
        /// </summary>
        public long ViewCount { get; set; } = 0;

        /// <summary>
        /// Last time the product was viewed.
        /// Helps track product engagement and freshness.
        /// </summary>
        public DateTime? LastViewedAt { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Normalized product name for consistent processing and searches.
        /// Trims whitespace and converts to proper case.
        /// </summary>
        [NotMapped]
        public string NormalizedName => Name?.Trim() ?? string.Empty;

        /// <summary>
        /// Normalized SKU in uppercase for consistency.
        /// Ensures SKU comparisons are case-insensitive.
        /// </summary>
        [NotMapped]
        public string? NormalizedSKU => SKU?.Trim().ToUpperInvariant();

        /// <summary>
        /// Indicates if the product is currently in stock.
        /// Considers both stock quantity and active status.
        /// </summary>
        [NotMapped]
        public bool IsInStock => IsActive && StockQuantity > 0;

        /// <summary>
        /// Indicates if stock is running low based on minimum threshold.
        /// Useful for inventory management and reorder alerts.
        /// </summary>
        [NotMapped]
        public bool IsLowStock => StockQuantity <= MinimumStockLevel && MinimumStockLevel > 0;

        /// <summary>
        /// Indicates if stock is overstocked based on maximum threshold.
        /// Helps identify inventory optimization opportunities.
        /// </summary>
        [NotMapped]
        public bool IsOverstocked => StockQuantity >= MaximumStockLevel && MaximumStockLevel < int.MaxValue;

        /// <summary>
        /// Calculates the product volume in cubic centimeters.
        /// Returns null if any dimension is missing.
        /// </summary>
        [NotMapped]
        public decimal? Volume => Length.HasValue && Width.HasValue && Height.HasValue
            ? Length * Width * Height
            : null;

        /// <summary>
        /// Gets the stock status as a descriptive string.
        /// Useful for display purposes and user interfaces.
        /// </summary>
        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (!IsActive) return "Inactive";
                if (StockQuantity == 0) return "Out of Stock";
                if (IsLowStock) return "Low Stock";
                if (IsOverstocked) return "Overstocked";
                return "In Stock";
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for Entity Framework and serialization.
        /// </summary>
        public Product() { }

        /// <summary>
        /// Creates a new product with required parameters.
        /// Validates business rules and initializes the entity properly.
        /// </summary>
        /// <param name="name">Product name (required)</param>
        /// <param name="price">Product price (must be positive)</param>
        /// <param name="tenantId">Tenant identifier for multi-tenancy</param>
        /// <param name="createdBy">User who created the product</param>
        /// <exception cref="ArgumentException">Thrown when required parameters are invalid</exception>
        /// <exception cref="BusinessRuleException">Thrown when business rules are violated</exception>
        public Product(string name, decimal price, string tenantId, string? createdBy = null)
        {
            SetName(name);
            SetPrice(price);
            SetTenantId(tenantId);
            CreatedBy = createdBy;

            // Initialize with safe defaults
            StockQuantity = 0;
            IsActive = true;
            Tags = new List<string>();

            // Validate the created entity
            ValidateEntity();
        }

        #endregion

        #region Domain Methods - Price Management

        /// <summary>
        /// Updates the product price with business validation and audit trail.
        /// Ensures price changes follow business rules and are properly tracked.
        /// </summary>
        /// <param name="newPrice">New price value</param>
        /// <param name="updatedBy">User making the price change</param>
        /// <exception cref="BusinessRuleException">Thrown when price is invalid or change is not allowed</exception>
        public void UpdatePrice(decimal newPrice, string? updatedBy = null)
        {
            if (newPrice <= 0)
                throw new BusinessRuleException("Price must be greater than zero");

            if (newPrice > 999999.99m)
                throw new BusinessRuleException("Price cannot exceed 999,999.99");

            // Check for extreme price changes (more than 500% increase or 90% decrease)
            if (_price > 0)
            {
                var priceChangeRatio = newPrice / _price;
                if (priceChangeRatio > 5.0m)
                    throw new BusinessRuleException("Price increase cannot exceed 500% of current price");
                if (priceChangeRatio < 0.1m)
                    throw new BusinessRuleException("Price decrease cannot be more than 90% of current price");
            }

            var oldPrice = _price;
            _price = newPrice;

            MarkAsUpdated(updatedBy);

            // Domain event could be raised here for price change notifications
            // RaiseDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice, updatedBy));
        }

        /// <summary>
        /// Applies a percentage discount to the current price.
        /// Useful for promotions and bulk pricing strategies.
        /// </summary>
        /// <param name="discountPercentage">Discount percentage (0-100)</param>
        /// <param name="updatedBy">User applying the discount</param>
        /// <exception cref="BusinessRuleException">Thrown when discount percentage is invalid</exception>
        public void ApplyDiscount(decimal discountPercentage, string? updatedBy = null)
        {
            if (discountPercentage < 0 || discountPercentage > 100)
                throw new BusinessRuleException("Discount percentage must be between 0 and 100");

            var discountAmount = _price * (discountPercentage / 100m);
            var newPrice = _price - discountAmount;

            UpdatePrice(newPrice, updatedBy);
        }

        #endregion

        #region Domain Methods - Stock Management

        /// <summary>
        /// Adjusts stock quantity with business validation and audit trail.
        /// Supports both positive (restock) and negative (sale/removal) adjustments.
        /// </summary>
        /// <param name="quantity">Quantity to add (positive) or remove (negative)</param>
        /// <param name="updatedBy">User making the stock adjustment</param>
        /// <param name="reason">Reason for the stock adjustment</param>
        /// <exception cref="BusinessRuleException">Thrown when adjustment would result in negative stock</exception>
        public void AdjustStock(int quantity, string? updatedBy = null, string? reason = null)
        {
            var newStock = _stockQuantity + quantity;

            if (newStock < 0)
                throw new BusinessRuleException($"Insufficient stock. Current: {_stockQuantity}, Requested: {Math.Abs(quantity)}");

            if (newStock > int.MaxValue - 1000) // Safety margin
                throw new BusinessRuleException("Stock quantity would exceed maximum allowed value");

            var oldStock = _stockQuantity;
            _stockQuantity = newStock;

            MarkAsUpdated(updatedBy);

            // Domain event could be raised here for stock change notifications
            // RaiseDomainEvent(new ProductStockChangedEvent(Id, oldStock, newStock, quantity, reason, updatedBy));
        }

        /// <summary>
        /// Reserves stock for a pending order or transaction.
        /// Reduces available stock without permanent removal.
        /// </summary>
        /// <param name="quantity">Quantity to reserve</param>
        /// <param name="reservedBy">User or system reserving the stock</param>
        /// <exception cref="BusinessRuleException">Thrown when insufficient stock is available</exception>
        public void ReserveStock(int quantity, string? reservedBy = null)
        {
            if (quantity <= 0)
                throw new BusinessRuleException("Reserve quantity must be positive");

            if (quantity > _stockQuantity)
                throw new BusinessRuleException($"Cannot reserve {quantity} items. Only {_stockQuantity} available");

            AdjustStock(-quantity, reservedBy, $"Reserved {quantity} items");
        }

        /// <summary>
        /// Restocks the product with new inventory.
        /// Increases stock quantity and updates inventory metadata.
        /// </summary>
        /// <param name="quantity">Quantity to add to stock</param>
        /// <param name="restockedBy">User performing the restock</param>
        /// <exception cref="BusinessRuleException">Thrown when quantity is invalid</exception>
        public void Restock(int quantity, string? restockedBy = null)
        {
            if (quantity <= 0)
                throw new BusinessRuleException("Restock quantity must be positive");

            AdjustStock(quantity, restockedBy, $"Restocked {quantity} items");
        }

        #endregion

        #region Domain Methods - Product Management

        /// <summary>
        /// Activates the product, making it available for sale.
        /// Performs business validation before activation.
        /// </summary>
        /// <param name="activatedBy">User activating the product</param>
        /// <exception cref="BusinessRuleException">Thrown when product cannot be activated</exception>
        public void Activate(string? activatedBy = null)
        {
            if (IsActive) return; // Already active

            // Validate that product can be activated
            if (string.IsNullOrWhiteSpace(_name))
                throw new BusinessRuleException("Cannot activate product without a name");

            if (_price <= 0)
                throw new BusinessRuleException("Cannot activate product with invalid price");

            IsActive = true;
            MarkAsUpdated(activatedBy);
        }

        /// <summary>
        /// Deactivates the product, removing it from sale but preserving data.
        /// Useful for temporary removal or end-of-life products.
        /// </summary>
        /// <param name="deactivatedBy">User deactivating the product</param>
        /// <param name="reason">Reason for deactivation</param>
        public void Deactivate(string? deactivatedBy = null, string? reason = null)
        {
            if (!IsActive) return; // Already inactive

            IsActive = false;
            MarkAsUpdated(deactivatedBy);

            // Domain event could be raised here for deactivation notifications
            // RaiseDomainEvent(new ProductDeactivatedEvent(Id, reason, deactivatedBy));
        }

        /// <summary>
        /// Marks the product as featured for promotional purposes.
        /// Featured products typically get priority display placement.
        /// </summary>
        /// <param name="featuredBy">User marking the product as featured</param>
        public void MarkAsFeatured(string? featuredBy = null)
        {
            if (IsFeatured) return; // Already featured

            if (!IsActive)
                throw new BusinessRuleException("Cannot feature an inactive product");

            IsFeatured = true;
            MarkAsUpdated(featuredBy);
        }

        /// <summary>
        /// Removes featured status from the product.
        /// </summary>
        /// <param name="unfeaturedBy">User removing featured status</param>
        public void RemoveFromFeatured(string? unfeaturedBy = null)
        {
            if (!IsFeatured) return; // Not featured

            IsFeatured = false;
            MarkAsUpdated(unfeaturedBy);
        }

        /// <summary>
        /// Records a product view for analytics and tracking.
        /// Updates view count and last viewed timestamp.
        /// </summary>
        public void RecordView()
        {
            ViewCount++;
            LastViewedAt = DateTime.UtcNow;
            // Note: We don't call MarkAsUpdated here to avoid updating UpdatedAt for simple views
        }

        /// <summary>
        /// Updates product tags for better categorization and search.
        /// Validates and normalizes tags before assignment.
        /// </summary>
        /// <param name="tags">New tags to assign to the product</param>
        /// <param name="updatedBy">User updating the tags</param>
        /// <exception cref="BusinessRuleException">Thrown when tags are invalid</exception>
        public void UpdateTags(IEnumerable<string> tags, string? updatedBy = null)
        {
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            var normalizedTags = tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (normalizedTags.Count > 20)
                throw new BusinessRuleException("Cannot assign more than 20 tags to a product");

            // Validate tag format
            foreach (var tag in normalizedTags)
            {
                if (tag.Length > 50)
                    throw new BusinessRuleException($"Tag '{tag}' exceeds maximum length of 50 characters");

                if (!System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[a-z0-9\-_]+$"))
                    throw new BusinessRuleException($"Tag '{tag}' contains invalid characters. Only lowercase letters, numbers, hyphens, and underscores are allowed");
            }

            Tags = normalizedTags;
            MarkAsUpdated(updatedBy);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Sets the product name with validation.
        /// </summary>
        /// <param name="name">Product name to set</param>
        /// <exception cref="BusinessRuleException">Thrown when name is invalid</exception>
        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessRuleException("Product name cannot be null or empty");

            var trimmedName = name.Trim();
            if (trimmedName.Length < 2)
                throw new BusinessRuleException("Product name must be at least 2 characters long");

            if (trimmedName.Length > 200)
                throw new BusinessRuleException("Product name cannot exceed 200 characters");

            // Validate name format
            if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedName, @"^[a-zA-Z�-�0-9\s\-\.\/\(\)\&\'\+]+$"))
                throw new BusinessRuleException("Product name contains invalid characters");

            _name = trimmedName;
        }

        /// <summary>
        /// Sets the product price with validation.
        /// </summary>
        /// <param name="price">Price to set</param>
        /// <exception cref="BusinessRuleException">Thrown when price is invalid</exception>
        private void SetPrice(decimal price)
        {
            if (price <= 0)
                throw new BusinessRuleException("Price must be greater than zero");

            if (price > 999999.99m)
                throw new BusinessRuleException("Price cannot exceed 999,999.99");

            // Validate decimal places (max 4 for storage, but business logic might want max 2)
            var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(price)[3])[2];
            if (decimalPlaces > 4)
                throw new BusinessRuleException("Price cannot have more than 4 decimal places");

            _price = price;
        }

        /// <summary>
        /// Sets the stock quantity with validation.
        /// </summary>
        /// <param name="stockQuantity">Stock quantity to set</param>
        /// <exception cref="BusinessRuleException">Thrown when stock quantity is invalid</exception>
        private void SetStockQuantity(int stockQuantity)
        {
            if (stockQuantity < 0)
                throw new BusinessRuleException("Stock quantity cannot be negative");

            _stockQuantity = stockQuantity;
        }

        /// <summary>
        /// Sets the tenant ID with validation specific to Product business rules.
        /// Overrides the base implementation to add product-specific validation.
        /// </summary>
        /// <param name="tenantId">Tenant ID to set</param>
        /// <exception cref="BusinessRuleException">Thrown when tenant ID is invalid</exception>
        public new void SetTenantId(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new BusinessRuleException("Tenant ID cannot be null or empty");

            // Call base implementation for standard validation
            base.SetTenantId(tenantId);
        }

        /// <summary>
        /// Validates the complete entity state and business rules.
        /// </summary>
        /// <exception cref="BusinessRuleException">Thrown when entity is in invalid state</exception>
        private void ValidateEntity()
        {
            if (!IsValid())
                throw new BusinessRuleException("Product entity is in an invalid state");

            if (MinimumStockLevel > MaximumStockLevel && MaximumStockLevel != int.MaxValue)
                throw new BusinessRuleException("Minimum stock level cannot be greater than maximum stock level");
        }

        #endregion

        #region Validation Override

        /// <summary>
        /// Validates the product entity state and business rules.
        /// Extends base validation with product-specific rules.
        /// </summary>
        /// <returns>True if the product is in a valid state</returns>
        public override bool IsValid()
        {
            return base.IsValid() &&
                   !string.IsNullOrWhiteSpace(_name) &&
                   _name.Length >= 2 &&
                   _name.Length <= 200 &&
                   _price > 0 &&
                   _price <= 999999.99m &&
                   _stockQuantity >= 0 &&
                   MinimumStockLevel >= 0 &&
                   MaximumStockLevel >= 0 &&
                   (MaximumStockLevel == int.MaxValue || MinimumStockLevel <= MaximumStockLevel);
        }

        #endregion

        #region ToString Override

        /// <summary>
        /// Returns a string representation of the product.
        /// Includes key product information for debugging and logging.
        /// </summary>
        /// <returns>String representation of the product</returns>
        public override string ToString()
        {
            var status = IsActive ? "ACTIVE" : "INACTIVE";
            var stockStatus = IsInStock ? $"Stock: {StockQuantity}" : "OUT OF STOCK";
            return $"Product [{status}] - {Name} (SKU: {SKU ?? "N/A"}) - Price: {Price:C} - {stockStatus} - Tenant: {TenantId}";
        }

        #endregion
    }
}
