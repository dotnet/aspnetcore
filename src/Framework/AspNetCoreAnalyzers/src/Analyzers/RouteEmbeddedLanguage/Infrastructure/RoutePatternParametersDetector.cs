// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class RoutePatternParametersDetector
{
    public static ImmutableArray<ISymbol> ResolvedParameters(ISymbol symbol, SemanticModel semanticModel)
    {
        var resolvedParameterSymbols = new List<ISymbol>();
        var childSymbols = symbol switch
        {
            ITypeSymbol typeSymbol => typeSymbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray().As<ISymbol>(),
            IMethodSymbol methodSymbol => methodSymbol.Parameters.As<ISymbol>(),
            _ => throw new InvalidOperationException("Unexpected symbol type: " + symbol)
        };

        foreach (var child in childSymbols)
        {
            if (child.HasAttribute("Microsoft.AspNetCore.Http.AsParametersAttribute", semanticModel))
            {
                resolvedParameterSymbols.AddRange(ResolvedParameters(child.GetParameterType(), semanticModel));
            }
            else if (HasExplicitNonRouteAttribute(child, semanticModel) || HasSpecialType(child, semanticModel))
            {
                continue;
            }
            else
            {
                resolvedParameterSymbols.Add(child);
            }
        }
        return resolvedParameterSymbols.ToImmutableArray();
    }

    private static bool HasSpecialType(ISymbol child, SemanticModel semanticModel)
    {
        var type = child.GetParameterType() as INamedTypeSymbol;
        if (type == null)
        {
            return false;
        }

        if (type.IsType(typeof(CancellationToken).FullName!, semanticModel))
        {
            return true;
        }

        if (type.IsType("Microsoft.AspNetCore.Http.HttpContext", semanticModel))
        {
            return true;
        }

        if (type.IsType("Microsoft.AspNetCore.Http.HttpRequest", semanticModel))
        {
            return true;
        }

        if (type.IsType("Microsoft.AspNetCore.Http.HttpResponse", semanticModel))
        {
            return true;
        }

        if (type.IsType("System.Security.Claims.ClaimsPrincipal", semanticModel))
        {
            return true;
        }

        if (type.IsType("Microsoft.AspNetCore.Http.IFormFileCollection", semanticModel))
        {
            return true;
        }

        if (type.IsType("Microsoft.AspNetCore.Http.IFormFile", semanticModel))
        {
            return true;
        }

        if (type.IsType("System.IO.Stream", semanticModel))
        {
            return true;
        }

        if (type.IsType("System.IO.Pipelines.PipeReader", semanticModel))
        {
            return true;
        }

        return false;
    }

    private static bool HasExplicitNonRouteAttribute(ISymbol child, SemanticModel semanticModel)
    {
        var fromBodyMetadataType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.Metadata.IFromBodyMetadata");
        var fromFormMetadataType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.Metadata.IFromFormMetadata");
        var fromHeaderMetadataType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.Metadata.IFromHeaderMetadata");
        var fromQueryMetadataType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.Metadata.IFromQueryMetadata");
        var fromServicesMetadataType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.Metadata.IFromServiceMetadata");

        var allNoneRouteMetadataTypes = new[]
        {
            fromBodyMetadataType,
            fromFormMetadataType,
            fromHeaderMetadataType,
            fromQueryMetadataType,
            fromServicesMetadataType
        };

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
