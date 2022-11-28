// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal record struct ParameterSymbol(ISymbol Symbol, ISymbol? TopLevelSymbol = null)
{
    public bool IsNested => TopLevelSymbol != null;
}

internal static class RoutePatternParametersDetector
{
    public static ImmutableArray<ParameterSymbol> ResolvedParameters(ISymbol symbol, WellKnownTypes wellKnownTypes)
    {
        return ResolvedParametersCore(symbol, topLevelSymbol: null, wellKnownTypes);

        static ImmutableArray<ParameterSymbol> ResolvedParametersCore(ISymbol symbol, ISymbol? topLevelSymbol, WellKnownTypes wellKnownTypes)
        {
            var resolvedParameterSymbols = ImmutableArray.CreateBuilder<ParameterSymbol>();
            var childSymbols = GetParameterSymbols(symbol);

            foreach (var child in childSymbols)
            {
                if (HasSpecialType(child, wellKnownTypes, RouteWellKnownTypes.ParameterSpecialTypes) || HasExplicitNonRouteAttribute(child, wellKnownTypes, RouteWellKnownTypes.NonRouteMetadataTypes))
                {
                    continue;
                }
                else if (child.HasAttribute(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_AsParametersAttribute)))
                {
                    resolvedParameterSymbols.AddRange(ResolvedParametersCore(child.GetParameterType(), child, wellKnownTypes));
                }
                else
                {
                    resolvedParameterSymbols.Add(new ParameterSymbol(child, topLevelSymbol));
                }
            }
            return resolvedParameterSymbols.ToImmutable();
        }
    }

    public static ImmutableArray<ISymbol> GetParameterSymbols(ISymbol symbol)
    {
        return symbol switch
        {
            ITypeSymbol typeSymbol => typeSymbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray().As<ISymbol>(),
            IMethodSymbol methodSymbol => methodSymbol.Parameters.As<ISymbol>(),
            _ => throw new InvalidOperationException("Unexpected symbol type: " + symbol)
        };
    }

    private static bool HasSpecialType(ISymbol child, WellKnownTypes wellKnownTypes, WellKnownType[] specialTypes)
    {
        if (child.GetParameterType() is not INamedTypeSymbol type)
        {
            return false;
        }

        return wellKnownTypes.IsType(type, specialTypes);
    }

    private static bool HasExplicitNonRouteAttribute(ISymbol child, WellKnownTypes wellKnownTypes, WellKnownType[] allNoneRouteMetadataTypes)
    {
        foreach (var attributeData in child.GetAttributes())
        {
            var attributeClass = attributeData.AttributeClass;

            if (wellKnownTypes.Implements(attributeClass, allNoneRouteMetadataTypes))
            {
                return true;
            }
        }

        return false;
    }
}
