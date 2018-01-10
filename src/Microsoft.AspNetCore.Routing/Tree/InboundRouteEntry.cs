// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Tree
{
    /// <summary>
    /// Used to build an <see cref="TreeRouter"/>. Represents a URL template tha will be used to match incoming
    /// request URLs.
    /// </summary>
    public class InboundRouteEntry
    {
        /// <summary>
        /// Gets or sets the route constraints.
        /// </summary>
        public IDictionary<string, IRouteConstraint> Constraints { get; set; }

        /// <summary>
        /// Gets or sets the route defaults.
        /// </summary>
        public RouteValueDictionary Defaults { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IRouter"/> to invoke when this entry matches.
        /// </summary>
        public IRouter Handler { get; set; }

        /// <summary>
        /// Gets or sets the order of the entry.
        /// </summary>
        /// <remarks>
        /// Entries are ordered first by <see cref="Order"/> (ascending) then by <see cref="Precedence"/> (descending).
        /// </remarks>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the precedence of the entry.
        /// </summary>
        /// <remarks>
        /// Entries are ordered first by <see cref="Order"/> (ascending) then by <see cref="Precedence"/> (descending).
        /// </remarks>
        public decimal Precedence { get; set; }

        /// <summary>
        /// Gets or sets the name of the route.
        /// </summary>
        public string RouteName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RouteTemplate"/>.
        /// </summary>
        public RouteTemplate RouteTemplate { get; set; }
    }
}
