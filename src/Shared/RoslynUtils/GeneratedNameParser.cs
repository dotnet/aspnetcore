// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

// These sources are copied from https://github.com/dotnet/roslyn/blob/7d7bf0cc73e335390d73c9de6d7afd1e49605c9d/src/Compilers/CSharp/Portable/Symbols/Synthesized/GeneratedNameParser.cs
// and exist to address the issues with extracting original method names for
// generated local functions. See https://github.com/dotnet/roslyn/issues/55651
// for more info.
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class GeneratedNameParser
    {
        // Parse the generated name. Returns true for names of the form
        // [CS$]<[middle]>c[__[suffix]] where [CS$] is included for certain
        // generated names, where [middle] and [__[suffix]] are optional,
        // and where c is a single character in [1-9a-z]
        // (csharp\LanguageAnalysis\LIB\SpecialName.cpp).
        internal static bool TryParseGeneratedName(
            string name,
            out GeneratedNameKind kind,
            out int openBracketOffset,
            out int closeBracketOffset)
        {
            openBracketOffset = -1;
            if (name.StartsWith("CS$<", StringComparison.Ordinal))
            {
                openBracketOffset = 3;
            }
            else if (name.StartsWith("<", StringComparison.Ordinal))
            {
                openBracketOffset = 0;
            }

            if (openBracketOffset >= 0)
            {
                closeBracketOffset = IndexOfBalancedParenthesis(name, openBracketOffset, '>');
                if (closeBracketOffset >= 0 && closeBracketOffset + 1 < name.Length)
                {
                    int c = name[closeBracketOffset + 1];
                    if (c is >= '1' and <= '9' or >= 'a' and <= 'z') // Note '0' is not special.
                    {
                        kind = (GeneratedNameKind)c;
                        return true;
                    }
                }
            }

            kind = GeneratedNameKind.None;
            openBracketOffset = -1;
            closeBracketOffset = -1;
            return false;
        }

        private static int IndexOfBalancedParenthesis(string str, int openingOffset, char closing)
        {
            char opening = str[openingOffset];

            int depth = 1;
            for (int i = openingOffset + 1; i < str.Length; i++)
            {
                var c = str[i];
                if (c == opening)
                {
                    depth++;
                }
                else if (c == closing)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        internal static bool TryParseSourceMethodNameFromGeneratedName(string generatedName, GeneratedNameKind requiredKind, [NotNullWhen(true)] out string? methodName)
        {
            if (!TryParseGeneratedName(generatedName, out var kind, out int openBracketOffset, out int closeBracketOffset))
            {
                methodName = null;
                return false;
            }

            if (requiredKind != 0 && kind != requiredKind)
            {
                methodName = null;
                return false;
            }

            methodName = generatedName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);

            if (kind.IsTypeName())
            {
                methodName = methodName.Replace(GeneratedNameConstants.DotReplacementInTypeNames, '.');
            }

            return true;
        }

        /// <summary>
        /// Parses generated local function name out of a generated method name.
        /// </summary>
        internal static bool TryParseLocalFunctionName(string generatedName, [NotNullWhen(true)] out string? localFunctionName)
        {
            localFunctionName = null;

            // '<' containing-method-name '>' 'g' '__' local-function-name '|' method-ordinal '_' lambda-ordinal
            if (!TryParseGeneratedName(generatedName, out var kind, out _, out int closeBracketOffset) || kind != GeneratedNameKind.LocalFunction)
            {
                return false;
            }

            int localFunctionNameStart = closeBracketOffset + 2 + GeneratedNameConstants.SuffixSeparator.Length;
            if (localFunctionNameStart >= generatedName.Length)
            {
                return false;
            }

            int localFunctionNameEnd = generatedName.IndexOf(GeneratedNameConstants.LocalFunctionNameTerminator, localFunctionNameStart);
            if (localFunctionNameEnd < 0)
            {
                return false;
            }

            localFunctionName = generatedName.Substring(localFunctionNameStart, localFunctionNameEnd - localFunctionNameStart);
            return true;
        }

        // Extracts the slot index from a name of a field that stores hoisted variables or awaiters.
        // Such a name ends with "__{slot index + 1}".
        // Returned slot index is >= 0.
        internal static bool TryParseSlotIndex(string fieldName, out int slotIndex)
        {
            int lastUnder = fieldName.LastIndexOf('_');
            if (lastUnder - 1 < 0 || lastUnder == fieldName.Length || fieldName[lastUnder - 1] != '_')
            {
                slotIndex = -1;
                return false;
            }

            if (int.TryParse(fieldName.AsSpan(lastUnder + 1), NumberStyles.None, CultureInfo.InvariantCulture, out slotIndex) && slotIndex >= 1)
            {
                slotIndex--;
                return true;
            }

            slotIndex = -1;
            return false;
        }

        internal static bool TryParseAnonymousTypeParameterName(string typeParameterName, [NotNullWhen(true)] out string? propertyName)
        {
            if (typeParameterName.StartsWith("<", StringComparison.Ordinal) &&
                typeParameterName.EndsWith(">j__TPar", StringComparison.Ordinal))
            {
                propertyName = typeParameterName.Substring(1, typeParameterName.Length - 9);
                return true;
            }

            propertyName = null;
            return false;
        }
    }
}