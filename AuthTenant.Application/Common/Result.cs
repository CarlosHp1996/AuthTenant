using System.Text;
using System.Text.Json.Serialization;

namespace AuthTenant.Application.Common
{
    /// <summary>
    /// Generic result wrapper for application operations following Railway Oriented Programming pattern.
    /// Provides a consistent way to handle success and failure scenarios across the application.
    /// Supports both single errors and multiple validation errors for comprehensive error handling.
    /// Thread-safe and immutable design ensures reliability in concurrent environments.
    /// </summary>
    /// <typeparam name="T">The type of data returned on successful operations</typeparam>
    public sealed class Result<T>
    {
        /// <summary>
        /// Gets a value indicating whether the operation was successful
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the operation failed
        /// </summary>
        [JsonIgnore]
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Gets the data returned by a successful operation
        /// </summary>
        public T? Data { get; private set; }

        /// <summary>
        /// Gets the primary error message for failed operations
        /// </summary>
        public string? Error { get; private set; }

        /// <summary>
        /// Gets the collection of error messages for operations with multiple validation failures
        /// </summary>
        public List<string> Errors { get; private set; } = new();

        /// <summary>
        /// Gets a value indicating whether there are multiple errors
        /// </summary>
        [JsonIgnore]
        public bool HasMultipleErrors => Errors.Count > 1;

        /// <summary>
        /// Gets a value indicating whether there are any errors (single or multiple)
        /// </summary>
        [JsonIgnore]
        public bool HasErrors => !string.IsNullOrEmpty(Error) || Errors.Count > 0;

        /// <summary>
        /// Gets all error messages combined (both Error and Errors collection)
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> AllErrors
        {
            get
            {
                if (!string.IsNullOrEmpty(Error))
                    yield return Error;

                foreach (var error in Errors)
                    yield return error;
            }
        }

        /// <summary>
        /// Private constructor to ensure result objects are created through factory methods
        /// </summary>
        /// <param name="isSuccess">Whether the operation was successful</param>
        /// <param name="data">The data returned by successful operations</param>
        /// <param name="error">The primary error message for failed operations</param>
        private Result(bool isSuccess, T? data, string? error)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;

            // Validate result state consistency
            if (isSuccess && HasErrors)
                throw new InvalidOperationException("Successful result cannot contain errors");

            if (!isSuccess && data != null)
                throw new InvalidOperationException("Failed result should not contain data");
        }

        /// <summary>
        /// Creates a successful result with the specified data
        /// </summary>
        /// <param name="data">The data to return</param>
        /// <returns>A successful result containing the specified data</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null for reference types</exception>
        public static Result<T> Success(T data)
        {
            // Allow null for nullable value types and reference types marked as nullable
            if (data == null && !IsNullableType(typeof(T)))
                throw new ArgumentNullException(nameof(data), "Data cannot be null for non-nullable types");

            return new Result<T>(true, data, null);
        }

        /// <summary>
        /// Creates a successful result without data (for operations that don't return data)
        /// </summary>
        /// <returns>A successful result with default data</returns>
        public static Result<T> Success()
        {
            return new Result<T>(true, default(T), null);
        }

        /// <summary>
        /// Creates a failed result with a single error message
        /// </summary>
        /// <param name="error">The error message describing the failure</param>
        /// <returns>A failed result with the specified error</returns>
        /// <exception cref="ArgumentException">Thrown when error message is null or empty</exception>
        public static Result<T> Failure(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                throw new ArgumentException("Error message cannot be null or empty", nameof(error));

            return new Result<T>(false, default(T), error.Trim());
        }

        /// <summary>
        /// Creates a failed result with multiple error messages
        /// </summary>
        /// <param name="errors">The collection of error messages</param>
        /// <returns>A failed result with the specified errors</returns>
        /// <exception cref="ArgumentException">Thrown when errors collection is null or empty</exception>
        public static Result<T> Failure(List<string> errors)
        {
            if (errors == null || errors.Count == 0)
                throw new ArgumentException("Errors collection cannot be null or empty", nameof(errors));

            // Filter out null or empty error messages
            var validErrors = errors
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .ToList();

            if (validErrors.Count == 0)
                throw new ArgumentException("At least one valid error message is required", nameof(errors));

            var result = new Result<T>(false, default(T), null)
            {
                Errors = validErrors
            };

            return result;
        }

        /// <summary>
        /// Creates a failed result with multiple error messages from an enumerable
        /// </summary>
        /// <param name="errors">The enumerable of error messages</param>
        /// <returns>A failed result with the specified errors</returns>
        public static Result<T> Failure(IEnumerable<string> errors)
        {
            return Failure(errors?.ToList() ?? new List<string>());
        }

        /// <summary>
        /// Creates a failed result from an exception
        /// </summary>
        /// <param name="exception">The exception that caused the failure</param>
        /// <param name="includeStackTrace">Whether to include the stack trace in the error message</param>
        /// <returns>A failed result with the exception message</returns>
        /// <exception cref="ArgumentNullException">Thrown when exception is null</exception>
        public static Result<T> Failure(Exception exception, bool includeStackTrace = false)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var errorMessage = includeStackTrace
                ? $"{exception.Message}\n{exception.StackTrace}"
                : exception.Message;

