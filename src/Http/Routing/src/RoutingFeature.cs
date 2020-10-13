// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// A feature for routing functionality.
    /// </summary>
    public class RoutingFeature : IRoutingFeature
    {
        /// <inheritdoc />
        public RouteData? RouteData { get; set; }
    }
}
