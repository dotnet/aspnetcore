// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Dispatcher.Patterns;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// Used to build a <see cref="TreeMatcher"/>. Represents a route pattern that will be used to match incoming
    /// request URLs.
    /// </summary>
    public class InboundRouteEntry
    {
        /// <summary>
        /// Gets or sets the dispatcher value constraints.
        /// </summary>
        public IDictionary<string, IDispatcherValueConstraint> Constraints { get; set; }

        /// <summary>
        /// Gets or sets the dispatcher value defaults.
        /// </summary>
        public DispatcherValueCollection Defaults { get; set; }

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
        /// Gets or sets the <see cref="RoutePattern"/>.
        /// </summary>
        public RoutePattern RoutePattern { get; set; }

        /// <summary>
        /// Gets or sets an arbitrary value associated with the entry.
        /// </summary>
        public object Tag { get; set; }
    }
}
