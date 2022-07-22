// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class RoutePatternParametersDetector
{
    public static ImmutableArray<ISymbol> ResolvedParameters(ISymbol symbol, WellKnownTypes wellKnownTypes)
    {
        var resolvedParameterSymbols = ImmutableArray.CreateBuilder<ISymbol>();
        var childSymbols = symbol switch
        {
            ITypeSymbol typeSymbol => typeSymbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray().As<ISymbol>(),
            IMethodSymbol methodSymbol => methodSymbol.Parameters.As<ISymbol>(),
            _ => throw new InvalidOperationException("Unexpected symbol type: " + symbol)
        };

        var allNoneRouteMetadataTypes = new[]
        {
            wellKnownTypes.IFromBodyMetadata,
            wellKnownTypes.IFromFormMetadata,
            wellKnownTypes.IFromHeaderMetadata,
            wellKnownTypes.IFromQueryMetadata,
            wellKnownTypes.IFromServiceMetadata
        };
        var specialTypes = new[]
        {
            wellKnownTypes.CancellationToken,
            wellKnownTypes.HttpContext,
            wellKnownTypes.HttpRequest,
            wellKnownTypes.HttpResponse,
            wellKnownTypes.ClaimsPrincipal,
            wellKnownTypes.IFormFileCollection,
            wellKnownTypes.IFormFile,
            wellKnownTypes.Stream,
            wellKnownTypes.PipeReader
        };

        foreach (var child in childSymbols)
        {
            if (HasSpecialType(child, specialTypes) || HasExplicitNonRouteAttribute(child, allNoneRouteMetadataTypes))
            {
                continue;
            }
            else if (child.HasAttribute(wellKnownTypes.AsParametersAttribute))
            {
                resolvedParameterSymbols.AddRange(ResolvedParameters(child.GetParameterType(), wellKnownTypes));
            }
            else
            {
                resolvedParameterSymbols.Add(child);
            }
        }
        return resolvedParameterSymbols.ToImmutable();
    }

    private static bool HasSpecialType(ISymbol child, INamedTypeSymbol[] specialTypes)
    {
        if (child.GetParameterType() is not INamedTypeSymbol type)
        {
            return false;
        }

        foreach (var specialType in specialTypes)
        {
            if (type.IsType(specialType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasExplicitNonRouteAttribute(ISymbol child, INamedTypeSymbol[] allNoneRouteMetadataTypes)
    {
        foreach (var attributeData in child.GetAttributes())
        {
            foreach (var nonRouteMetadata in allNoneRouteMetadataTypes)
            {
                if (attributeData.AttributeClass.Implements(nonRouteMetadata))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
