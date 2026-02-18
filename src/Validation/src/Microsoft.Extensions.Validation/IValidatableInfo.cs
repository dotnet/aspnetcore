// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Represents an interface for validating a value.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IValidatableInfo
{
    /// <summary>
    /// Validates the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">A cancellation token to support cancellation of the validation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken);
}
