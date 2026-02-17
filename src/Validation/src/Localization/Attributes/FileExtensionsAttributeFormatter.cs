// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization.Attributes;

internal class FileExtensionsAttributeFormatter(FileExtensionsAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, attribute.Extensions);
}
