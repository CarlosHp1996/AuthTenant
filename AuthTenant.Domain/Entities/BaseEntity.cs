using AuthTenant.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace AuthTenant.Domain.Entities
{
    /// <summary>
    /// Base entity abstract class that provides common properties and functionality
    /// for all domain entities following DDD and Clean Architecture principles.
    /// Implements multi-tenancy, auditing, and soft delete patterns.
    /// </summary>
    public abstract class BaseEntity : ITenantEntity, IAuditableEntity, IEquatable<BaseEntity>
    {
        #region Properties

        /// <summary>
        /// Unique identifier for the entity.
        /// Generated automatically using a new GUID when the entity is created.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant identifier for multi-tenant data isolation.
        /// Required for all entities to ensure proper data segregation.
        /// </summary>
        [Required]
        [StringLength(450)] // Standard length for ASP.NET Core Identity
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the entity was created.
        /// Set automatically to UTC time when the entity is instantiated.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Identifier of the user who created this entity.
        /// Can be null for system-generated entities.
        /// </summary>
        [StringLength(450)] // Standard length for user identifiers
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Timestamp when the entity was last updated.
        /// Null if the entity has never been modified since creation.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Identifier of the user who last updated this entity.
        /// Null if the entity has never been modified since creation.
        /// </summary>
        [StringLength(450)] // Standard length for user identifiers
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Soft delete flag to mark entities as deleted without physical removal.
        /// Default is false. When true, entity should be excluded from normal queries.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Timestamp when the entity was soft deleted.
        /// Null if the entity has not been deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Identifier of the user who deleted this entity.
        /// Null if the entity has not been deleted.
        /// </summary>
        [StringLength(450)]
        public string? DeletedBy { get; set; }

        #endregion

        #region IAuditableEntity Implementation

        /// <summary>
        /// Indicates if the entity has been modified since creation.
        /// </summary>
        public bool HasBeenModified => UpdatedAt.HasValue;

        /// <summary>
        /// Gets the age of the entity (time since creation).
        /// </summary>
        public TimeSpan Age => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// Gets the time since last update, or null if never updated.
        /// </summary>
        public TimeSpan? TimeSinceLastUpdate => UpdatedAt.HasValue ? DateTime.UtcNow - UpdatedAt.Value : null;

        /// <summary>
        /// Validates if the audit state is consistent and valid.
        /// </summary>
        /// <returns>True if audit state is valid</returns>
        public bool IsAuditStateValid()
        {
            // CreatedAt should not be in the future (with 1 minute tolerance for clock skew)
            if (CreatedAt > DateTime.UtcNow.AddMinutes(1))
                return false;

            // If UpdatedAt exists, it should be after or equal to CreatedAt
            if (UpdatedAt.HasValue && UpdatedAt.Value < CreatedAt)
                return false;

            // UpdatedAt should not be in the future (with 1 minute tolerance)
            if (UpdatedAt.HasValue && UpdatedAt.Value > DateTime.UtcNow.AddMinutes(1))
                return false;

            return true;
        }

        /// <summary>
        /// Gets a summary of audit information.
        /// </summary>
        /// <returns>Formatted audit summary string</returns>
        public string GetAuditSummary()
        {
            var summary = $"Created: {CreatedAt:yyyy-MM-dd HH:mm:ss} UTC";

            if (!string.IsNullOrWhiteSpace(CreatedBy))
                summary += $" by {CreatedBy}";

            if (UpdatedAt.HasValue)
            {
                summary += $" | Last Updated: {UpdatedAt.Value:yyyy-MM-dd HH:mm:ss} UTC";

                if (!string.IsNullOrWhiteSpace(UpdatedBy))
                    summary += $" by {UpdatedBy}";
            }
            else
            {
                summary += " | Never Modified";
            }

            return summary;
        }

        #endregion

        #region ITenantEntity Implementation

        /// <summary>
        /// Validates if the TenantId is in valid format.
        /// </summary>
        /// <returns>True if TenantId is valid</returns>
        public bool HasValidTenantId()
        {
            return !string.IsNullOrWhiteSpace(TenantId) &&
                   TenantId.Length >= 1 &&
                   TenantId.Length <= 450;
        }

        /// <summary>
        /// Sets the tenant ID with validation.
        /// </summary>
        /// <param name="tenantId">The tenant ID to set</param>
        /// <exception cref="ArgumentException">When tenantId is invalid</exception>
        public void SetTenantId(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("TenantId cannot be null or empty", nameof(tenantId));

            if (tenantId.Length > 450)
                throw new ArgumentException("TenantId cannot be longer than 450 characters", nameof(tenantId));

            TenantId = tenantId.Trim();
        }

        /// <summary>
        /// Gets tenant diagnostic information.
        /// </summary>
        /// <returns>Tenant information string</returns>
        public string GetTenantInfo()
        {
            if (string.IsNullOrWhiteSpace(TenantId))
                return "No Tenant Assigned";

            return $"Tenant: {TenantId} (Length: {TenantId.Length}, Valid: {HasValidTenantId()})";
        }

        #endregion

        #region Domain Methods

        /// <summary>
        /// Marks the entity as updated with the current timestamp.
        /// Should be called whenever entity properties are modified.
        /// </summary>
        /// <param name="updatedBy">Optional identifier of the user making the update</param>
        public virtual void MarkAsUpdated(string? updatedBy = null)
        {
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        /// <summary>
        /// Performs soft delete on the entity.
        /// Sets IsDeleted to true and records deletion metadata.
        /// </summary>
        /// <param name="deletedBy">Optional identifier of the user performing the deletion</param>
        public virtual void MarkAsDeleted(string? deletedBy = null)
        {
            if (IsDeleted) return; // Already deleted

            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
            MarkAsUpdated(deletedBy);
        }

        /// <summary>
        /// Restores a soft-deleted entity.
        /// Sets IsDeleted to false and clears deletion metadata.
        /// </summary>
        /// <param name="restoredBy">Optional identifier of the user performing the restoration</param>
        public virtual void Restore(string? restoredBy = null)
        {
            if (!IsDeleted) return; // Not deleted

            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
            MarkAsUpdated(restoredBy);
        }

        /// <summary>
        /// Validates if the entity belongs to the specified tenant.
        /// Critical for multi-tenant data security.
        /// </summary>
        /// <param name="tenantId">The tenant ID to validate against</param>
        /// <returns>True if the entity belongs to the specified tenant</returns>
        /// <exception cref="ArgumentException">Thrown when tenantId is null or empty</exception>
        public virtual bool BelongsToTenant(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));

            return string.Equals(TenantId, tenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates the entity state and business rules.
        /// Override in derived classes to implement specific validation logic.
        /// </summary>
        /// <returns>True if the entity is in a valid state</returns>
        public virtual bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(TenantId) &&
                   Id != Guid.Empty &&
                   CreatedAt <= DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the entity's age (time since creation).
        /// </summary>
        /// <returns>TimeSpan representing the age of the entity</returns>
        [Obsolete("Use Age property instead")]
        public TimeSpan GetAge()
        {
            return Age;
        }

        /// <summary>
        /// Gets the time since last update.
        /// Returns null if the entity has never been updated.
        /// </summary>
        /// <returns>TimeSpan since last update, or null if never updated</returns>
        [Obsolete("Use TimeSinceLastUpdate property instead")]
        public TimeSpan? GetTimeSinceLastUpdate()
        {
            return TimeSinceLastUpdate;
        }

        #endregion

        #region Equality Implementation

        /// <summary>
        /// Determines whether the specified BaseEntity is equal to the current BaseEntity.
        /// Entities are considered equal if they have the same Id and TenantId.
        /// </summary>
        /// <param name="other">The BaseEntity to compare with the current BaseEntity</param>
        /// <returns>True if the specified BaseEntity is equal to the current BaseEntity</returns>
        public virtual bool Equals(BaseEntity? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (GetType() != other.GetType()) return false;

            return Id.Equals(other.Id) &&
                   string.Equals(TenantId, other.TenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current BaseEntity.
        /// </summary>
        /// <param name="obj">The object to compare with the current BaseEntity</param>
        /// <returns>True if the specified object is equal to the current BaseEntity</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as BaseEntity);
        }

        /// <summary>
        /// Generates a hash code for the current BaseEntity based on Id and TenantId.
        /// </summary>
        /// <returns>A hash code for the current BaseEntity</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, TenantId.ToLowerInvariant());
        }

        #endregion

        #region Operators

        /// <summary>
        /// Determines whether two BaseEntity instances are equal.
        /// </summary>
        /// <param name="left">The first BaseEntity to compare</param>
        /// <param name="right">The second BaseEntity to compare</param>
        /// <returns>True if the BaseEntity instances are equal</returns>
        public static bool operator ==(BaseEntity? left, BaseEntity? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two BaseEntity instances are not equal.
        /// </summary>
        /// <param name="left">The first BaseEntity to compare</param>
        /// <param name="right">The second BaseEntity to compare</param>
        /// <returns>True if the BaseEntity instances are not equal</returns>
        public static bool operator !=(BaseEntity? left, BaseEntity? right)
        {
            return !Equals(left, right);
        }

        #endregion

        #region ToString Override

        /// <summary>
        /// Returns a string representation of the entity.
        /// Includes type name, Id, and basic metadata.
        /// </summary>
        /// <returns>String representation of the entity</returns>
        public override string ToString()
        {
            var typeName = GetType().Name;
            var status = IsDeleted ? "[DELETED]" : "[ACTIVE]";
            return $"{typeName} {status} - Id: {Id}, Tenant: {TenantId}, Created: {CreatedAt:yyyy-MM-dd HH:mm:ss}";
        }

        #endregion
    }
}
