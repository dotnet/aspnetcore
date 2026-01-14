// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides methods for generating valid HTML id attribute values from field names.
/// </summary>
internal static class FieldIdGenerator
{
    /// <summary>
    /// Sanitizes a field name to create a valid HTML id attribute value.
    /// </summary>
    /// <param name="fieldName">The field name to sanitize.</param>
    /// <returns>A valid HTML id attribute value, or an empty string if the input is null or empty.</returns>
    /// <remarks>
    /// This method follows HTML5 id attribute rules:
    /// - The value must contain at least one character
    /// - The value must not contain any whitespace characters
    /// - Periods are replaced with underscores to avoid CSS selector conflicts
    /// </remarks>
    public static string SanitizeHtmlId(string? fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return string.Empty;
        }

        // Fast path: check if sanitization is needed
        var needsSanitization = false;
        foreach (var c in fieldName)
        {
            if (char.IsWhiteSpace(c) || c == '.')
            {
                needsSanitization = true;
                break;
            }
        }

        if (!needsSanitization)
        {
            return fieldName;
        }

        // Slow path: build sanitized string
        var result = new StringBuilder(fieldName.Length);

        foreach (var c in fieldName)
        {
            if (char.IsWhiteSpace(c) || c == '.')
            {
                result.Append('_');
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
