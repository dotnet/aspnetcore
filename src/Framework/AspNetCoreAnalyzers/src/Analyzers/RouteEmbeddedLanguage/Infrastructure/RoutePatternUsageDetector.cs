// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal record struct RoutePatternUsageContext(
    IMethodSymbol? MethodSymbol,
    bool IsMinimal,
    bool IsMvcAttribute);

internal static class RoutePatternUsageDetector
{
    public static RoutePatternUsageContext BuildContext(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (token.Parent is not LiteralExpressionSyntax)
        {
            return default;
        }

        var container = token.TryFindContainer();
        if (container is null)
        {
            return default;
        }

        if (container.Parent.IsKind(SyntaxKind.Argument))
        {
            // We're an argument in a method call. See if we're a MapXXX method.
            var mapMethodSymbol = FindMapMethod(semanticModel, container, cancellationToken);
            if (mapMethodSymbol == null)
            {
                return default;
            }    
            return new(mapMethodSymbol, true, false);
        }
        else if (container.Parent.IsKind(SyntaxKind.AttributeArgument))
        {
            // We're an argument in an attribute. See if attribute is on a controller method.
            var attributeParent = FindAttributeParent(container);
            if (attributeParent is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                var actionMethodSymbol = FindMvcMethod(semanticModel, methodDeclarationSyntax, cancellationToken);
                if (actionMethodSymbol == null)
                {
                    return default;
                }
                return new(actionMethodSymbol, false, true);
            }
            else if (attributeParent is ClassDeclarationSyntax classDeclarationSyntax)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken);

                return new(null, false, MvcDetector.IsController(classSymbol, semanticModel));
            }
        }

        return default;
    }

    private static SyntaxNode? FindAttributeParent(SyntaxNode container)
    {
        var argument = container.Parent;
        if (argument.Parent is not AttributeArgumentListSyntax argumentList)
        {
            return null;
        }

        if (argumentList.Parent is not AttributeSyntax attribute)
        {
            return null;
        }

        if (attribute.Parent is not AttributeListSyntax attributeList)
        {
            return null;
        }

        return attributeList.Parent;
    }

    private static IMethodSymbol? FindMvcMethod(SemanticModel semanticModel, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);

        if (methodSymbol.ContainingType is not ITypeSymbol typeSymbol)
        {
            return null;
        }

        if (!MvcDetector.IsController(typeSymbol, semanticModel))
        {
            return null;
        }

        if (!MvcDetector.IsAction(methodSymbol, semanticModel))
        {
            return null;
        }

        return methodSymbol;
    }

    private static IMethodSymbol? FindMapMethod(SemanticModel semanticModel, SyntaxNode container, CancellationToken cancellationToken)
    {
        var argument = container.Parent;
        if (argument.Parent is not BaseArgumentListSyntax argumentList ||
            argumentList.Parent is null)
        {
            return null;
        }

        // Multiple overloads could be resolved, e.g. MapGet(string, RequestDelegate) and MapGet(string, Delegate)
        // Check each overload result to see whether it matches and return the first valid result.
        var symbols = GetBestOrAllSymbols(semanticModel.GetSymbolInfo(argumentList.Parent, cancellationToken));

        foreach (var symbol in symbols)
        {
            if (symbol is IMethodSymbol methodSymbol)
            {
                var matchingMapSymbol = FindValidMapMethod(semanticModel, argumentList, methodSymbol, cancellationToken);
                if (matchingMapSymbol != null)
                {
                    return matchingMapSymbol;
                }
            }
        }

        return null;
    }

    private static IMethodSymbol? FindValidMapMethod(SemanticModel semanticModel, BaseArgumentListSyntax argumentList, IMethodSymbol method, CancellationToken cancellationToken)
    {
        if (!method.Name.StartsWith("Map", StringComparison.Ordinal))
        {
            return null;
        }

        var delegateSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Delegate");
        var endpointRouteBuilderSymbol = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Routing.IEndpointRouteBuilder");

        var delegateArgument = method.Parameters.FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(delegateSymbol, a.Type));
        if (delegateArgument == null)
        {
            return null;
        }

        // IEndpointRouteBuilder may be removed from symbol because the method is called as an extension method.
        // ReducedFrom includes the original IEndpointRouteBuilder parameter.
        if (!(method.ReducedFrom ?? method).Parameters.Any(
            a => SymbolEqualityComparer.Default.Equals(a.Type, endpointRouteBuilderSymbol) ||
                a.Type.Implements(endpointRouteBuilderSymbol)))
        {
            return null;
        }

        var delegateIndex = method.Parameters.IndexOf(delegateArgument);
        if (delegateIndex >= argumentList.Arguments.Count)
        {
            return null;
        }

        var item = argumentList.Arguments[delegateIndex];

        return GetMethodInfo(semanticModel, item.Expression, cancellationToken);
    }

    private static IMethodSymbol? GetMethodInfo(SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        var delegateSymbolInfo = semanticModel.GetSymbolInfo(syntaxNode, cancellationToken);
        var delegateSymbol = delegateSymbolInfo.Symbol;
        if (delegateSymbol == null && delegateSymbolInfo.CandidateSymbols.Length == 1)
        {
            delegateSymbol = delegateSymbolInfo.CandidateSymbols[0];
        }

        return delegateSymbol as IMethodSymbol;
    }

    private static ImmutableArray<ISymbol> GetBestOrAllSymbols(SymbolInfo info)
    {
        if (info.Symbol != null)
        {
            return ImmutableArray.Create(info.Symbol);
        }
        else if (info.CandidateSymbols.Length > 0)
        {
            return info.CandidateSymbols;
        }

        return ImmutableArray<ISymbol>.Empty;
    }
}
