// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Result
{
    internal sealed class CreatedAtRouteResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedAtRouteResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public CreatedAtRouteResult(object? routeValues, object? value)
            : this(routeName: null, routeValues: routeValues, value: value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedAtRouteResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public CreatedAtRouteResult(
            string? routeName,
            object? routeValues,
            object? value)
            : base(value, StatusCodes.Status201Created)
        {
            RouteName = routeName;
            RouteValues = routeValues == null ? null : new RouteValueDictionary(routeValues);
        }

        /// <summary>
        /// Gets or sets the name of the route to use for generating the URL.
        /// </summary>
        public string? RouteName { get; set; }

        /// <summary>
        /// Gets or sets the route data to use for generating the URL.
        /// </summary>
        public RouteValueDictionary? RouteValues { get; set; }

        /// <inheritdoc />
        protected override void ConfigureResponseHeaders(HttpContext context)
        {
            var linkGenerator = context.RequestServices.GetRequiredService<LinkGenerator>();
            var url = linkGenerator.GetUriByRouteValues(
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
