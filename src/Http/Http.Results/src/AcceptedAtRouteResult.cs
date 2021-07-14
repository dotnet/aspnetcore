// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Result
{
    internal sealed class AcceptedAtRouteResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptedAtRouteResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public AcceptedAtRouteResult(object? routeValues, object? value)
            : this(routeName: null, routeValues: routeValues, value: value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptedAtRouteResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public AcceptedAtRouteResult(
            string? routeName,
            object? routeValues,
            object? value)
            : base(value, StatusCodes.Status202Accepted)
        {
            RouteName = routeName;
            RouteValues = new RouteValueDictionary(routeValues);
        }

        /// <summary>
        /// Gets the name of the route to use for generating the URL.
        /// </summary>
        public string? RouteName { get; }

        /// <summary>
        /// Gets the route data to use for generating the URL.
        /// </summary>
        public RouteValueDictionary RouteValues { get; }

        /// <inheritdoc />
        protected override void ConfigureResponseHeaders(HttpContext context)
        {
            var linkGenerator = context.RequestServices.GetRequiredService<LinkGenerator>();
            var url = linkGenerator.GetUriByAddress(
                context,
                RouteName,
                RouteValues,
                fragment: FragmentString.Empty);

            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException("No route matches the supplied values.");
            }

            context.Response.Headers.Location = url;
        }
    }
}
