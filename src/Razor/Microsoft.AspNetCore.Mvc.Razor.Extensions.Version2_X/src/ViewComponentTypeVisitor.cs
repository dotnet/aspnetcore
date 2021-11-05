// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

internal class ViewComponentTypeVisitor : SymbolVisitor
{
    private readonly INamedTypeSymbol _viewComponentAttribute;
    private readonly INamedTypeSymbol _nonViewComponentAttribute;
    private readonly List<INamedTypeSymbol> _results;

    public ViewComponentTypeVisitor(
        INamedTypeSymbol viewComponentAttribute,
        INamedTypeSymbol nonViewComponentAttribute,
        List<INamedTypeSymbol> results)
    {
        _viewComponentAttribute = viewComponentAttribute;
        _nonViewComponentAttribute = nonViewComponentAttribute;
        _results = results;
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        if (IsViewComponent(symbol))
        {
            _results.Add(symbol);
        }

        if (symbol.DeclaredAccessibility != Accessibility.Public)
        {
            return;
        }

        foreach (var member in symbol.GetTypeMembers())
        {
            Visit(member);
        }
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            Visit(member);
        }
    }

    internal bool IsViewComponent(INamedTypeSymbol symbol)
    {
        if (_viewComponentAttribute == null)
        {
            return false;
        }

        if (symbol.DeclaredAccessibility != Accessibility.Public ||
            symbol.IsAbstract ||
            symbol.IsGenericType ||
            AttributeIsDefined(symbol, _nonViewComponentAttribute))
        {
            return false;
        }

        return symbol.Name.EndsWith(ViewComponentTypes.ViewComponentSuffix, StringComparison.Ordinal) ||
            AttributeIsDefined(symbol, _viewComponentAttribute);
    }

    private static bool AttributeIsDefined(INamedTypeSymbol type, INamedTypeSymbol queryAttribute)
    {
        if (type == null || queryAttribute == null)
        {
            return false;
        }

        foreach (var attribute in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, queryAttribute))
            {
                return true;
            }
        }

        return AttributeIsDefined(type.BaseType, queryAttribute);
    }
}
