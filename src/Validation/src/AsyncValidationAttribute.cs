// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.ComponentModel.DataAnnotations;

/// <summary>
/// Do not ship. This must be shipped from .NET runtime.
/// </summary>
public abstract class AsyncValidationAttribute : ValidationAttribute
{
    /// <inheritdoc/>
    protected abstract override ValidationResult? IsValid(object? value, ValidationContext validationContext);

    /// <inheritdoc/>
    public sealed override bool IsValid(object? value)
        => throw new UnreachableException();

    /// <inheritdoc/>
    public sealed override bool RequiresValidationContext => true;

    /// <summary>
    /// Do not ship. This must be shipped from .NET runtime.
    /// </summary>
    protected abstract Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Do not ship. This must be shipped from .NET runtime.
    /// </summary>
    public async Task<ValidationResult?> GetValidationResultAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
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
