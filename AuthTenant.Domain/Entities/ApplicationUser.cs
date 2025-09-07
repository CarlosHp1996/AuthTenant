using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Identity;

namespace AuthTenant.Domain.Entities
{
    /// <summary>
    /// Application user entity extending ASP.NET Core Identity with multi-tenancy and custom properties.
    /// Represents a user in the system with tenant isolation and additional business information.
    /// Follows DDD principles with rich domain model and encapsulated business logic.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        #region Private Fields

        private string _tenantId = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// Tenant identifier for multi-tenant data isolation.
        /// Required for all users to ensure proper data segregation.
        /// </summary>
        [Required(ErrorMessage = "Tenant ID is required")]
        [StringLength(450, ErrorMessage = "Tenant ID cannot exceed 450 characters")]
        public string TenantId
        {
            get => _tenantId;
            set => SetTenantId(value);
        }

        /// <summary>
        /// User's first name with validation and formatting.
        /// Must be between 2 and 50 characters and contain only valid characters.
        /// </summary>
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s\-'\.]+$", ErrorMessage = "First name contains invalid characters")]
        public string FirstName
        {
            get => _firstName;
            set => SetFirstName(value);
        }

        /// <summary>
        /// User's last name with validation and formatting.
        /// Must be between 2 and 50 characters and contain only valid characters.
        /// </summary>
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s\-'\.]+$", ErrorMessage = "Last name contains invalid characters")]
        public string LastName
        {
            get => _lastName;
            set => SetLastName(value);
        }

        /// <summary>
        /// Indicates whether the user account is active and can perform operations.
        /// Inactive users cannot log in or perform any actions in the system.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Timestamp when the user account was created.
        /// Set automatically to UTC time when the user is registered.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp of the user's last successful login.
        /// Used for security monitoring and user activity tracking.
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Timestamp of the user's last activity in the system.
        /// Updated on each authenticated request for session management.
        /// </summary>
        public DateTime? LastActivity { get; set; }

        /// <summary>
        /// Number of failed login attempts since last successful login.
        /// Used for security purposes and account lockout policies.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Failed login attempts cannot be negative")]
        public int FailedLoginAttempts { get; set; } = 0;

        /// <summary>
        /// Timestamp when the account was locked due to failed login attempts.
        /// Null if the account is not locked.
        /// </summary>
        public DateTime? LockedAt { get; set; }

        /// <summary>
        /// Timestamp when the account lock expires and user can attempt login again.
        /// Null if the account is not locked or lock doesn't expire automatically.
        /// </summary>
        public DateTime? LockoutExpiresAt { get; set; }

