// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Metadata used during link generation to find the associated endpoint using route name.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public sealed class RouteNameMetadata : IRouteNameMetadata
    {
        /// <summary>
        /// Creates a new instance of <see cref="RouteNameMetadata"/> with the provided route name.
        /// </summary>
        /// <param name="routeName">The route name. Can be null.</param>
        public RouteNameMetadata(string routeName)
        {
            RouteName = routeName;
        }

        /// <summary>
        /// Gets the route name. Can be null.
        /// </summary>
        public string RouteName { get; }

        internal string DebuggerToString()
        {
            return $"Name: {RouteName}";
        }
    }
}