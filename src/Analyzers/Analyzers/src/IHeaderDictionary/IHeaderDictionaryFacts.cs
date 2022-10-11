// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.IHeaderDictionary;

internal static class IHeaderDictionaryFacts
{
    public static bool IsIHeaderDictionary(IHeaderDictionarySymbols symbols, INamedTypeSymbol symbol)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        return SymbolEqualityComparer.Default.Equals(symbol, symbols.IHeaderDictionary);
    }

    public static bool IsAdd(IHeaderDictionarySymbols symbols, IMethodSymbol symbol)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (symbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (symbol.Name == null ||
            !string.Equals(symbol.Name, SymbolNames.IHeaderDictionary.AddMethodName, StringComparison.Ordinal))
        {
            return false;
        }

        if (symbol.Parameters.Length != 2)
        {
            return false;
        }

        return true;
    }
}