            return Failure(errorMessage);
        }

        /// <summary>
        /// Converts a successful result of one type to another using a mapping function
        /// </summary>
        /// <typeparam name="TTarget">The target type for conversion</typeparam>
        /// <param name="mapper">Function to convert the data</param>
        /// <returns>A result of the target type</returns>
        public Result<TTarget> Map<TTarget>(Func<T, TTarget> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            if (!IsSuccess)
            {
                return HasMultipleErrors
                    ? Result<TTarget>.Failure(Errors)
                    : Result<TTarget>.Failure(Error!);
            }

            try
            {
                var mappedData = mapper(Data!);
                return Result<TTarget>.Success(mappedData);
            }
            catch (Exception ex)
            {
                return Result<TTarget>.Failure($"Mapping failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes an action on successful results and returns the original result
        /// </summary>
        /// <param name="action">Action to execute on success</param>
        /// <returns>The original result</returns>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (IsSuccess)
            {
                try
                {
                    action(Data!);
                }
                catch
                {
                    // Log exception but don't modify the result
                    // This maintains the original successful state
                }
            }

            return this;
        }

        /// <summary>
        /// Executes an action on failed results and returns the original result
        /// </summary>
        /// <param name="action">Action to execute on failure</param>
        /// <returns>The original result</returns>
        public Result<T> OnFailure(Action<string> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (IsFailure)
            {
                try
                {
                    var errorMessage = !string.IsNullOrEmpty(Error)
                        ? Error
                        : string.Join("; ", Errors);
                    action(errorMessage);
                }
                catch
                {
                    // Log exception but don't modify the result
                    // This maintains the original failed state
                }
            }

            return this;
        }

        /// <summary>
        /// Gets a formatted error message combining all errors
        /// </summary>
        /// <returns>A formatted string containing all error messages</returns>
        public string GetErrorMessage()
        {
            if (IsSuccess)
                return string.Empty;

            if (!string.IsNullOrEmpty(Error) && Errors.Count == 0)
                return Error;

            if (string.IsNullOrEmpty(Error) && Errors.Count > 0)
                return string.Join("; ", Errors);

            // Both Error and Errors have values
            var allErrors = AllErrors.ToList();
            return string.Join("; ", allErrors);
        }

        /// <summary>
        /// Determines if a type is nullable (either nullable value type or reference type)
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is nullable</returns>
        private static bool IsNullableType(Type type)
        {
            if (!type.IsValueType) return true; // Reference types are nullable
            return Nullable.GetUnderlyingType(type) != null; // Nullable<T>
        }

        /// <summary>
        /// Provides a safe string representation of the result for logging and debugging
        /// </summary>
        /// <returns>A formatted string representation</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Result<{typeof(T).Name}>(");
            sb.Append($"Success: {IsSuccess}");

            if (IsSuccess)
            {
                var dataStr = Data?.ToString() ?? "null";
                if (dataStr.Length > 50)
                    dataStr = dataStr.Substring(0, 47) + "...";
                sb.Append($", Data: {dataStr}");
            }
            else
            {
                var errorMsg = GetErrorMessage();
                if (errorMsg.Length > 100)
                    errorMsg = errorMsg.Substring(0, 97) + "...";
                sb.Append($", Error: {errorMsg}");
            }

            sb.Append(")");
            return sb.ToString();
        }

        /// <summary>
        /// Determines equality based on result state and data
        /// </summary>
        /// <param name="obj">Object to compare with</param>
        /// <returns>True if objects are equal</returns>
        public override bool Equals(object? obj)
        {
            if (obj is not Result<T> other)
                return false;

            if (IsSuccess != other.IsSuccess)
                return false;

            if (IsSuccess)
                return EqualityComparer<T>.Default.Equals(Data, other.Data);

            return string.Equals(GetErrorMessage(), other.GetErrorMessage(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the hash code for the result
        /// </summary>
        /// <returns>Hash code value</returns>
        public override int GetHashCode()
        {
            return IsSuccess
                ? HashCode.Combine(IsSuccess, Data)
                : HashCode.Combine(IsSuccess, GetErrorMessage());
        }

        /// <summary>
        /// Implicit conversion from data to successful result
        /// </summary>
        /// <param name="data">The data to wrap in a result</param>
        public static implicit operator Result<T>(T data)
        {
            return Success(data);
        }

        /// <summary>
        /// Implicit conversion from string to failed result
        /// </summary>
        /// <param name="error">The error message</param>
        public static implicit operator Result<T>(string error)
        {
            return Failure(error);
        }
    }

    /// <summary>
    /// Non-generic result class for operations that don't return data
    /// </summary>
    public static class Result
    {
        /// <summary>
        /// Creates a successful result without data
        /// </summary>
        /// <returns>A successful result</returns>
        public static Result<Unit> Success()
        {
            return Result<Unit>.Success(Unit.Value);
        }

        /// <summary>
        /// Creates a failed result with a single error message
        /// </summary>
        /// <param name="error">The error message</param>
        /// <returns>A failed result</returns>
        public static Result<Unit> Failure(string error)
        {
            return Result<Unit>.Failure(error);
        }

        /// <summary>
        /// Creates a failed result with multiple error messages
        /// </summary>
        /// <param name="errors">The collection of error messages</param>
        /// <returns>A failed result</returns>
        public static Result<Unit> Failure(List<string> errors)
        {
            return Result<Unit>.Failure(errors);
        }
    }

    /// <summary>
    /// Unit type for results that don't return data
    /// </summary>
    public sealed class Unit
    {
        /// <summary>
        /// The singleton instance of Unit
        /// </summary>
        public static readonly Unit Value = new Unit();

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private Unit() { }

        /// <summary>
        /// String representation of Unit
        /// </summary>
        /// <returns>Unit string representation</returns>
        public override string ToString() => "()";

        /// <summary>
        /// Equality comparison for Unit (always true for Unit instances)
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>True if obj is Unit</returns>
        public override bool Equals(object? obj) => obj is Unit;

        /// <summary>
        /// Hash code for Unit (constant value)
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode() => 0;
    }
}
