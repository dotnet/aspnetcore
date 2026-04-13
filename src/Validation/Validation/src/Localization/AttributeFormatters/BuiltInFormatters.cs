// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Extensions.Validation;

internal sealed class CompareAttributeFormatter(CompareAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.OtherProperty);
}

internal sealed class FileExtensionsAttributeFormatter(FileExtensionsAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.Extensions);
}

internal sealed class LengthAttributeFormatter(LengthAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.MinimumLength, attribute.MaximumLength);
}

internal sealed class MaxLengthAttributeFormatter(MaxLengthAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.Length);
}

internal sealed class MinLengthAttributeFormatter(MinLengthAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.Length);
}

internal sealed class RangeAttributeFormatter(RangeAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.Minimum, attribute.Maximum);
}

internal sealed class RegularExpressionAttributeFormatter(RegularExpressionAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.Pattern);
}

internal sealed class StringLengthAttributeFormatter(StringLengthAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.MaximumLength, attribute.MinimumLength);
}
