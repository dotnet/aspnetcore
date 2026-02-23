// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization.Attributes;

/// <summary>
/// Fallback <see cref="IValidationAttributeFormatter"/> for attributes whose error message
/// template contains only the display name placeholder (<c>{0}</c>).
/// </summary>
internal class DefaultAttributeFormatter : IValidationAttributeFormatter
{
    /// <summary>
    /// Gets the singleton instance of <see cref="DefaultAttributeFormatter"/>.
    /// </summary>
    public static DefaultAttributeFormatter Instance { get; } = new DefaultAttributeFormatter();

    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName);
}
