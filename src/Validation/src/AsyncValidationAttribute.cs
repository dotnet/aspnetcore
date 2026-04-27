// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.DataAnnotations;

/// <summary>
/// Do not ship. This must be shipped from .NET runtime.
/// </summary>
public abstract class AsyncValidationAttribute : ValidationAttribute
{
    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        throw new NotSupportedException(
            $"The validation attribute '{GetType().Name}' supports only asynchronous validation. " +
            "Use the async validation APIs (e.g., Validator.TryValidateObjectAsync).");
    }

    /// <summary>
    /// Do not ship. This must be shipped from .NET runtime.
    /// </summary>
    protected abstract ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Do not ship. This must be shipped from .NET runtime.
    /// </summary>
    public async ValueTask<ValidationResult?> GetValidationResultAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        var result = await IsValidAsync(value, validationContext, cancellationToken).ConfigureAwait(false);

        // If validation fails, we want to ensure we have a ValidationResult that guarantees it has an ErrorMessage
        if (result != null)
        {
            if (string.IsNullOrEmpty(result.ErrorMessage))
            {
                var errorMessage = FormatErrorMessage(validationContext.DisplayName);
                result = new ValidationResult(errorMessage, result.MemberNames);
            }
        }

        return result;
    }
}
