using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AuthTenant.Domain.Entities
{
    /// <summary>
    /// Tenant entity representing an isolated instance in a multi-tenant system.
    /// Provides complete data and operational isolation between different organizations.
    /// Implements rich domain model with business logic and configuration management.
    /// </summary>
    public class Tenant
    {
        #region Private Fields

        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _displayName = string.Empty;
        private Dictionary<string, string> _settings = new();

        #endregion

        #region Properties

        /// <summary>
        /// Unique identifier for the tenant.
        /// Used throughout the system for data isolation and access control.
        /// </summary>
        [Key]
        [Required(ErrorMessage = "Tenant ID is required")]
        [StringLength(450, MinimumLength = 3, ErrorMessage = "Tenant ID must be between 3 and 450 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Tenant ID can only contain letters, numbers, hyphens, and underscores")]
        public string Id
        {
            get => _id;
            set => SetId(value);
        }

        /// <summary>
        /// Internal name of the tenant for system identification.
        /// Must be unique and follow naming conventions for technical use.
        /// </summary>
        [Required(ErrorMessage = "Tenant name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tenant name must be between 3 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\-_\s]+$", ErrorMessage = "Tenant name contains invalid characters")]
        public string Name
        {
            get => _name;
            set => SetName(value);
        }

        /// <summary>
        /// Display name of the tenant for user interfaces and presentations.
        /// More user-friendly version of the tenant name with formatting allowed.
        /// </summary>
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 200 characters")]
        public string DisplayName
        {
            get => _displayName;
            set => SetDisplayName(value);
        }

        /// <summary>
        /// Optional description of the tenant organization or purpose.
        /// Provides additional context about the tenant for administrative purposes.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Indicates whether the tenant is active and operational.
        /// Inactive tenants cannot access the system or perform any operations.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Timestamp when the tenant was created and registered in the system.
        /// Used for billing, analytics, and lifecycle management.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the tenant configuration was last updated.
        /// Helps track configuration changes and maintenance activities.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Optional database connection string for tenant-specific database isolation.
        /// Supports advanced multi-tenancy scenarios with separate databases.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Connection string cannot exceed 2000 characters")]
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Tenant-specific configuration settings stored as key-value pairs.
        /// Allows flexible configuration without schema changes.
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> Settings
        {
            get => _settings;
            set => SetSettings(value);
        }

        /// <summary>
        /// JSON serialized settings for database storage.
        /// Internal property used by Entity Framework for persistence.
        /// </summary>
        [Column("Settings")]
        [StringLength(4000, ErrorMessage = "Settings JSON cannot exceed 4000 characters")]
        public string? SettingsJson
        {
            get => _settings.Count > 0 ? JsonSerializer.Serialize(_settings) : null;
            set => _settings = !string.IsNullOrWhiteSpace(value) ?
                JsonSerializer.Deserialize<Dictionary<string, string>>(value) ?? new Dictionary<string, string>() :
                new Dictionary<string, string>();
        }

        /// <summary>
        /// Maximum number of users allowed for this tenant.
        /// Used for subscription management and resource planning.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Maximum users must be at least 1")]
        public int MaxUsers { get; set; } = 100;

        /// <summary>
        /// Maximum storage quota for the tenant in bytes.
        /// Used for resource management and billing purposes.
        /// </summary>
        [Range(0, long.MaxValue, ErrorMessage = "Storage quota cannot be negative")]
        public long StorageQuotaBytes { get; set; } = 1073741824; // 1 GB default

        /// <summary>
        /// Current storage usage in bytes.
        /// Tracked to enforce quotas and provide usage analytics.
        /// </summary>
        [Range(0, long.MaxValue, ErrorMessage = "Storage usage cannot be negative")]
        public long StorageUsedBytes { get; set; } = 0;

        /// <summary>
        /// Subscription plan or tier for the tenant.
        /// Determines available features and resource limits.
        /// </summary>
        [StringLength(50, ErrorMessage = "Subscription plan cannot exceed 50 characters")]
        public string? SubscriptionPlan { get; set; } = "Basic";

        /// <summary>
        /// Subscription expiration date for billing and access control.
        /// After expiration, tenant access may be restricted.
        /// </summary>
        public DateTime? SubscriptionExpiresAt { get; set; }

        /// <summary>
        /// Primary contact email for tenant administration and notifications.
        /// Used for system communications and billing correspondence.
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid contact email format")]
        [StringLength(254, ErrorMessage = "Contact email cannot exceed 254 characters")]
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Primary contact phone number for tenant support.
        /// Optional but recommended for critical communications.
        /// </summary>
        [Phone(ErrorMessage = "Invalid contact phone format")]
        [StringLength(20, ErrorMessage = "Contact phone cannot exceed 20 characters")]
        public string? ContactPhone { get; set; }

        /// <summary>
        /// Tenant's physical or billing address.
        /// Important for compliance and billing requirements.
        /// </summary>
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        /// <summary>
        /// Country code for the tenant's location.
        /// Used for compliance, taxation, and localization.
        /// </summary>
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be exactly 2 characters")]
        [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "Country code must be in ISO 3166-1 alpha-2 format")]
        public string? CountryCode { get; set; }

        /// <summary>
        /// Timezone identifier for the tenant's primary location.
        /// Used for scheduling and time-based operations.
        /// </summary>
        [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters")]
        public string? TimeZone { get; set; }

        /// <summary>
        /// Primary language/culture for the tenant.
        /// Determines default localization and formatting.
        /// </summary>
        [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
        [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$", ErrorMessage = "Invalid language code format")]
        public string? Language { get; set; } = "en-US";

        /// <summary>
        /// Custom domain name for tenant-specific branding.
        /// Allows white-label deployments and branded access.
        /// </summary>
        [StringLength(253, ErrorMessage = "Custom domain cannot exceed 253 characters")]
        [RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$",
            ErrorMessage = "Invalid domain format")]
        public string? CustomDomain { get; set; }

        /// <summary>
        /// Indicates if the tenant uses single sign-on authentication.
        /// Enables integration with external identity providers.
        /// </summary>
        public bool UsesSingleSignOn { get; set; } = false;

        /// <summary>
        /// Single sign-on provider identifier (e.g., "azure-ad", "okta").
        /// Specifies which SSO provider to use for authentication.
        /// </summary>
        [StringLength(50, ErrorMessage = "SSO provider cannot exceed 50 characters")]
        public string? SSOProvider { get; set; }

        /// <summary>
        /// Indicates if the tenant is marked for deletion (soft delete).
        /// Preserves data integrity while hiding the tenant from normal operations.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Timestamp when the tenant was marked for deletion.
        /// Used for cleanup processes and audit trails.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Identifier of the user or system that deleted the tenant.
        /// Important for audit and accountability purposes.
        /// </summary>
        [StringLength(450, ErrorMessage = "Deleted by identifier cannot exceed 450 characters")]
        public string? DeletedBy { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Collection of users belonging to this tenant.
        /// Lazy loaded to avoid unnecessary database queries.
        /// </summary>
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the normalized tenant ID in lowercase for consistent comparisons.
        /// </summary>
        [NotMapped]
        public string NormalizedId => Id.ToLowerInvariant();

        /// <summary>
        /// Gets the normalized name in lowercase for consistent comparisons.
        /// </summary>
        [NotMapped]
        public string NormalizedName => Name.ToLowerInvariant();

        /// <summary>
        /// Indicates if the tenant's subscription is currently active and valid.
        /// Considers both subscription status and expiration date.
        /// </summary>
        [NotMapped]
        public bool HasActiveSubscription
        {
            get
            {
                if (!SubscriptionExpiresAt.HasValue) return true; // No expiration set
                return DateTime.UtcNow < SubscriptionExpiresAt.Value;
            }
        }

        /// <summary>
        /// Calculates the percentage of storage quota currently used.
        /// Returns 0 if no quota is set.
        /// </summary>
        [NotMapped]
        public double StorageUsagePercentage
        {
            get
            {
                if (StorageQuotaBytes == 0) return 0;
                return Math.Min(100.0, (double)StorageUsedBytes / StorageQuotaBytes * 100.0);
            }
        }

        /// <summary>
        /// Gets the remaining storage quota in bytes.
        /// Returns 0 if quota is exceeded.
        /// </summary>
        [NotMapped]
        public long RemainingStorageBytes => Math.Max(0, StorageQuotaBytes - StorageUsedBytes);

        /// <summary>
        /// Indicates if the tenant is approaching storage quota limit (>80%).
        /// Useful for proactive storage management and notifications.
        /// </summary>
        [NotMapped]
        public bool IsApproachingStorageLimit => StorageUsagePercentage > 80.0;

        /// <summary>
        /// Indicates if the tenant has exceeded storage quota limit.
        /// Should trigger restrictions on new storage operations.
        /// </summary>
        [NotMapped]
        public bool HasExceededStorageLimit => StorageUsedBytes > StorageQuotaBytes;

        /// <summary>
        /// Gets the tenant's age (time since creation).
        /// Useful for analytics and lifecycle management.
        /// </summary>
        [NotMapped]
        public TimeSpan Age => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// Gets the time until subscription expiration.
        /// Returns null if no expiration is set.
        /// </summary>
        [NotMapped]
        public TimeSpan? TimeUntilSubscriptionExpiration
        {
            get
            {
                if (!SubscriptionExpiresAt.HasValue) return null;
                var timeRemaining = SubscriptionExpiresAt.Value - DateTime.UtcNow;
                return timeRemaining > TimeSpan.Zero ? timeRemaining : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Indicates if the subscription is expiring soon (within 30 days).
        /// Useful for renewal notifications and billing alerts.
        /// </summary>
        [NotMapped]
        public bool IsSubscriptionExpiringSoon
        {
            get
            {
                var timeUntilExpiration = TimeUntilSubscriptionExpiration;
                return timeUntilExpiration.HasValue && timeUntilExpiration.Value <= TimeSpan.FromDays(30);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for Entity Framework and serialization.
        /// </summary>
        public Tenant() { }

        /// <summary>
        /// Creates a new tenant with required parameters.
        /// Validates business rules and initializes the entity properly.
        /// </summary>
        /// <param name="id">Unique tenant identifier</param>
        /// <param name="name">Tenant name for system use</param>
        /// <param name="displayName">Tenant display name for UI</param>
        /// <param name="contactEmail">Primary contact email</param>
        /// <exception cref="ArgumentException">Thrown when required parameters are invalid</exception>
        public Tenant(string id, string name, string displayName, string? contactEmail = null)
        {
            SetId(id);
            SetName(name);
            SetDisplayName(displayName);
            ContactEmail = contactEmail;

            // Initialize with safe defaults
            IsActive = true;
            Settings = new Dictionary<string, string>();
            MaxUsers = 100;
            StorageQuotaBytes = 1073741824; // 1 GB
            StorageUsedBytes = 0;
            SubscriptionPlan = "Basic";
            Language = "en-US";
            UsesSingleSignOn = false;

            ValidateTenant();
        }

        #endregion

        #region Domain Methods - Tenant Management

        /// <summary>
        /// Activates the tenant, enabling access to all system functionality.
        /// Performs business validation before activation.
        /// </summary>
        /// <param name="activatedBy">User or system activating the tenant</param>
        /// <exception cref="InvalidOperationException">Thrown when tenant cannot be activated</exception>
        public void Activate(string? activatedBy = null)
        {
            if (IsActive) return; // Already active

            if (IsDeleted)
                throw new InvalidOperationException("Cannot activate a deleted tenant");

            if (!HasActiveSubscription)
                throw new InvalidOperationException("Cannot activate tenant with expired subscription");

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the tenant, preventing access to system functionality.
        /// Tenant data is preserved but no operations can be performed.
        /// </summary>
        /// <param name="deactivatedBy">User or system deactivating the tenant</param>
        /// <param name="reason">Reason for deactivation</param>
        public void Deactivate(string? deactivatedBy = null, string? reason = null)
        {
            if (!IsActive) return; // Already inactive

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;

            // Add reason to settings if provided
            if (!string.IsNullOrWhiteSpace(reason))
            {
                Settings["DeactivationReason"] = reason;
                Settings["DeactivatedAt"] = DateTime.UtcNow.ToString("O");
                Settings["DeactivatedBy"] = deactivatedBy ?? "System";
            }
        }

        /// <summary>
        /// Marks the tenant as deleted using soft delete pattern.
        /// Preserves data integrity while hiding the tenant from normal operations.
        /// </summary>
        /// <param name="deletedBy">User or system performing the deletion</param>
        public void MarkAsDeleted(string? deletedBy = null)
        {
            if (IsDeleted) return; // Already deleted

            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Restores a soft-deleted tenant.
        /// Removes deletion markers but does not automatically activate the tenant.
        /// </summary>
        /// <param name="restoredBy">User or system performing the restoration</param>
        public void Restore(string? restoredBy = null)
        {
            if (!IsDeleted) return; // Not deleted

            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
            UpdatedAt = DateTime.UtcNow;

            // Note: Tenant is not automatically activated after restoration
        }

        #endregion

        #region Domain Methods - Subscription Management

        /// <summary>
        /// Updates the tenant's subscription plan and settings.
        /// Validates subscription changes and applies new limits.
        /// </summary>
        /// <param name="plan">New subscription plan</param>
        /// <param name="expiresAt">New subscription expiration date</param>
        /// <param name="maxUsers">New maximum user limit</param>
        /// <param name="storageQuotaBytes">New storage quota in bytes</param>
        /// <param name="updatedBy">User making the subscription change</param>
        /// <exception cref="ArgumentException">Thrown when subscription parameters are invalid</exception>
        public void UpdateSubscription(string plan, DateTime? expiresAt = null, int? maxUsers = null,
            long? storageQuotaBytes = null, string? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(plan))
                throw new ArgumentException("Subscription plan cannot be null or empty", nameof(plan));

            SubscriptionPlan = plan.Trim();
            SubscriptionExpiresAt = expiresAt;

            if (maxUsers.HasValue)
            {
                if (maxUsers.Value < 1)
                    throw new ArgumentException("Maximum users must be at least 1", nameof(maxUsers));
                MaxUsers = maxUsers.Value;
            }

            if (storageQuotaBytes.HasValue)
            {
                if (storageQuotaBytes.Value < 0)
                    throw new ArgumentException("Storage quota cannot be negative", nameof(storageQuotaBytes));
                StorageQuotaBytes = storageQuotaBytes.Value;
            }

            UpdatedAt = DateTime.UtcNow;

            // Record subscription change in settings
            Settings["LastSubscriptionChange"] = DateTime.UtcNow.ToString("O");
            Settings["LastSubscriptionChangedBy"] = updatedBy ?? "System";
        }

        /// <summary>
        /// Extends the subscription expiration date.
        /// Useful for renewals and subscription extensions.
        /// </summary>
        /// <param name="extensionPeriod">Time period to extend the subscription</param>
        /// <param name="extendedBy">User performing the extension</param>
        public void ExtendSubscription(TimeSpan extensionPeriod, string? extendedBy = null)
        {
            var currentExpiration = SubscriptionExpiresAt ?? DateTime.UtcNow;
            var newExpiration = currentExpiration.Add(extensionPeriod);

            SubscriptionExpiresAt = newExpiration;
            UpdatedAt = DateTime.UtcNow;

            // Record extension in settings
            Settings["LastSubscriptionExtension"] = DateTime.UtcNow.ToString("O");
            Settings["LastExtensionBy"] = extendedBy ?? "System";
            Settings["ExtensionPeriod"] = extensionPeriod.ToString();
        }

        #endregion

        #region Domain Methods - Storage Management

        /// <summary>
        /// Updates the tenant's storage usage statistics.
        /// Should be called when files are added, modified, or deleted.
        /// </summary>
        /// <param name="deltaBytes">Change in storage usage (positive for increase, negative for decrease)</param>
        /// <exception cref="InvalidOperationException">Thrown when operation would exceed quota</exception>
        public void UpdateStorageUsage(long deltaBytes)
        {
            var newUsage = StorageUsedBytes + deltaBytes;

            if (newUsage < 0)
                newUsage = 0; // Cannot have negative usage

            // Check quota only for increases
            if (deltaBytes > 0 && newUsage > StorageQuotaBytes)
                throw new InvalidOperationException($"Storage operation would exceed quota. Available: {RemainingStorageBytes} bytes, Requested: {deltaBytes} bytes");

            StorageUsedBytes = newUsage;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Resets storage usage statistics.
        /// Useful for quota resets and cleanup operations.
        /// </summary>
        /// <param name="resetBy">User or system performing the reset</param>
        public void ResetStorageUsage(string? resetBy = null)
        {
            StorageUsedBytes = 0;
            UpdatedAt = DateTime.UtcNow;

            // Record reset in settings
            Settings["LastStorageReset"] = DateTime.UtcNow.ToString("O");
            Settings["ResetBy"] = resetBy ?? "System";
        }

        #endregion

        #region Domain Methods - Configuration Management

        /// <summary>
        /// Gets a configuration setting value by key.
        /// Returns default value if the setting is not found.
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="defaultValue">Default value to return if setting is not found</param>
        /// <returns>Setting value or default value</returns>
        public string? GetSetting(string key, string? defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Setting key cannot be null or empty", nameof(key));

            return Settings.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Sets a configuration setting value.
        /// Creates new setting if key doesn't exist, updates if it does.
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="value">Setting value</param>
        /// <param name="updatedBy">User making the configuration change</param>
        /// <exception cref="ArgumentException">Thrown when key is invalid</exception>
        public void SetSetting(string key, string value, string? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Setting key cannot be null or empty", nameof(key));

            Settings[key] = value ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;

            // Record who made the change
            Settings[$"{key}_LastModified"] = DateTime.UtcNow.ToString("O");
            if (!string.IsNullOrWhiteSpace(updatedBy))
                Settings[$"{key}_LastModifiedBy"] = updatedBy;
        }

        /// <summary>
        /// Removes a configuration setting.
        /// Does nothing if the setting doesn't exist.
        /// </summary>
        /// <param name="key">Setting key to remove</param>
        /// <param name="removedBy">User removing the setting</param>
        public void RemoveSetting(string key, string? removedBy = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Setting key cannot be null or empty", nameof(key));

            if (Settings.Remove(key))
            {
                UpdatedAt = DateTime.UtcNow;

                // Also remove metadata
                Settings.Remove($"{key}_LastModified");
                Settings.Remove($"{key}_LastModifiedBy");
            }
        }

        /// <summary>
        /// Updates multiple configuration settings atomically.
        /// Validates all settings before applying any changes.
        /// </summary>
        /// <param name="newSettings">Dictionary of settings to update</param>
        /// <param name="updatedBy">User making the configuration changes</param>
        /// <exception cref="ArgumentException">Thrown when settings are invalid</exception>
        public void UpdateSettings(Dictionary<string, string> newSettings, string? updatedBy = null)
        {
            if (newSettings == null)
                throw new ArgumentNullException(nameof(newSettings));

            // Validate all keys first
            foreach (var key in newSettings.Keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException("Setting key cannot be null or empty");
            }

            // Apply all changes
            foreach (var setting in newSettings)
            {
                SetSetting(setting.Key, setting.Value, updatedBy);
            }
        }

        #endregion

        #region Domain Methods - Localization

        /// <summary>
        /// Updates the tenant's localization settings.
        /// Validates language and timezone before applying changes.
        /// </summary>
        /// <param name="language">Language code (e.g., "en-US", "pt-BR")</param>
        /// <param name="timeZone">Timezone identifier</param>
        /// <param name="countryCode">ISO 3166-1 alpha-2 country code</param>
        /// <param name="updatedBy">User making the localization changes</param>
        /// <exception cref="ArgumentException">Thrown when localization parameters are invalid</exception>
        public void UpdateLocalization(string? language = null, string? timeZone = null,
            string? countryCode = null, string? updatedBy = null)
        {
            if (!string.IsNullOrWhiteSpace(language))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(language, @"^[a-z]{2}(-[A-Z]{2})?$"))
                    throw new ArgumentException("Invalid language code format", nameof(language));
                Language = language;
            }

            if (!string.IsNullOrWhiteSpace(timeZone))
            {
                try
                {
                    TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                    TimeZone = timeZone;
                }
                catch (TimeZoneNotFoundException)
                {
                    throw new ArgumentException("Invalid timezone identifier", nameof(timeZone));
                }
            }

            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(countryCode, @"^[A-Z]{2}$"))
                    throw new ArgumentException("Country code must be in ISO 3166-1 alpha-2 format", nameof(countryCode));
                CountryCode = countryCode;
            }

            UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Sets the tenant ID with validation.
        /// </summary>
        /// <param name="id">Tenant ID to set</param>
        /// <exception cref="ArgumentException">Thrown when ID is invalid</exception>
        private void SetId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(id));

            var trimmedId = id.Trim();
            if (trimmedId.Length < 3)
                throw new ArgumentException("Tenant ID must be at least 3 characters long", nameof(id));

            if (trimmedId.Length > 450)
                throw new ArgumentException("Tenant ID cannot exceed 450 characters", nameof(id));

            if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedId, @"^[a-zA-Z0-9\-_]+$"))
                throw new ArgumentException("Tenant ID can only contain letters, numbers, hyphens, and underscores", nameof(id));

            _id = trimmedId;
        }

        /// <summary>
        /// Sets the tenant name with validation.
        /// </summary>
        /// <param name="name">Tenant name to set</param>
        /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tenant name cannot be null or empty", nameof(name));

            var trimmedName = name.Trim();
            if (trimmedName.Length < 3)
                throw new ArgumentException("Tenant name must be at least 3 characters long", nameof(name));

            if (trimmedName.Length > 100)
                throw new ArgumentException("Tenant name cannot exceed 100 characters", nameof(name));

            if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedName, @"^[a-zA-Z0-9\-_\s]+$"))
                throw new ArgumentException("Tenant name contains invalid characters", nameof(name));

            _name = trimmedName;
        }

        /// <summary>
        /// Sets the tenant display name with validation.
        /// </summary>
        /// <param name="displayName">Display name to set</param>
        /// <exception cref="ArgumentException">Thrown when display name is invalid</exception>
        private void SetDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be null or empty", nameof(displayName));

            var trimmedDisplayName = displayName.Trim();
            if (trimmedDisplayName.Length < 2)
                throw new ArgumentException("Display name must be at least 2 characters long", nameof(displayName));

            if (trimmedDisplayName.Length > 200)
                throw new ArgumentException("Display name cannot exceed 200 characters", nameof(displayName));

            _displayName = trimmedDisplayName;
        }

        /// <summary>
        /// Sets the settings dictionary with validation.
        /// </summary>
        /// <param name="settings">Settings dictionary to set</param>
        /// <exception cref="ArgumentNullException">Thrown when settings is null</exception>
        private void SetSettings(Dictionary<string, string> settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Validates the complete tenant entity state and business rules.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when tenant is in invalid state</exception>
        private void ValidateTenant()
        {
            if (string.IsNullOrWhiteSpace(_id))
                throw new InvalidOperationException("Tenant must have a valid ID");

            if (string.IsNullOrWhiteSpace(_name))
                throw new InvalidOperationException("Tenant must have a valid name");

            if (string.IsNullOrWhiteSpace(_displayName))
                throw new InvalidOperationException("Tenant must have a valid display name");

            if (MaxUsers < 1)
                throw new InvalidOperationException("Tenant must allow at least 1 user");

            if (StorageQuotaBytes < 0)
                throw new InvalidOperationException("Storage quota cannot be negative");

            if (StorageUsedBytes < 0)
                throw new InvalidOperationException("Storage usage cannot be negative");
        }

        #endregion

        #region ToString Override

        /// <summary>
        /// Returns a string representation of the tenant.
        /// Includes key tenant information for debugging and logging.
        /// </summary>
        /// <returns>String representation of the tenant</returns>
        public override string ToString()
        {
            var status = IsActive ? "ACTIVE" : "INACTIVE";
            if (IsDeleted) status = "DELETED";

            var subscription = HasActiveSubscription ? $"[{SubscriptionPlan}]" : "[EXPIRED]";
            var storage = $"Storage: {StorageUsagePercentage:F1}%";

            return $"Tenant [{status}] {subscription} - {DisplayName} ({Id}) - Users: {MaxUsers} - {storage} - Created: {CreatedAt:yyyy-MM-dd}";
        }

        #endregion
    }
}
