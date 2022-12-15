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
        if (_lazyRoutePatterns.TryGetValue(syntaxToken, out var routeUsageModel))
        {
            return routeUsageModel;
        }

        return GetAndCache(syntaxToken, cancellationToken);
    }

    private RouteUsageModel? GetAndCache(SyntaxToken syntaxToken, CancellationToken cancellationToken)
    {
        return _lazyRoutePatterns.GetOrAdd(syntaxToken, token =>
        {
            if (syntaxToken.SyntaxTree == null)
            {
                return null;
            }

            var semanticModel = _compilation.GetSemanticModel(syntaxToken.SyntaxTree);

            if (!RouteStringSyntaxDetector.IsRouteStringSyntaxToken(token, semanticModel, cancellationToken, out var options))
            {
                return null;
            }

            var wellKnownTypes = WellKnownTypes.GetOrCreate(_compilation);
            var usageContext = RouteUsageDetector.BuildContext(
                options,
                token,
                semanticModel,
                wellKnownTypes,
                cancellationToken);

            var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            var isMvc = usageContext.UsageType == RouteUsageType.MvcAction || usageContext.UsageType == RouteUsageType.MvcController;
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
        });
    }
}
