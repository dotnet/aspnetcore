// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing
{
    public static class RouteConstraintMatcher
    {
        public static bool Match(IDictionary<string, IRouteConstraint> constraints,
                                 [NotNull] IDictionary<string, object> routeValues,
                                 [NotNull] HttpContext httpContext,
                                 [NotNull] IRouter route,
                                 [NotNull] RouteDirection routeDirection)
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
                    return false;
                }
            }

            return true;
        }
    }
}
