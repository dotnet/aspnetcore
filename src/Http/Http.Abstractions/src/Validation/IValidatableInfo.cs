// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Represents an interface for validating a value.
/// </summary>
public interface IValidatableInfo
{
    /// <summary>
    /// Validates the specified value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken"></param>
    ValueTask ValidateAsync(object? value, ValidatableContext context, CancellationToken cancellationToken);
}
