// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Routing.Tree
{
    /// <summary>
    /// Used to build an <see cref="TreeRouter"/>. Represents an individual URL-matching route that will be
    /// aggregated into the <see cref="TreeRouter"/>.
    /// </summary>
    public class TreeRouteMatchingEntry
    {
        /// <summary>
        /// The order of the template.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The precedence of the template.
        /// </summary>
        public decimal Precedence { get; set; }

        /// <summary>
        /// The <see cref="IRouter"/> to invoke when this entry matches.
        /// </summary>
        public IRouter Target { get; set; }

        /// <summary>
        /// The name of the route.
        /// </summary>
        public string RouteName { get; set; }

        /// <summary>
        /// The <see cref="RouteTemplate"/>.
        /// </summary>
        public RouteTemplate RouteTemplate { get; set; }

        /// <summary>
        /// The <see cref="TemplateMatcher"/>.
        /// </summary>
        public TemplateMatcher TemplateMatcher { get; set; }

        /// <summary>
        /// The route constraints.
        /// </summary>
        public IDictionary<string, IRouteConstraint> Constraints { get; set; }
    }
}
