// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal readonly struct ThisAndExtensionMethod(ITypeSymbol thisType, string extensionMethod)
{
    public ITypeSymbol ThisType { get; } = thisType;
    public string ExtensionMethod { get; } = extensionMethod;

    public override bool Equals(object obj)
    {
        return obj is ThisAndExtensionMethod other &&
            SymbolEqualityComparer.Default.Equals(ThisType, other.ThisType) &&
            ExtensionMethod == other.ExtensionMethod;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SymbolEqualityComparer.Default.GetHashCode(ThisType), ExtensionMethod);
    }
}
