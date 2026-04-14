// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization.AttributeFormatters;

/// <summary>
/// Formats error messages for <see cref="StringLengthAttribute"/> using the maximum and minimum length.
/// </summary>
internal class StringLengthAttributeFormatter(StringLengthAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.MaximumLength, attribute.MinimumLength);
}
