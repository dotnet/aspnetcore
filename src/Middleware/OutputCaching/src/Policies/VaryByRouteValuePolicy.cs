// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every value of the provided route values.
/// </summary>
internal sealed class VaryByRouteValuePolicy : IOutputCachePolicy
{
    private readonly StringValues _routeValueNames;

    /// <summary>
    /// Creates a policy that doesn't vary the cached content based on route values.
    /// </summary>
    public VaryByRouteValuePolicy()
    {
    }

    /// <summary>
    /// Creates a policy that varies the cached content based on the specified route value name.
    /// </summary>
    public VaryByRouteValuePolicy(string routeValue)
    {
        ArgumentNullException.ThrowIfNull(routeValue);

        _routeValueNames = routeValue;
    }

    /// <summary>
    /// Creates a policy that varies the cached content based on the specified route value names.
    /// </summary>
    public VaryByRouteValuePolicy(params string[] routeValueNames)
    {
        ArgumentNullException.ThrowIfNull(routeValueNames);

        _routeValueNames = routeValueNames;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // No vary by route value?
        if (_routeValueNames.Count == 0)
        {
            context.CacheVaryByRules.RouteValueNames = _routeValueNames;
            return ValueTask.CompletedTask;
        }

        context.CacheVaryByRules.RouteValueNames = StringValues.Concat(context.CacheVaryByRules.RouteValueNames, _routeValueNames);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
