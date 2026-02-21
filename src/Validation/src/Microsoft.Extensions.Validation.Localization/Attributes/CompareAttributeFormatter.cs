// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization.Attributes;

/// <summary>
/// Formats error messages for <see cref="CompareAttribute"/> using the other property name.
/// </summary>
internal class CompareAttributeFormatter(CompareAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.OtherProperty);
}
