// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Avoid referencing the whole Microsoft.AspNetCore.Components.Routing namespace to
// avoid the risk of accidentally relying on the non-legacy types in the legacy fork
using RouteContext = Microsoft.AspNetCore.Components.Routing.RouteContext;

namespace Microsoft.AspNetCore.Components.LegacyRouteMatching
{
    internal class LegacyRouteTable : Routing.IRouteTable
    {
        public LegacyRouteTable(LegacyRouteEntry[] routes)
        {
            Routes = routes;
        }

        public LegacyRouteEntry[] Routes { get; }

        public void Route(RouteContext routeContext)
        {
            for (var i = 0; i < Routes.Length; i++)
            {
                Routes[i].Match(routeContext);
                if (routeContext.Handler != null)
                {
                    return;
                }
            }
        }
    }
}
