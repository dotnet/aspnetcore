// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Formats a localized error message template with attribute-specific arguments.
/// Replaces the MVC adapter pattern with a simpler, framework-independent interface.
/// </summary>
[Experimental("ASP0030")]
public interface IValidationAttributeFormatter
{
    /// <summary>
    /// Formats the error message to present to the user.
    /// </summary>
    string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName);
}
