// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

[ExportAspNetCoreEmbeddedLanguageDocumentHighlighter(name: "Route", language: LanguageNames.CSharp)]
internal class RoutePatternHighlighter : IAspNetCoreEmbeddedLanguageDocumentHighlighter
{
    public ImmutableArray<AspNetCoreDocumentHighlights> GetDocumentHighlights(
        SemanticModel semanticModel, SyntaxToken token, int position, CancellationToken cancellationToken)
    {
        var routeUsageCache = RouteUsageCache.GetOrCreate(semanticModel.Compilation);
        var routeUsage = routeUsageCache.Get(token, cancellationToken);
        if (routeUsage is null)
        {
            return ImmutableArray<AspNetCoreDocumentHighlights>.Empty;
        }

        return GetHighlights(routeUsage, semanticModel, position, cancellationToken);
    }

    private static ImmutableArray<AspNetCoreDocumentHighlights> GetHighlights(
        RouteUsageModel routeUsage, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
    {
        var routePattern = routeUsage.RoutePattern;
        var virtualChar = routePattern.Text.Find(position);
        if (virtualChar == null)
        {
            return ImmutableArray<AspNetCoreDocumentHighlights>.Empty;
        }

        var node = FindParameterNode(routePattern.Root, virtualChar.Value);
        if (node == null)
        {
            return ImmutableArray<AspNetCoreDocumentHighlights>.Empty;
        }

        var highlightSpans = ImmutableArray.CreateBuilder<AspNetCoreHighlightSpan>();

        // Highlight the parameter in the route string, e.g. "{id}" highlights "id".
        highlightSpans.Add(new AspNetCoreHighlightSpan(node.GetSpan(), AspNetCoreHighlightSpanKind.Reference));

        if (routeUsage.UsageContext.MethodSymbol is { } methodSymbol)
        {
            // Resolve possible parameter symbols. Includes properties from AsParametersAttribute.
            var resolvedParameters = routeUsage.UsageContext.ResolvedParameters;

            // Match route parameter to method parameter. Parameters in a route aren't case sensitive.
            // It's possible to match multiple parameters, either based on parameter name, or [FromRoute(Name = "XXX")] attribute.
            var parameterName = node.ParameterNameToken.Value!.ToString();
            foreach (var matchingParameter in resolvedParameters.Where(s => string.Equals(s.RouteParameterName, parameterName, StringComparison.OrdinalIgnoreCase)))
            {
                HighlightSymbol(semanticModel, methodSymbol, highlightSpans, matchingParameter.Symbol, cancellationToken);
            }
        }

        return ImmutableArray.Create(new AspNetCoreDocumentHighlights(highlightSpans.ToImmutable()));
    }

    private static void HighlightSymbol(SemanticModel semanticModel, IMethodSymbol methodSymbol, IList<AspNetCoreHighlightSpan> highlightSpans, ISymbol matchingParameter, CancellationToken cancellationToken)
    {
        // Highlight parameter in method signature.
        // e.g. "{id}" in route highlights id in "void Foo(string id) {}"
        foreach (var item in matchingParameter.DeclaringSyntaxReferences)
        {
            var syntaxNode = item.GetSyntax(cancellationToken);
            if (syntaxNode is ParameterSyntax parameterSyntax)
            {
                highlightSpans.Add(new AspNetCoreHighlightSpan(parameterSyntax.Identifier.Span, AspNetCoreHighlightSpanKind.Definition));
            }
        }

        // Highlight parameter references inside method.
        // e.g. "{id}" in route highlights id in "_repository.GetBy(id)"
        foreach (var item in methodSymbol.DeclaringSyntaxReferences)
        {
            var methodSyntax = item.GetSyntax(cancellationToken);

            // Have to call GetSymbolInfo because it's easy to have identifiers with the same name
            // that reference a different API. For example, a type with the same name as parameter.
            // GetSymbolInfo can be slow. To reduce calls to it we only get IdentifierNameSyntax
            // nodes, filter them by name first, then check GetSymbolInfo. 
            var parameterReferences = methodSyntax
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(i => i.Identifier.Text == matchingParameter.Name)
                .Where(i => semanticModel.GetSymbolInfo(i) is var symbolInfo && SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault(), matchingParameter));

            foreach (var reference in parameterReferences)
            {
                highlightSpans.Add(new AspNetCoreHighlightSpan(reference.Identifier.Span, AspNetCoreHighlightSpanKind.Reference));
            }
        }
    }

    private static RoutePatternNameParameterPartNode? FindParameterNode(RoutePatternNode node, VirtualChar ch)
        => FindNode<RoutePatternNameParameterPartNode>(node, ch, (parameter, c) => parameter.ParameterNameToken.VirtualChars.Contains(c));

    private static TNode? FindNode<TNode>(RoutePatternNode node, VirtualChar ch, Func<TNode, VirtualChar, bool> predicate)
        where TNode : RoutePatternNode
    {
        if (node is TNode nodeMatch && predicate(nodeMatch, ch))
        {
            return nodeMatch;
        }

        foreach (var child in node)
        {
            if (child.IsNode)
            {
                var result = FindNode(child.Node, ch, predicate);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }
}
