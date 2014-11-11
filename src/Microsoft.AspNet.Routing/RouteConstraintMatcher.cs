// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.AspNet.Routing.Logging.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Routing
{
    public static class RouteConstraintMatcher
    {
        public static bool Match(IReadOnlyDictionary<string, IRouteConstraint> constraints,
                                 [NotNull] IDictionary<string, object> routeValues,
                                 [NotNull] HttpContext httpContext,
                                 [NotNull] IRouter route,
                                 [NotNull] RouteDirection routeDirection,
                                 [NotNull] ILogger logger)
        {
            if (constraints == null)
            {
                return true;
            }

            foreach (var kvp in constraints)
            {
                var constraint = kvp.Value;
                if (!constraint.Match(httpContext, route, kvp.Key, routeValues, routeDirection))
                {
                    if (routeDirection.Equals(RouteDirection.IncomingRequest)
                        && logger.IsEnabled(LogLevel.Verbose))
                    {
                        logger.WriteValues(new RouteConstraintMatcherMatchValues()
                        {
                            ConstraintKey = kvp.Key,
                            Constraint = kvp.Value,
                            Matched = false
                        });
                    }

                    return false;
                }

                if (routeDirection.Equals(RouteDirection.IncomingRequest)
                    && logger.IsEnabled(LogLevel.Verbose))
                {
                    logger.WriteValues(new RouteConstraintMatcherMatchValues()
                    {
                        ConstraintKey = kvp.Key,
                        Constraint = kvp.Value,
                        Matched = true
                    });
                }
            }

            return true;
        }
    }
}
