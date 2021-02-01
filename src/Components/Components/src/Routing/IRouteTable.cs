// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
