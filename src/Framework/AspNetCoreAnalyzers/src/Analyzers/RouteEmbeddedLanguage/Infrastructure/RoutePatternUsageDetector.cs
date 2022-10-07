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

internal readonly record struct RoutePatternUsageContext(
    IMethodSymbol? MethodSymbol,
    SyntaxNode MethodSyntax,
    bool IsMinimal,
    bool IsMvcAttribute);

internal readonly record struct MapMethodParts(
    IMethodSymbol Method,
    LiteralExpressionSyntax RouteStringExpression,
    ExpressionSyntax DelegateExpression);

internal static class RoutePatternUsageDetector
{
    public static RoutePatternUsageContext BuildContext(SyntaxToken token, SemanticModel semanticModel, WellKnownTypes wellKnownTypes, CancellationToken cancellationToken)
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
            var mapMethodParts = FindMapMethodParts(semanticModel, wellKnownTypes, container, cancellationToken);
            if (mapMethodParts == null)
            {
                return default;
            }

            // Get the map method delegate.
            var mapMethodSymbol = GetMethodInfo(semanticModel, mapMethodParts.Value.DelegateExpression, cancellationToken);

            return new(MethodSymbol: mapMethodSymbol, MethodSyntax: mapMethodParts.Value.DelegateExpression, IsMinimal: true, IsMvcAttribute: false);
        }
        else if (container.Parent.IsKind(SyntaxKind.AttributeArgument))
        {
            // We're an argument in an attribute. See if attribute is on a controller method.
            var attributeParent = FindAttributeParent(container);
            if (attributeParent is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax, cancellationToken);

                var actionMethodSymbol = FindMvcMethod(wellKnownTypes, methodSymbol);
                if (actionMethodSymbol == null)
                {
                    return default;
                }
                return new(MethodSymbol: actionMethodSymbol, MethodSyntax: methodDeclarationSyntax, IsMinimal: false, IsMvcAttribute: true);
            }
            else if (attributeParent is ClassDeclarationSyntax classDeclarationSyntax)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken);

                return new(MethodSymbol: null, MethodSyntax: null, IsMinimal: false, IsMvcAttribute: MvcDetector.IsController(classSymbol, wellKnownTypes));
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

    private static IMethodSymbol? FindMvcMethod(WellKnownTypes wellKnownTypes, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ContainingType is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!MvcDetector.IsController(typeSymbol, wellKnownTypes))
        {
            return null;
        }

        if (!MvcDetector.IsAction(methodSymbol, wellKnownTypes))
        {
            return null;
        }

        return methodSymbol;
    }

    public static MapMethodParts? FindMapMethodParts(SemanticModel semanticModel, WellKnownTypes wellKnownTypes, SyntaxNode container, CancellationToken cancellationToken)
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
                var mapMethodParts = FindValidMapMethodParts(semanticModel, wellKnownTypes, argumentList, methodSymbol);
                if (mapMethodParts != null)
                {
                    return mapMethodParts;
                }
            }
        }

        return null;
    }

    private static MapMethodParts? FindValidMapMethodParts(SemanticModel semanticModel, WellKnownTypes wellKnownTypes, BaseArgumentListSyntax argumentList, IMethodSymbol method)
    {
        if (!method.Name.StartsWith("Map", StringComparison.Ordinal))
        {
            return null;
        }

        // IEndpointRouteBuilder may be removed from symbol because the method is called as an extension method.
        // ReducedFrom includes the original IEndpointRouteBuilder parameter.
        if (!(method.ReducedFrom ?? method).Parameters.Any(
            a => SymbolEqualityComparer.Default.Equals(a.Type, wellKnownTypes.IEndpointRouteBuilder) ||
                a.Type.Implements(wellKnownTypes.IEndpointRouteBuilder)))
        {
            return null;
        }

        // Method has a delegate parameter. Could be Delegate or something that inherits from it, e.g. RequestDelegate.
        var delegateSymbol = semanticModel.Compilation.GetSpecialType(SpecialType.System_Delegate);
        var delegateParameter = method.Parameters.FirstOrDefault(p => delegateSymbol.IsAssignableFrom(p.Type));
        if (delegateParameter == null)
        {
            return null;
        }

        var delegateArgument = GetArgumentSyntax(argumentList, method, delegateParameter);
        if (delegateArgument == null)
        {
            return null;
        }

        var stringSymbol = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
        var routeStringParameter = method.Parameters.FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(stringSymbol, p.Type) &&
            RouteStringSyntaxDetector.HasMatchingStringSyntaxAttribute(p, out var identifer) &&
            identifer == "Route");
        if (routeStringParameter == null)
        {
            return null;
        }

        var routeStringArgument = GetArgumentSyntax(argumentList, method, routeStringParameter);
        if (routeStringArgument?.Expression is not LiteralExpressionSyntax literalExpression)
        {
            return null;
        }

        return new MapMethodParts(method, literalExpression, delegateArgument.Expression);
    }

    private static ArgumentSyntax? GetArgumentSyntax(BaseArgumentListSyntax argumentList, IMethodSymbol methodSymbol, IParameterSymbol? parameterSymbol)
    {
        foreach (var argument in argumentList.Arguments)
        {
            // Handle named argument
            if (argument.NameColon != null && !argument.NameColon.IsMissing)
            {
                var name = argument.NameColon.Name.Identifier.ValueText;
                if (name == parameterSymbol.Name)
                {
                    return argument;
                }
            }
        }

        // Handle positional argument
        var index = methodSymbol.Parameters.IndexOf(parameterSymbol);
        if (index >= argumentList.Arguments.Count)
        {
            return null;
        }

        return argumentList.Arguments[index];
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
