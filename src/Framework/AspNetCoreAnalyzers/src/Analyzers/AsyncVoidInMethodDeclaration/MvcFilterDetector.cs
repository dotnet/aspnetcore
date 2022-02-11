// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public partial class AsyncVoidInMethodDeclarationAnalyzer
{
    private static bool IsMvcFilter(ITypeSymbol? classSymbol, WellKnownTypes wellKnownTypes) // TODO method unstable
    {
        // TODO: use all possible interfaces defined in wellknowntypes to detect
        return ((classSymbol?.AllInterfaces.Contains(wellKnownTypes.IActionFilter) ?? false)
            || SymbolEqualityComparer.Default.Equals(wellKnownTypes.IActionFilter, classSymbol?.BaseType));
    }

    private static bool IsMvcFilterMethod(IMethodSymbol? methodSymbol, WellKnownTypes wellKnownTypes)
    {
        return false;
    }
}
