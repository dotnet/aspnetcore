// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

internal static class Extensions
{
    public static bool TryGetEscapeCharacter(this VirtualChar ch, out char escapedChar)
        => TryGetEscapeCharacter(ch.Value, out escapedChar);

    public static bool TryGetEscapeCharacter(Rune rune, out char escapedChar)
        => TryGetEscapeCharacter(rune.Value, out escapedChar);

    private static bool TryGetEscapeCharacter(int value, out char escapedChar)
    {
        // Keep in sync with CSharpVirtualCharService.TryAddSingleCharacterEscape
        switch (value)
        {
            // Note: we don't care about single quote as that doesn't need to be escaped when
            // producing a normal C# string literal.

            // case '\'':

            // escaped characters that translate to themselves.  
            case '"': escapedChar = '"'; return true;
            case '\\': escapedChar = '\\'; return true;

            // translate escapes as per C# spec 2.4.4.4
            case '\0': escapedChar = '0'; return true;
            case '\a': escapedChar = 'a'; return true;
            case '\b': escapedChar = 'b'; return true;
            case '\f': escapedChar = 'f'; return true;
            case '\n': escapedChar = 'n'; return true;
            case '\r': escapedChar = 'r'; return true;
            case '\t': escapedChar = 't'; return true;
            case '\v': escapedChar = 'v'; return true;
        }

        escapedChar = default;
        return false;
    }
}
