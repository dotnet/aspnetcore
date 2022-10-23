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

    private VaryByRouteValuePolicy()
    {
    }

    public VaryByRouteValuePolicy(string routeValue, params string[] routeValueNames)
    {
        ArgumentNullException.ThrowIfNull(routeValue);

        _routeValueNames = routeValue;

        if (routeValueNames != null && routeValueNames.Length > 0)
        {
            _routeValueNames = StringValues.Concat(_routeValueNames, routeValueNames);
        }
    }

    public VaryByRouteValuePolicy(string[] routeValueNames)
    {
        ArgumentNullException.ThrowIfNull(routeValueNames);

        _routeValueNames = routeValueNames;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        context.CacheVaryByRules.RouteValueNames = _routeValueNames;
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
