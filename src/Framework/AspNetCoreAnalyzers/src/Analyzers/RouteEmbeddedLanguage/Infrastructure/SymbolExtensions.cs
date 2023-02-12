// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class SymbolExtensions
{
    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attributeType)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeType))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasAttributeImplementingInterface(this ISymbol symbol, INamedTypeSymbol interfaceType)
    {
        return symbol.HasAttributeImplementingInterface(interfaceType, out var _);
    }

    public static bool HasAttributeImplementingInterface(this ISymbol symbol, INamedTypeSymbol interfaceType, [NotNullWhen(true)] out AttributeData? matchedAttribute)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (attributeData.AttributeClass is not null && attributeData.AttributeClass.Implements(interfaceType))
            {
                matchedAttribute = attributeData;
                return true;
            }
        }

        matchedAttribute = null;
        return false;
    }

    public static bool Implements(this ITypeSymbol type, ITypeSymbol interfaceType)
    {
        foreach (var t in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(t, interfaceType))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsType(this INamedTypeSymbol type, string typeName, SemanticModel semanticModel)
        => SymbolEqualityComparer.Default.Equals(type, semanticModel.Compilation.GetTypeByMetadataName(typeName));

    public static bool IsType(this INamedTypeSymbol type, INamedTypeSymbol otherType)
        => SymbolEqualityComparer.Default.Equals(type, otherType);

    public static ITypeSymbol GetParameterType(this ISymbol symbol)
    {
        return symbol switch
        {
            IParameterSymbol parameterSymbol => parameterSymbol.Type,
            IPropertySymbol propertySymbol => propertySymbol.Type,
            _ => throw new InvalidOperationException("Unexpected symbol type: " + symbol)
        };
    }

    public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol? symbol)
        => symbol switch
        {
            IMethodSymbol methodSymbol => methodSymbol.Parameters,
            IPropertySymbol parameterSymbol => parameterSymbol.Parameters,
            _ => ImmutableArray<IParameterSymbol>.Empty,
        };

    public static ISymbol? GetAnySymbol(this SymbolInfo info)
        => info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
}
