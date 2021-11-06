// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor;

// Visits top-level types and finds interface implementations.
internal class TagHelperTypeVisitor : SymbolVisitor
{
    private readonly INamedTypeSymbol _interface;
    private readonly List<INamedTypeSymbol> _results;

    public TagHelperTypeVisitor(INamedTypeSymbol @interface, List<INamedTypeSymbol> results)
    {
        _interface = @interface;
        _results = results;
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        if (IsTagHelper(symbol))
        {
            _results.Add(symbol);
        }
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            Visit(member);
        }
    }

    internal bool IsTagHelper(INamedTypeSymbol symbol)
    {
        if (_interface == null)
        {
            return false;
        }

        return
            symbol.TypeKind != TypeKind.Error &&
            symbol.DeclaredAccessibility == Accessibility.Public &&
            !symbol.IsAbstract &&
            !symbol.IsGenericType &&
            symbol.AllInterfaces.Contains(_interface);
    }
}
