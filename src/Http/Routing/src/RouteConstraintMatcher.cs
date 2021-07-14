// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Use to evaluate if all route parameter values match their constraints.
    /// </summary>
    public static partial class RouteConstraintMatcher
    {
        /// <summary>
        /// Determines if <paramref name="routeValues"/> match the provided <paramref name="constraints"/>.
        /// </summary>
        /// <param name="constraints">The constraints for the route.</param>
        /// <param name="routeValues">The route parameter values extracted from the matched route.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="route">The router that this constraint belongs to.</param>
        /// <param name="routeDirection">
        /// Indicates whether the constraint check is performed
        /// when the incoming request is handled or when a URL is generated.
        /// </param>
        /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
        /// <returns><see langword="true"/> if the all route values match their constraints.</returns>
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

                        Log.ConstraintNotMatched(logger, routeValue!, kvp.Key, kvp.Value);
                    }

                    return false;
                }
            }

            return true;
        }

        private static partial class Log
        {
            [LoggerMessage(1, LogLevel.Debug,
                "Route value '{RouteValue}' with key '{RouteKey}' did not match the constraint '{RouteConstraint}'",
                EventName = "ConstraintNotMatched")]
            public static partial void ConstraintNotMatched(
                ILogger logger,
                object routeValue,
                string routeKey,
                IRouteConstraint routeConstraint);
        }
    }
}
