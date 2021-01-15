// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
