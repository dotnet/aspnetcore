// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization.Attributes;

public interface IValidationAttributeFormatter
{
    string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName);
}
