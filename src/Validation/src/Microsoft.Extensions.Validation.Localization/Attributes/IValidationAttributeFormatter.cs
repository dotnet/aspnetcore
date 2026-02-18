// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization.Attributes;

/// <summary>
/// Represents an ability to take a validation error message template and format it with arguments.
/// The default validation localization pipeline uses <see cref="ValidationAttributeFormatterProvider"/>
/// to retrieve such formatter for any attribute it validates.
/// </summary>
public interface IValidationAttributeFormatter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="culture"></param>
    /// <param name="messageTemplate"></param>
    /// <param name="displayName"></param>
    /// <returns></returns>
    string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName);
}
