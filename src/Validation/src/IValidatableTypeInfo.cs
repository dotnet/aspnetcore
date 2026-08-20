// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Represents an interface for validating a value of a type.
/// </summary>
public interface IValidatableTypeInfo
{
    /// <summary>
    /// Validates the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    void Validate(object? value, ValidateContext context);

    /// <summary>
    /// Validates the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">A cancellation token to support cancellation of the validation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the validatable property info for the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to find its validatable property info.</param>
    /// <param name="validationOptions">The validation options.</param>
    /// <param name="validatablePropertyInfo">The validatable property info, or null if not found.</param>
    /// <returns>True if the property is found. Otherwise, false.</returns>
    bool TryFindProperty(string propertyName, ValidationOptions validationOptions, [NotNullWhen(true)] out IValidatablePropertyInfo? validatablePropertyInfo);
}
