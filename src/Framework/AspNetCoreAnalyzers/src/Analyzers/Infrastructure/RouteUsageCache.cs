// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.App.Analyzers.Infrastructure;

internal sealed class RouteUsageCache
{
    private static readonly BoundedCacheWithFactory<Compilation, RouteUsageCache> LazyRouteUsageCache = new();

    public static RouteUsageCache GetOrCreate(Compilation compilation) =>
        LazyRouteUsageCache.GetOrCreateValue(compilation, static c => new RouteUsageCache(c));

    private readonly ConcurrentDictionary<SyntaxToken, RouteUsageModel?> _lazyRoutePatterns;
    private readonly Compilation _compilation;

    private RouteUsageCache(Compilation compilation)
    {
        _lazyRoutePatterns = new();
        _compilation = compilation;
    }

    public RouteUsageModel? Get(SyntaxToken syntaxToken, CancellationToken cancellationToken)
    {
        LastInspectedStringNode? lastInspectedStringNode = null;
        return Get(syntaxToken, ref lastInspectedStringNode, cancellationToken);
    }

    public RouteUsageModel? Get(SyntaxToken syntaxToken, ref LastInspectedStringNode? lastInspectedStringNode, CancellationToken cancellationToken)
    {
        if (_lazyRoutePatterns.TryGetValue(syntaxToken, out var routeUsageModel))
        {
            return routeUsageModel;
        }

        return GetAndCache(syntaxToken, ref lastInspectedStringNode, cancellationToken);
    }

    private sealed class RouteUsageModelLoader
    {
        private readonly Compilation _compilation;
        private readonly CancellationToken _cancellationToken;
        private LastInspectedStringNode? _lastInspectedStringNode;

        public LastInspectedStringNode? LastInspectedStringNode => _lastInspectedStringNode;

        public RouteUsageModelLoader(Compilation compilation, LastInspectedStringNode? lastInspectedStringNode, CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _lastInspectedStringNode = lastInspectedStringNode;
            _cancellationToken = cancellationToken;
        }

        public RouteUsageModel? Load(SyntaxToken token)
        {
            if (token.SyntaxTree == null)
            {
                return null;
            }

            var semanticModel = _compilation.GetSemanticModel(token.SyntaxTree);

            if (!RouteStringSyntaxDetector.IsRouteStringSyntaxToken(token, semanticModel, ref _lastInspectedStringNode, _cancellationToken, out var options))
            {
                return null;
            }

            var wellKnownTypes = WellKnownTypes.GetOrCreate(_compilation);
            var usageContext = RouteUsageDetector.BuildContext(
                options,
                token,
                semanticModel,
                wellKnownTypes,
                _cancellationToken);

            var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            var tree = RoutePatternParser.TryParse(virtualChars, usageContext.RoutePatternOptions);
            if (tree == null)
            {
                return null;
            }

            return new RouteUsageModel
            {
                RoutePattern = tree,
                UsageContext = usageContext
            };
        }
    }

    private RouteUsageModel? GetAndCache(SyntaxToken syntaxToken, ref LastInspectedStringNode? lastInspectedStringNode, CancellationToken cancellationToken)
    {
        var routeUsageModelLoader = new RouteUsageModelLoader(_compilation, lastInspectedStringNode, cancellationToken);
        var model = _lazyRoutePatterns.GetOrAdd(syntaxToken, routeUsageModelLoader.Load);
        lastInspectedStringNode = routeUsageModelLoader.LastInspectedStringNode;

        return model;
    }
}
