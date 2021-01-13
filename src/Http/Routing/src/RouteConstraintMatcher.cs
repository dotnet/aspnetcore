// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing
{
    public static class RouteConstraintMatcher
    {
        public static bool Match(
            IDictionary<string, IRouteConstraint> constraints,
            RouteValueDictionary routeValues,
            HttpContext httpContext,
            IRouter route,
            RouteDirection routeDirection,
            ILogger logger)
        {
            if (routeValues == null)
            {
                throw new ArgumentNullException(nameof(routeValues));
            }

            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (constraints == null || constraints.Count == 0)
            {
                return true;
            }

            foreach (var kvp in constraints)
            {
                var constraint = kvp.Value;
                if (!constraint.Match(httpContext, route, kvp.Key, routeValues, routeDirection))
                {
                    if (routeDirection.Equals(RouteDirection.IncomingRequest))
                    {
                        routeValues.TryGetValue(kvp.Key, out var routeValue);

                        logger.ConstraintNotMatched(routeValue, kvp.Key, kvp.Value);
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
