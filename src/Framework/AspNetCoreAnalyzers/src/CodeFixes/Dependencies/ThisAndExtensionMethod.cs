// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal readonly struct ThisAndExtensionMethod(ITypeSymbol thisType, string extensionMethod)
{
    public ITypeSymbol ThisType { get; } = thisType;
    public string ExtensionMethod { get; } = extensionMethod;

    public override bool Equals(object? obj)
    {
        if (obj is ThisAndExtensionMethod other)
        {
            return SymbolEqualityComparer.Default.Equals(ThisType, other.ThisType) &&
                ExtensionMethod.Equals(other.ExtensionMethod, StringComparison.Ordinal);
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(ThisType, ExtensionMethod);
}
