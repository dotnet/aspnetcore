// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    /// <summary>
    /// Constrains the HTTP method of request or a route.
    /// </summary>
    public class HttpMethodRouteConstraint : IRouteConstraint
    {
        /// <summary>
        /// Creates a new instance of <see cref="HttpMethodRouteConstraint"/> that accepts the HTTP methods specified
        /// by <paramref name="allowedMethods"/>.
        /// </summary>
        /// <param name="allowedMethods">The allowed HTTP methods.</param>
        public HttpMethodRouteConstraint(params string[] allowedMethods)
        {
            if (allowedMethods == null)
            {
                throw new ArgumentNullException(nameof(allowedMethods));
            }

            AllowedMethods = new List<string>(allowedMethods);
        }

        /// <summary>
        /// Gets the HTTP methods allowed by the constraint.
        /// </summary>
        public IList<string> AllowedMethods { get; }

        /// <inheritdoc />
        public virtual bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (routeKey == null)
            {
                throw new ArgumentNullException(nameof(routeKey));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            switch (routeDirection)
            {
                case RouteDirection.IncomingRequest:
                    // Only required for constraining incoming requests
                    if (httpContext == null)
                    {
                        throw new ArgumentNullException(nameof(httpContext));
                    }

                    return AllowedMethods.Contains(httpContext.Request.Method, StringComparer.OrdinalIgnoreCase);

                case RouteDirection.UrlGeneration:
                    // We need to see if the user specified the HTTP method explicitly.  Consider these two routes:
                    //
                    // a) Route: template = "/{foo}", Constraints = { httpMethod = new HttpMethodRouteConstraint("GET") }
                    // b) Route: template = "/{foo}", Constraints = { httpMethod = new HttpMethodRouteConstraint("POST") }
                    //
                    // A user might know ahead of time that a URI he/she is generating might be used with a particular HTTP
                    // method.  If a URI will be used for an HTTP POST but we match on (a) while generating the URI, then
                    // the HTTP GET-specific route will be used for URI generation, which might have undesired behavior.
                    //
                    // To prevent this, a user might call GetVirtualPath(..., { httpMethod = "POST" }) to
                    // signal that they are generating a URI that will be used for an HTTP POST, so they want the URI
                    // generation to be performed by the (b) route instead of the (a) route, consistent with what would
                    // happen on incoming requests.
                    if (!values.TryGetValue(routeKey, out var obj))
                    {
                        return true;
                    }

                    return AllowedMethods.Contains(Convert.ToString(obj), StringComparer.OrdinalIgnoreCase);

                default:
                    throw new ArgumentOutOfRangeException(nameof(routeDirection));
            }
        }
    }
}
