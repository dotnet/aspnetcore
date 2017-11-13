// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Dispatcher.Patterns;
#if ROUTING
using Microsoft.AspNetCore.Routing.Template;
#elif DISPATCHER
using Microsoft.AspNetCore.Dispatcher;
#else
#error
#endif

#if ROUTING
namespace Microsoft.AspNetCore.Routing.Tree
#elif DISPATCHER
namespace Microsoft.AspNetCore.Dispatcher
#else
#error
#endif
{
#if ROUTING
    /// <summary>
    /// Used to build a <see cref="TreeRouter"/>. Represents a route template that will be used to match incoming
    /// request URLs.
    /// </summary>
    public
#elif DISPATCHER
    /// <summary>
    /// Used to build a <see cref="TreeMatcher"/>. Represents a route pattern that will be used to match incoming
    /// request URLs.
    /// </summary>
    internal
#else
#error
#endif
    class InboundRouteEntry
    {
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

#if ROUTING
        /// <summary>
        /// Gets or sets the name of the route.
        /// </summary>
        public string RouteName { get; set; }

        /// <summary>
        /// Gets or sets the route constraints.
        /// </summary>
        public IDictionary<string, IRouteConstraint> Constraints { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RouteTemplate"/>.
        /// </summary>
        public RouteTemplate RouteTemplate { get; set; }

        /// <summary>
        /// Gets or sets the route defaults.
        /// </summary>
        public RouteValueDictionary Defaults { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IRouter"/> to invoke when this entry matches.
        /// </summary>
        public IRouter Handler { get; set; }

#elif DISPATCHER
        /// <summary>
        /// Gets or sets the array of endpoints associated with the entry.
        /// </summary>
        public Endpoint[] Endpoints { get; set; }

        /// <summary>
        /// Gets or sets the dispatcher value constraints.
        /// </summary>
        public IDictionary<string, IDispatcherValueConstraint> Constraints { get; set; }

        /// <summary>
        /// Gets or sets the dispatcher value defaults.
        /// </summary>
        public DispatcherValueCollection Defaults { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RoutePattern"/>.
        /// </summary>
        public RoutePattern RoutePattern { get; set; }
#else
#error
#endif
    }
}
