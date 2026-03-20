// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides methods for generating valid HTML id attribute values from field names.
/// </summary>
internal static class FieldIdGenerator
{
    // Invalid characters for HTML5 id attributes: all Unicode whitespace characters and periods
    // Periods are excluded to avoid CSS selector conflicts
    private static readonly SearchValues<char> InvalidIdChars = SearchValues.Create(
        " \t\n\r\f\v\u0085\u00A0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u2028\u2029\u202F\u205F\u3000.");

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
        if (fieldName.AsSpan().IndexOfAny(InvalidIdChars) < 0)
        {
            return fieldName;
        }

        // Slow path: build sanitized string
        var result = new StringBuilder(fieldName.Length);

        foreach (var c in fieldName)
        {
            if (InvalidIdChars.Contains(c))
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
