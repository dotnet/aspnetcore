// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

internal static class CSharpIdentifier
{
    private const string CshtmlExtension = ".cshtml";

    public static string GetClassNameFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        if (path.EndsWith(CshtmlExtension, StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring(0, path.Length - CshtmlExtension.Length);
        }

        return SanitizeClassName(path);
    }

    // CSharp Spec §2.4.2
    private static bool IsIdentifierStart(char character)
    {
        return char.IsLetter(character) ||
            character == '_' ||
            CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.LetterNumber;
    }

    public static bool IsIdentifierPart(char character)
    {
        return char.IsDigit(character) ||
               IsIdentifierStart(character) ||
               IsIdentifierPartByUnicodeCategory(character);
    }

    private static bool IsIdentifierPartByUnicodeCategory(char character)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(character);

        return category == UnicodeCategory.NonSpacingMark || // Mn
            category == UnicodeCategory.SpacingCombiningMark || // Mc
            category == UnicodeCategory.ConnectorPunctuation || // Pc
            category == UnicodeCategory.Format; // Cf
    }

    public static string SanitizeClassName(string inputName)
    {
        if (string.IsNullOrEmpty(inputName))
        {
            return inputName;
        }

        if (!IsIdentifierStart(inputName[0]) && IsIdentifierPart(inputName[0]))
        {
            inputName = "_" + inputName;
        }

        var builder = new StringBuilder(inputName.Length);
        for (var i = 0; i < inputName.Length; i++)
        {
            var ch = inputName[i];
            builder.Append(IsIdentifierPart(ch) ? ch : '_');
        }

        return builder.ToString();
    }
}
