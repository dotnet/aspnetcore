// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// An address of route name and values.
    /// </summary>
    public class RouteValuesAddress
    {
        /// <summary>
        /// Gets or sets the route name.
        /// </summary>
        public string? RouteName { get; set; }

        /// <summary>
        /// Gets or sets the route values that are explicitly specified.
        /// </summary>
        public RouteValueDictionary ExplicitValues { get; set; }

        /// <summary>
        /// Gets or sets ambient route values from the current HTTP request.
        /// </summary>
        public RouteValueDictionary? AmbientValues { get; set; }
    }
}
