// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

// Copied from
// https://github.com/dotnet/aspnetcore-tooling/blob/master/src/Razor/src/Microsoft.AspNetCore.Razor.Language/CSharpIdentifier.cs
namespace Microsoft.Extensions.ApiDescription.Client;

internal static class CSharpIdentifier
{
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

    public static string SanitizeIdentifier(string inputName)
    {
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
