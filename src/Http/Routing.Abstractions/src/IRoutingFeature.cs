// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// A feature interface for routing functionality.
    /// </summary>
    public interface IRoutingFeature
    {
        /// <summary>
        /// Gets or sets the <see cref="Routing.RouteData"/> associated with the current request.
        /// </summary>
        RouteData RouteData { get; set; }
    }
}
