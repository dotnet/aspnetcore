// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// Provides an abstraction over <see cref="RouteTable"/> and <see cref="LegacyRouteMatching.LegacyRouteTable"/>.
    /// This is only an internal implementation detail of <see cref="Router"/> and can be removed once
    /// the legacy route matching logic is removed.
    /// </summary>
    internal interface IRouteTable
    {
        void Route(RouteContext routeContext);
    }
}
