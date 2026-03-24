// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Base class for validation attributes that require asynchronous operations,
/// such as database lookups or remote service calls.
/// </summary>
/// <remarks>
/// <para>
/// Subclasses must override <see cref="IsValidAsync"/> to provide async validation logic.
/// The synchronous <see cref="ValidationAttribute.IsValid(object?, ValidationContext)"/> method
/// throws <see cref="NotSupportedException"/> to prevent silent fallback to a sync code path.
/// </para>
/// <para>
/// Async validation attributes are evaluated by the <c>Microsoft.Extensions.Validation</c>
/// pipeline when <see cref="ValidatablePropertyInfo.ValidateAsync"/> processes the property's
/// attributes. They are <b>not</b> evaluated by the legacy
/// <see cref="Validator.TryValidateObject(object, ValidationContext, System.Collections.Generic.ICollection{ValidationResult}?, bool)"/>
/// code path.
/// </para>
/// <example>
/// <code>
/// public sealed class UniqueEmailAttribute : AsyncValidationAttribute
/// {
///     protected override async Task&lt;ValidationResult?&gt; IsValidAsync(
///         object? value, ValidationContext validationContext, CancellationToken cancellationToken)
///     {
///         if (value is not string email)
///             return ValidationResult.Success;
///
///         var db = validationContext.GetRequiredService&lt;IUserRepository&gt;();
///         var exists = await db.EmailExistsAsync(email, cancellationToken);
///
///         return exists
///             ? new ValidationResult($"'{email}' is already taken.", new[] { validationContext.MemberName! })
///             : ValidationResult.Success;
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public abstract class AsyncValidationAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidationAttribute"/> class.
    /// </summary>
    protected AsyncValidationAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidationAttribute"/> class
    /// with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message to associate with a validation control.</param>
    protected AsyncValidationAttribute(string errorMessage) : base(errorMessage)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidationAttribute"/> class
    /// with a function that provides the error message.
    /// </summary>
    /// <param name="errorMessageAccessor">The function that enables access to validation resources.</param>
    protected AsyncValidationAttribute(Func<string> errorMessageAccessor) : base(errorMessageAccessor)
    {
    }

    /// <summary>
    /// Validates the specified <paramref name="value"/> asynchronously with respect to the current validation attribute.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context information about the validation operation.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the async validation operation.</param>
    /// <returns>
    /// An instance of <see cref="ValidationResult.Success"/> when validation is successful;
    /// otherwise, a <see cref="ValidationResult"/> that describes the validation failure.
    /// </returns>
    protected abstract Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes async validation and returns the result. This is the entry point used by the
    /// <c>Microsoft.Extensions.Validation</c> pipeline.
    /// </summary>
    internal Task<ValidationResult?> GetValidationResultAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        return IsValidAsync(value, validationContext, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// This override always throws <see cref="NotSupportedException"/>.
    /// Use the async validation pipeline (<c>Microsoft.Extensions.Validation</c>) instead of
    /// <see cref="Validator.TryValidateObject(object, ValidationContext, System.Collections.Generic.ICollection{ValidationResult}?, bool)"/>.
    /// </remarks>
    protected sealed override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        throw new NotSupportedException(
            $"The attribute '{GetType().Name}' requires async validation. " +
            $"Use the Microsoft.Extensions.Validation async pipeline (e.g. EditForm with DataAnnotationsValidator) " +
            $"instead of the synchronous Validator.TryValidateObject() code path.");
    }
}
