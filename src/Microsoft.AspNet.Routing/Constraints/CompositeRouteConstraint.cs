// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing
{
    /// <summary>
    /// Constrains a route by several child constraints.
    /// </summary>
    public class CompositeRouteConstraint : IRouteConstraint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeRouteConstraint" /> class.
        /// </summary>
        /// <param name="constraints">The child constraints that must match for this constraint to match.</param>
        public CompositeRouteConstraint([NotNull] IEnumerable<IRouteConstraint> constraints)
        {
            Constraints = constraints;
        }

        /// <summary>
        /// Gets the child constraints that must match for this constraint to match.
        /// </summary>
        public IEnumerable<IRouteConstraint> Constraints { get; private set; }

        /// <inheritdoc />
        public bool Match([NotNull] HttpContext httpContext,
                          [NotNull] IRouter route,
                          [NotNull] string routeKey,
                          [NotNull] IDictionary<string, object> values,
                          RouteDirection routeDirection)
        {
            foreach (var constraint in Constraints)
            {
                if (!constraint.Match(httpContext, route, routeKey, values, routeDirection))
                {
                    return false;
                }
            }

            return true;
        }
    }
}