        /// <summary>
        /// User's preferred language/culture for localization.
        /// Defaults to system default if not specified.
        /// </summary>
        [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
        [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$", ErrorMessage = "Invalid language code format")]
        public string? PreferredLanguage { get; set; }

        /// <summary>
        /// User's timezone for date/time display and scheduling.
        /// Important for multi-timezone applications.
        /// </summary>
        [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters")]
        public string? TimeZone { get; set; }

        /// <summary>
        /// User's job title or role within their organization.
        /// Optional field for organizational context.
        /// </summary>
        [StringLength(100, ErrorMessage = "Job title cannot exceed 100 characters")]
        public string? JobTitle { get; set; }

        /// <summary>
        /// User's department within their organization.
        /// Optional field for organizational structure.
        /// </summary>
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        public string? Department { get; set; }

        /// <summary>
        /// Indicates if the user has completed the initial setup/onboarding process.
        /// Used to guide new users through required configuration steps.
        /// </summary>
        public bool HasCompletedSetup { get; set; } = false;

        /// <summary>
        /// Indicates if the user account is marked for deletion.
        /// Soft delete pattern to maintain referential integrity.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Timestamp when the user account was marked for deletion.
        /// Null if the account has not been deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Identifier of the user or system that deleted this account.
        /// Null if the account has not been deleted.
        /// </summary>
        [StringLength(450)]
        public string? DeletedBy { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Navigation property to the tenant this user belongs to.
        /// Lazy loaded to avoid unnecessary database queries.
        /// </summary>
        public virtual Tenant? Tenant { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// User's full name combining first and last names.
        /// Properly formatted with single space between names.
        /// </summary>
        [NotMapped]
        public string FullName => $"{FirstName?.Trim()} {LastName?.Trim()}".Trim();

        /// <summary>
        /// User's display name for UI purposes.
        /// Falls back to username if full name is not available.
        /// </summary>
        [NotMapped]
        public string DisplayName
        {
            get
            {
                var fullName = FullName;
                return !string.IsNullOrWhiteSpace(fullName) ? fullName : UserName ?? Email ?? "Unknown User";
            }
        }

        /// <summary>
        /// User's initials for avatar display.
        /// Takes first character of first and last names.
        /// </summary>
        [NotMapped]
        public string Initials
        {
            get
            {
                var firstInitial = !string.IsNullOrWhiteSpace(FirstName) ? FirstName[0].ToString().ToUpperInvariant() : "";
                var lastInitial = !string.IsNullOrWhiteSpace(LastName) ? LastName[0].ToString().ToUpperInvariant() : "";
                return $"{firstInitial}{lastInitial}";
            }
        }

        /// <summary>
        /// Indicates if the user account is currently locked out.
        /// Considers both explicit lockout and automatic lockout expiration.
        /// </summary>
        [NotMapped]
        public bool IsLockedOut
        {
            get
            {
                if (!LockedAt.HasValue) return false;
                if (!LockoutExpiresAt.HasValue) return true; // Permanently locked
                return DateTime.UtcNow < LockoutExpiresAt.Value;
            }
        }

        /// <summary>
        /// Gets the user's age based on their account creation date.
        /// Useful for analytics and user lifecycle management.
        /// </summary>
        [NotMapped]
        public TimeSpan AccountAge => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// Gets the time since the user's last login.
        /// Returns null if the user has never logged in.
        /// </summary>
        [NotMapped]
        public TimeSpan? TimeSinceLastLogin => LastLogin.HasValue ? DateTime.UtcNow - LastLogin.Value : null;

        /// <summary>
        /// Gets the time since the user's last activity.
        /// Returns null if no activity has been recorded.
        /// </summary>
        [NotMapped]
        public TimeSpan? TimeSinceLastActivity => LastActivity.HasValue ? DateTime.UtcNow - LastActivity.Value : null;

        /// <summary>
        /// Indicates if the user is considered active based on recent login.
        /// Active is defined as logged in within the last 30 days.
        /// </summary>
        [NotMapped]
        public bool IsRecentlyActive
        {
            get
            {
                if (!LastLogin.HasValue) return false;
                return DateTime.UtcNow - LastLogin.Value <= TimeSpan.FromDays(30);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for Entity Framework and Identity framework.
        /// </summary>
        public ApplicationUser() { }

        /// <summary>
        /// Creates a new application user with required parameters.
        /// Validates business rules and initializes the entity properly.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="firstName">User's first name</param>
        /// <param name="lastName">User's last name</param>
        /// <param name="tenantId">Tenant identifier</param>
        /// <exception cref="ArgumentException">Thrown when required parameters are invalid</exception>
        public ApplicationUser(string email, string firstName, string lastName, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            Email = email.Trim().ToLowerInvariant();
            UserName = Email; // Use email as username by default
            SetFirstName(firstName);
            SetLastName(lastName);
            SetTenantId(tenantId);

            // Initialize with safe defaults
            IsActive = true;
            EmailConfirmed = false;
            PhoneNumberConfirmed = false;
            TwoFactorEnabled = false;
            LockoutEnabled = true;
            HasCompletedSetup = false;
            FailedLoginAttempts = 0;

            ValidateUser();
        }

        #endregion

        #region Domain Methods - Account Management

        /// <summary>
        /// Activates the user account, allowing login and system access.
        /// Performs business validation before activation.
        /// </summary>
        /// <param name="activatedBy">User or system activating the account</param>
        /// <exception cref="InvalidOperationException">Thrown when account cannot be activated</exception>
        public void Activate(string? activatedBy = null)
        {
            if (IsActive) return; // Already active

            if (IsDeleted)
                throw new InvalidOperationException("Cannot activate a deleted user account");

            if (!EmailConfirmed)
                throw new InvalidOperationException("Cannot activate account with unconfirmed email");

            IsActive = true;
            FailedLoginAttempts = 0;
            LockedAt = null;
            LockoutExpiresAt = null;
        }

        /// <summary>
        /// Deactivates the user account, preventing login and system access.
        /// Account data is preserved but user cannot perform any operations.
        /// </summary>
        /// <param name="deactivatedBy">User or system deactivating the account</param>
        /// <param name="reason">Reason for deactivation</param>
        public void Deactivate(string? deactivatedBy = null, string? reason = null)
        {
            if (!IsActive) return; // Already inactive

            IsActive = false;
            // Clear any active sessions or tokens would happen at application level
        }

        /// <summary>
        /// Marks the user account as deleted using soft delete pattern.
        /// Preserves data integrity while hiding the account from normal operations.
        /// </summary>
        /// <param name="deletedBy">User or system performing the deletion</param>
        public void MarkAsDeleted(string? deletedBy = null)
        {
            if (IsDeleted) return; // Already deleted

            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
            IsActive = false;
        }

        /// <summary>
        /// Restores a soft-deleted user account.
        /// Removes deletion markers but does not automatically activate the account.
        /// </summary>
        /// <param name="restoredBy">User or system performing the restoration</param>
        public void Restore(string? restoredBy = null)
        {
            if (!IsDeleted) return; // Not deleted

            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
            // Note: Account is not automatically activated after restoration
        }

        #endregion

        #region Domain Methods - Authentication & Security

        /// <summary>
        /// Records a successful login attempt.
        /// Updates login timestamp and resets failed attempt counter.
        /// </summary>
        public void RecordSuccessfulLogin()
        {
            LastLogin = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
            FailedLoginAttempts = 0;
            LockedAt = null;
            LockoutExpiresAt = null;
        }

        /// <summary>
        /// Records a failed login attempt and applies security policies.
        /// May lock the account if too many failed attempts occur.
        /// </summary>
        /// <param name="lockoutDuration">Duration to lock account if threshold is reached</param>
        /// <param name="maxAttempts">Maximum allowed failed attempts before lockout</param>
        /// <returns>True if account was locked due to this attempt</returns>
        public bool RecordFailedLogin(TimeSpan? lockoutDuration = null, int maxAttempts = 5)
        {
            FailedLoginAttempts++;

            if (FailedLoginAttempts >= maxAttempts)
            {
                LockAccount(lockoutDuration);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Locks the user account for security purposes.
        /// Account cannot be used for login until unlocked or lockout expires.
        /// </summary>
        /// <param name="lockoutDuration">Duration of lockout, null for permanent lock</param>
        public void LockAccount(TimeSpan? lockoutDuration = null)
        {
            LockedAt = DateTime.UtcNow;
            LockoutExpiresAt = lockoutDuration.HasValue ? DateTime.UtcNow.Add(lockoutDuration.Value) : null;
        }

        /// <summary>
        /// Unlocks the user account, allowing login attempts again.
        /// Resets failed login attempt counter.
        /// </summary>
        public void UnlockAccount()
        {
            LockedAt = null;
            LockoutExpiresAt = null;
            FailedLoginAttempts = 0;
        }

        /// <summary>
        /// Updates the user's last activity timestamp.
        /// Should be called on each authenticated request for session management.
        /// </summary>
        public void UpdateLastActivity()
        {
            LastActivity = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the user as having completed the initial setup process.
        /// Typically called after user has configured their profile and preferences.
        /// </summary>
        public void CompleteSetup()
        {
            HasCompletedSetup = true;
        }

        #endregion

        #region Domain Methods - Profile Management

        /// <summary>
        /// Updates the user's profile information with validation.
        /// Ensures all business rules are maintained during profile updates.
        /// </summary>
        /// <param name="firstName">New first name</param>
        /// <param name="lastName">New last name</param>
        /// <param name="jobTitle">New job title (optional)</param>
        /// <param name="department">New department (optional)</param>
        /// <exception cref="ArgumentException">Thrown when profile data is invalid</exception>
        public void UpdateProfile(string firstName, string lastName, string? jobTitle = null, string? department = null)
        {
            SetFirstName(firstName);
            SetLastName(lastName);
            JobTitle = jobTitle?.Trim();
            Department = department?.Trim();

            ValidateUser();
        }

        /// <summary>
        /// Updates the user's localization preferences.
        /// Validates language and timezone codes before assignment.
        /// </summary>
        /// <param name="preferredLanguage">Language code (e.g., "en-US", "pt-BR")</param>
        /// <param name="timeZone">Timezone identifier (e.g., "America/New_York")</param>
        /// <exception cref="ArgumentException">Thrown when language or timezone is invalid</exception>
        public void UpdateLocalizationPreferences(string? preferredLanguage = null, string? timeZone = null)
        {
            if (!string.IsNullOrWhiteSpace(preferredLanguage))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(preferredLanguage, @"^[a-z]{2}(-[A-Z]{2})?$"))
                    throw new ArgumentException("Invalid language code format", nameof(preferredLanguage));
                PreferredLanguage = preferredLanguage;
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
        }

        #endregion

        #region Domain Methods - Tenant Management

        /// <summary>
        /// Validates if the user belongs to the specified tenant.
        /// Critical for multi-tenant data security and access control.
        /// </summary>
        /// <param name="tenantId">The tenant ID to validate against</param>
        /// <returns>True if the user belongs to the specified tenant</returns>
        /// <exception cref="ArgumentException">Thrown when tenantId is null or empty</exception>
        public bool BelongsToTenant(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));

            return string.Equals(TenantId, tenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Changes the user's tenant assignment.
        /// Important operation that should be carefully controlled and audited.
        /// </summary>
        /// <param name="newTenantId">New tenant identifier</param>
        /// <param name="changedBy">User or system making the change</param>
        /// <exception cref="ArgumentException">Thrown when new tenant ID is invalid</exception>
        public void ChangeTenant(string newTenantId, string? changedBy = null)
        {
            if (string.IsNullOrWhiteSpace(newTenantId))
                throw new ArgumentException("New tenant ID cannot be null or empty", nameof(newTenantId));

            if (string.Equals(TenantId, newTenantId, StringComparison.OrdinalIgnoreCase))
                return; // Same tenant, no change needed

            var oldTenantId = TenantId;
            SetTenantId(newTenantId);

            // Domain event could be raised here for tenant change notifications
            // RaiseDomainEvent(new UserTenantChangedEvent(Id, oldTenantId, newTenantId, changedBy));
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Sets the first name with validation.
        /// </summary>
        /// <param name="firstName">First name to set</param>
        /// <exception cref="ArgumentException">Thrown when first name is invalid</exception>
        private void SetFirstName(string firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be null or empty", nameof(firstName));

            var trimmedName = firstName.Trim();
            if (trimmedName.Length < 2)
                throw new ArgumentException("First name must be at least 2 characters long", nameof(firstName));

            if (trimmedName.Length > 50)
                throw new ArgumentException("First name cannot exceed 50 characters", nameof(firstName));

            if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedName, @"^[a-zA-ZÀ-ÿ\s\-'\.]+$"))
                throw new ArgumentException("First name contains invalid characters", nameof(firstName));

            _firstName = trimmedName;
        }

        /// <summary>
        /// Sets the last name with validation.
        /// </summary>
        /// <param name="lastName">Last name to set</param>
        /// <exception cref="ArgumentException">Thrown when last name is invalid</exception>
        private void SetLastName(string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be null or empty", nameof(lastName));

            var trimmedName = lastName.Trim();
            if (trimmedName.Length < 2)
                throw new ArgumentException("Last name must be at least 2 characters long", nameof(lastName));

            if (trimmedName.Length > 50)
                throw new ArgumentException("Last name cannot exceed 50 characters", nameof(lastName));

            if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedName, @"^[a-zA-ZÀ-ÿ\s\-'\.]+$"))
                throw new ArgumentException("Last name contains invalid characters", nameof(lastName));

            _lastName = trimmedName;
        }

        /// <summary>
        /// Sets the tenant ID with validation.
        /// </summary>
        /// <param name="tenantId">Tenant ID to set</param>
        /// <exception cref="ArgumentException">Thrown when tenant ID is invalid</exception>
        private void SetTenantId(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));

            _tenantId = tenantId.Trim();
        }

        /// <summary>
        /// Validates the complete user entity state and business rules.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when user is in invalid state</exception>
        private void ValidateUser()
        {
            if (string.IsNullOrWhiteSpace(_tenantId))
                throw new InvalidOperationException("User must have a valid tenant ID");

            if (string.IsNullOrWhiteSpace(_firstName))
                throw new InvalidOperationException("User must have a valid first name");

            if (string.IsNullOrWhiteSpace(_lastName))
                throw new InvalidOperationException("User must have a valid last name");

            if (string.IsNullOrWhiteSpace(Email))
                throw new InvalidOperationException("User must have a valid email address");
        }

        #endregion

        #region ToString Override

        /// <summary>
        /// Returns a string representation of the user.
        /// Includes key user information for debugging and logging.
        /// </summary>
        /// <returns>String representation of the user</returns>
        public override string ToString()
        {
            var status = IsActive ? "ACTIVE" : "INACTIVE";
            if (IsDeleted) status = "DELETED";
            if (IsLockedOut) status += " [LOCKED]";

            return $"User [{status}] - {DisplayName} ({Email}) - Tenant: {TenantId} - Created: {CreatedAt:yyyy-MM-dd}";
        }

        #endregion
    }
}
