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
    // Valid characters for HTML 4.01 id attributes (excluding '.' to avoid CSS selector conflicts)
    // See: https://www.w3.org/TR/html401/types.html#type-id
    private static readonly SearchValues<char> ValidIdChars =
        SearchValues.Create("-0123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");

    /// <summary>
    /// Sanitizes a field name to create a valid HTML id attribute value.
    /// </summary>
    /// <param name="fieldName">The field name to sanitize.</param>
    /// <returns>A valid HTML id attribute value, or an empty string if the input is null or empty.</returns>
    /// <remarks>
    /// This method follows HTML 4.01 id attribute rules:
    /// - The first character must be a letter (A-Z, a-z)
    /// - Subsequent characters can be letters, digits, hyphens, underscores, colons, or periods
    /// - Periods are replaced with underscores to avoid CSS selector conflicts
    /// </remarks>
    public static string SanitizeHtmlId(string? fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return string.Empty;
        }

        // Fast path: check if sanitization is needed
        var firstChar = fieldName[0];
        var startsWithLetter = char.IsAsciiLetter(firstChar);
        var indexOfInvalidChar = fieldName.AsSpan(1).IndexOfAnyExcept(ValidIdChars);

        if (startsWithLetter && indexOfInvalidChar < 0)
        {
            return fieldName;
        }

        // Slow path: build sanitized string
        var result = new StringBuilder(fieldName.Length);

        // First character must be a letter
        if (startsWithLetter)
        {
            result.Append(firstChar);
        }
        else
        {
            result.Append('z');
            if (IsValidIdChar(firstChar))
            {
                result.Append(firstChar);
            }
            else
            {
                result.Append('_');
            }
        }

        // Process remaining characters
        for (var i = 1; i < fieldName.Length; i++)
        {
            var c = fieldName[i];
            result.Append(IsValidIdChar(c) ? c : '_');
        }

        return result.ToString();
    }

    private static bool IsValidIdChar(char c)
        => ValidIdChars.Contains(c);
}
