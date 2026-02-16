// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Provides the format arguments used when formatting localized validation error messages.
/// </summary>
public interface IAttributeArgumentProvider
{
    /// <summary>
    /// Returns the format arguments for the specified <paramref name="attribute"/>.
    /// </summary>
    /// <param name="attribute">The validation attribute whose error message is being formatted.</param>
    /// <param name="displayName">The resolved display name of the member being validated.</param>
    /// <returns>An array of arguments to pass to <see cref="string.Format(string, object?[])"/>.</returns>
    public object?[] GetFormatArgs(ValidationAttribute attribute, string displayName);
}
