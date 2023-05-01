// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints.DependencyInjection;

internal sealed class EndpointRouteDataProvider : DefaultRouteDataProvider
{
    private RouteData? _routeData;

    public override RouteData? GetRouteData(string url) => _routeData;

    internal void SetRouteData(RouteData? routeData)
    {
        _routeData = routeData;
    }
}
