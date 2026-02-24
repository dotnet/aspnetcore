// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Provides <see cref="IValidationAttributeFormatter"/> instances for
/// <see cref="ValidationAttribute"/> types used during validation error message formatting.
/// </summary>
public interface IValidationAttributeFormatterProvider
{
    /// <summary>
    /// Returns an <see cref="IValidationAttributeFormatter"/> capable of formatting the
    /// error message for the specified <paramref name="attribute"/>.
    /// </summary>
    /// <param name="attribute">The validation attribute to get a formatter for.</param>
    /// <returns>An <see cref="IValidationAttributeFormatter"/> for the attribute.</returns>
    public IValidationAttributeFormatter GetFormatter(ValidationAttribute attribute);
}
