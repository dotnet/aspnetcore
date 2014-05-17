// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing.Constraints
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

        /// <summary>
        /// Calls Match on the child constraints. 
        /// The call returns as soon as one of the child constraints does not match.
        /// </summary>
        /// <param name="httpContext">The HTTP context associated with the current call.</param>
        /// <param name="route">The route that is being constrained.</param>
        /// <param name="routeKey">The route key used for the constraint.</param>
        /// <param name="values">The route value dictionary.</param>
        /// <param name="routeDirection">The direction of the routing,
        /// i.e. incoming request or URL generation.</param>
        /// <returns>True if all the constraints Match,
        ///  false as soon as one of the child constraints does not match.</returns>
        /// <remarks>
        /// There is no guarantee for the order in which child constraints are invoked, 
        /// also the method returns as soon as one of the constraints does not match.
        /// </remarks>
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