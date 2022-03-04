// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Extension methods for <see cref="HttpContext"/> related to routing.
    /// </summary>
    public static class RoutingHttpContextExtensions
    {
        /// <summary>
        /// Gets the <see cref="RouteData"/> associated with the provided <paramref name="httpContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <returns>The <see cref="RouteData"/>, or null.</returns>
        public static RouteData GetRouteData(this HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var routingFeature = httpContext.Features.Get<IRoutingFeature>();
            return routingFeature?.RouteData ?? new RouteData(httpContext.Request.RouteValues);
        }

        /// <summary>
        /// Gets a route value from <see cref="RouteData.Values"/> associated with the provided
        /// <paramref name="httpContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="key">The key of the route value.</param>
        /// <returns>The corresponding route value, or null.</returns>
        public static object GetRouteValue(this HttpContext httpContext, string key)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return httpContext.Features.Get<IRouteValuesFeature>()?.RouteValues[key];
        }
    }
}
