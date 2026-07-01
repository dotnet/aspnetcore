// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Formats a validation message template with attribute-specific arguments.
/// Used by the validation localization pipeline to produce fully formatted messages
/// from localized templates that contain positional placeholders.
/// </summary>
public interface IValidationAttributeFormatter
{
    /// <summary>
    /// Formats the specified <paramref name="messageTemplate"/> by substituting attribute-specific
    /// arguments alongside the <paramref name="displayName"/>.
    /// </summary>
    /// <param name="culture">The <see cref="CultureInfo"/> to use when formatting.</param>
    /// <param name="messageTemplate">The message template containing format placeholders.</param>
    /// <param name="displayName">The resolved display name of the member being validated.</param>
    /// <returns>The fully formatted message.</returns>
    string FormatMessage(CultureInfo culture, string messageTemplate, string displayName);
}
