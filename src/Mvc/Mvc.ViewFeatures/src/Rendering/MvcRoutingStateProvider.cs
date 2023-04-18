// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Rendering;

internal sealed class MvcRoutingStateProvider : RoutingStateProvider
{
    internal void SetRouteData(RouteData routeData)
    {
        RouteData = routeData;
    }
}
