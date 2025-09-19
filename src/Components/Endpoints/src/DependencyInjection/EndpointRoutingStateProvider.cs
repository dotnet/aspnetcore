// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints.DependencyInjection;

internal sealed class EndpointRoutingStateProvider : IRoutingStateProvider
{
    private static volatile bool _cacheInvalidated;
    private RouteData? _routeData;

    static EndpointRoutingStateProvider()
    {
        if (HotReloadManager.Default.MetadataUpdateSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += InvalidateCache;
        }
    }

    public RouteData? RouteData 
    { 
        get => _cacheInvalidated ? null : _routeData;
        internal set => _routeData = value; 
    }

    private static void InvalidateCache()
    {
        // Invalidate cached route data during hot reload to prevent stale route data
        // from being used when component route templates change
        _cacheInvalidated = true;
    }
}
