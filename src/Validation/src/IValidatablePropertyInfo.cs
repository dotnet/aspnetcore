// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Represents an interface for validating a property.
/// </summary>
public interface IValidatablePropertyInfo
{
    /// <summary>
    /// Validates the property on a given object.
    /// </summary>
    /// <param name="containingObject">The object that contains the property to validate.</param>
    /// <param name="context">The validation context.</param>
    void Validate(object containingObject, ValidateContext context);

    /// <summary>
    /// Validates the property on a given object.
    /// </summary>
    /// <param name="containingObject">The object that contains the property to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">A cancellation token to support cancellation of the validation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ValidateAsync(object containingObject, ValidateContext context, CancellationToken cancellationToken);
}
